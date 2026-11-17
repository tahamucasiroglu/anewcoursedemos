using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Setup;

/// <summary>
/// Üretimde kullanılabilir (kurulu sayılan) bileşenler (spec 0028 review r1 M-1): kayıt var, sürümü çözülmüş listedekiyle aynı (yeni ajan
/// sürümünde değişen model eski dosyalarla çalıştırılmaz) ve çok dosyalı bileşenin her dosyası yerinde, boyutu doğru (bozuk ağırlık "kurulu"
/// sayılmaz). Böyle bir bileşen araç denetiminde eksik + kurulabilir döner; 0027 penceresi onarımı önerir. Listede olmayan kayıt (eski
/// sürümden kalan) olduğu gibi kurulu sayılır.
/// </summary>
public static class UsableComponents
{
    /// <summary>Kullanılabilir bileşen kimlikleri.</summary>
    /// <param name="state">Kurulum durumu.</param>
    /// <param name="catalog">Çözülmüş liste.</param>
    /// <param name="installRoot">Kurulum yeri (dosya denetimi için); <c>null</c> ise dosyalara bakılmaz.</param>
    /// <returns>Kimlikler.</returns>
    public static IReadOnlySet<string> Of(SetupState state, IReadOnlyList<SetupComponent> catalog, string? installRoot)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(catalog);
        var usable = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, record) in state.Installed)
        {
            var component = catalog.FirstOrDefault(c => c.Id == id);
            if (component is null || (string.Equals(record.Version, component.Version, StringComparison.Ordinal) && FilesIntact(component, installRoot)))
            {
                usable.Add(id);
            }
        }

        return usable;
    }

    private static bool FilesIntact(SetupComponent component, string? installRoot) =>
        installRoot is null || (component.Files ?? []).All(f =>
        {
            var path = Path.Combine(installRoot, "components", component.Id, f.Path.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) && new FileInfo(path).Length == f.Bytes;
        });
}
