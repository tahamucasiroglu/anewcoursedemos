using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Bileşen listesinin kuralları (spec 0028): donanıma göre varyant seçimi (Req 5), bağımlılık kapanışı ve araç denetimi (Req 2, 6), ekran
/// kartı gereksinimi ve çok dosyalı bileşenin dosya listesi metni (Req 3; hash'i katalogdaki <see cref="SetupComponent.Sha256"/>).
/// </summary>
public static class ComponentRules
{
    /// <summary>Varyantları bu bilgisayara göre çözer: ilk uyan varyantın hash'i ve boyutları alınır, sürüme <c>+varyant</c> eklenir.</summary>
    /// <param name="catalog">Gömülü liste.</param>
    /// <param name="gpu">Tespit edilen ekran kartı ya da <c>null</c>.</param>
    /// <returns>Çözülmüş liste (aynı sıra).</returns>
    public static IReadOnlyList<SetupComponent> Resolve(IReadOnlyList<SetupComponent> catalog, GpuInfo? gpu)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return [.. catalog.Select(c => c.Variants is { Count: > 0 } variants ? Apply(c, variants.First(v => Fits(v, gpu))) : c)];
    }

    /// <summary>Varyant bu bilgisayara uyuyor mu.</summary>
    /// <param name="variant">Varyant.</param>
    /// <param name="gpu">Ekran kartı ya da <c>null</c>.</param>
    /// <returns>Uyuyorsa <c>true</c>.</returns>
    public static bool Fits(ComponentVariant variant, GpuInfo? gpu)
    {
        ArgumentNullException.ThrowIfNull(variant);
        return variant.Gpu is null
            || (gpu is not null && string.Equals(variant.Gpu, gpu.Vendor, StringComparison.Ordinal) && gpu.VramMb >= variant.MinVramMb);
    }

    /// <summary>Ekran kartı gereksinimi karşılanıyor mu (gereksinim yoksa her zaman).</summary>
    /// <param name="component">Bileşen.</param>
    /// <param name="gpu">Ekran kartı ya da <c>null</c>.</param>
    /// <returns>Karşılanıyorsa <c>true</c>.</returns>
    public static bool GpuSatisfied(SetupComponent component, GpuInfo? gpu)
    {
        ArgumentNullException.ThrowIfNull(component);
        return component.Gpu is null || (gpu is not null && gpu.VramMb >= component.Gpu.MinVramMb);
    }

    /// <summary>Bileşen ve (özyinelemeli) bağımlılıkları, liste sırasında.</summary>
    /// <param name="id">Bileşen.</param>
    /// <param name="catalog">Liste.</param>
    /// <returns>Kimlikler; listede yoksa yalnızca kendisi.</returns>
    public static IReadOnlyList<string> Closure(string id, IReadOnlyList<SetupComponent> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var byId = catalog.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var found = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>([id]);
        while (stack.Count > 0)
        {
            var next = stack.Pop();
            if (found.Add(next) && byId.TryGetValue(next, out var component))
            {
                foreach (var dependency in component.Requires ?? [])
                {
                    stack.Push(dependency);
                }
            }
        }

        return [.. catalog.Select(c => c.Id).Where(found.Contains), .. found.Where(f => !byId.ContainsKey(f))];
    }

    /// <summary>
    /// Araç denetimi (Req 2, 6): istenen her aracın kendisi ve kurulu olmayan bağımlılıkları eksiktir (liste sırasında; listede olmayan
    /// kurulu değilse eksik, kurulamaz). Ekran kartı gereksinimi karşılanmayan bileşen kurulu olsa da eksik + desteklenmez sayılır.
    /// </summary>
    /// <param name="tools">İstenen araçlar.</param>
    /// <param name="catalog">Çözülmüş liste.</param>
    /// <param name="installed">Kurulu bileşenler.</param>
    /// <param name="gpu">Ekran kartı ya da <c>null</c>.</param>
    /// <returns>Sonuç.</returns>
    public static ToolCheckResult Check(IEnumerable<string> tools, IReadOnlyList<SetupComponent> catalog, IReadOnlySet<string> installed, GpuInfo? gpu)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(installed);
        var missing = new HashSet<string>(StringComparer.Ordinal);
        var unsupported = new List<SetupComponent>();
        var extra = new List<string>();
        foreach (var tool in tools.Distinct(StringComparer.Ordinal))
        {
            var component = catalog.FirstOrDefault(c => string.Equals(c.Id, tool, StringComparison.Ordinal));
            if (component is null)
            {
                if (!installed.Contains(tool) && missing.Add(tool))
                {
                    extra.Add(tool);
                }

                continue;
            }

            if (!GpuSatisfied(component, gpu))
            {
                missing.Add(tool);
                unsupported.Add(component);
                continue;
            }

            foreach (var id in Closure(tool, catalog).Where(id => !installed.Contains(id)))
            {
                missing.Add(id);
            }
        }

        return new ToolCheckResult([.. catalog.Select(c => c.Id).Where(missing.Contains), .. extra], unsupported);
    }

    /// <summary>Çok dosyalı bileşenin dosya listesi metni (kurulumda <see cref="SetupComponent.FilesManifestName"/> olarak yazılır): satır başına "sha256 boyut yol".</summary>
    /// <param name="files">Dosyalar.</param>
    /// <returns>Metin (LF).</returns>
    public static string FilesManifest(IEnumerable<ComponentFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return string.Concat(files.Select(f => f.Sha256 + " " + f.Bytes.ToString(CultureInfo.InvariantCulture) + " " + f.Path + "\n"));
    }

    /// <summary>Dosya listesi metninin SHA-256'sı (katalogdaki hash ile karşılaştırılır).</summary>
    /// <param name="files">Dosyalar.</param>
    /// <returns>Küçük harf onaltılık.</returns>
    public static string FilesManifestSha256(IEnumerable<ComponentFile> files) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(FilesManifest(files))));

    /// <summary>Göreli dosya yolu güvenli mi: köklü değil, sürücü/akış adı ve <c>..</c> içermiyor (bileşen klasörünün dışına yazılamaz).</summary>
    /// <param name="relative">Yol.</param>
    /// <returns>Güvenliyse <c>true</c>.</returns>
    public static bool IsSafeRelativePath(string relative) =>
        !string.IsNullOrWhiteSpace(relative) && !Path.IsPathRooted(relative) && !relative.Contains(':', StringComparison.Ordinal)
        && relative.Split('/', '\\').All(p => p.Length > 0 && p != "." && p != "..");

    private static SetupComponent Apply(SetupComponent component, ComponentVariant variant) =>
        component with
        {
            Sha256 = variant.Sha256,
            DownloadBytes = variant.DownloadBytes,
            InstalledBytes = variant.InstalledBytes,
            Version = component.Version + "+" + variant.Id,
            Variant = variant.Id,
        };
}
