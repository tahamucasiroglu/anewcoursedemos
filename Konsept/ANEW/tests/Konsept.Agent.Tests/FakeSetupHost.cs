using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Tests;

/// <summary>
/// Sahte kurulum dünyası (spec 0009): gerçek indirme yok — adres → bellekteki zip; erişilemeyen adresler, çevrimdışı ve disk alanı
/// ayarlanabilir; hash ve zip açma gerçek (geçici klasörde). Araç çalıştırma çıktısı ayarlanabilir.
/// </summary>
public sealed class FakeSetupHost : ISetupHost
{
    /// <summary>Gets adres → içerik.</summary>
    public Dictionary<Uri, byte[]> Content { get; } = [];

    /// <summary>Gets her zaman başarısız olan adresler.</summary>
    public HashSet<Uri> Unreachable { get; } = [];

    /// <summary>Gets yapılan indirmeler (sırayla).</summary>
    public List<Uri> Downloads { get; } = [];

    /// <summary>Gets or sets a value indicating whether internet var.</summary>
    public bool Online { get; set; } = true;

    /// <summary>Gets or sets boş alan.</summary>
    public long Free { get; set; } = long.MaxValue / 4;

    /// <summary>Gets or sets indirme başlamadan çalışan eylem (iptal testi).</summary>
    public Action<Uri, string>? BeforeDownload { get; set; }

    /// <summary>Gets or sets indirmeyi bekleten kapı (arayüz testleri kurulum sürerken durumu okur).</summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <summary>Gets or sets araç çalıştırma (exe, argüman) → çıktı.</summary>
    public Func<string, string, string?> Run { get; set; } = (_, _) => "v1.0.0 tool version 2.0";

    /// <summary>Tek bir yürütülebilir dosya içeren zip üretir.</summary>
    /// <param name="root">Arşiv kök klasörü.</param>
    /// <param name="executable">Kök klasöre göre exe yolu.</param>
    /// <param name="text">Exe içeriği.</param>
    /// <returns>Zip baytları.</returns>
    public static byte[] Zip(string root, string executable, string text)
    {
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry($"{root}/{executable}");
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes(text));
        }

        return memory.ToArray();
    }

    /// <summary>Baytların SHA-256'sı.</summary>
    /// <param name="bytes">Baytlar.</param>
    /// <returns>Küçük harf onaltılık.</returns>
    public static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>Test bileşeni üretir ve içeriğini sahte sunucuya koyar.</summary>
    /// <param name="id">Kimlik.</param>
    /// <param name="mirrored">Aynası var mı.</param>
    /// <param name="check">Hazır denetiminde beklenen metin.</param>
    /// <param name="required">Zorunlu çekirdek bileşen mi (spec 0027: isteğe bağlı olan yalnızca istenince kurulur).</param>
    /// <returns>Bileşen.</returns>
    public SetupComponent Add(string id, bool mirrored = false, string? check = "v1.0.0", bool required = true)
    {
        var zip = Zip(id + "-root", "bin/tool.exe", id + " exe");
        var url = new Uri($"https://example.invalid/{id}.zip");
        Content[url] = zip;
        return new SetupComponent(id, id.ToUpperInvariant(), "amaç", "MIT", "1.0.0", url, id + ".zip", Sha(zip), 1000, 2000, id + "-root", "bin/tool.exe", check is null ? null : "--version", check, required, mirrored);
    }

    /// <inheritdoc />
    public bool IsOnline() => Online;

    /// <inheritdoc />
    public long FreeBytes(string directory) => Free;

    /// <inheritdoc />
    public async Task DownloadAsync(Uri source, string destination, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (Hold is { } hold)
        {
            await hold.Task.WaitAsync(cancellationToken);
        }

        Downloads.Add(source);
        BeforeDownload?.Invoke(source, destination);
        cancellationToken.ThrowIfCancellationRequested();
        if (Unreachable.Contains(source) || !Content.TryGetValue(source, out var bytes))
        {
            throw new HttpRequestException("ulaşılamadı");
        }

        await File.WriteAllBytesAsync(destination, bytes, cancellationToken);
        progress?.Report(0.5);
        progress?.Report(1);
    }

    /// <inheritdoc />
    public async Task<string> ComputeSha256Async(string file, CancellationToken cancellationToken) =>
        Sha(await File.ReadAllBytesAsync(file, cancellationToken));

    /// <inheritdoc />
    public void ExtractArchive(string archive, string destination) => ZipFile.ExtractToDirectory(archive, destination, overwriteFiles: true);

    /// <inheritdoc />
    public Task<string?> RunAsync(string executable, string arguments, CancellationToken cancellationToken) =>
        Task.FromResult(Run(executable, arguments));

    /// <summary>Gets npm kurulum çağrıları (bileşen, kilit hash'i, Node klasörü).</summary>
    public List<(string Id, string Lock, string Node)> PackageInstalls { get; } = [];

    /// <summary>Gets or sets a value indicating whether npm kurulumu başarılı olur.</summary>
    public bool PackagesSucceed { get; set; } = true;

    /// <summary>npm bileşeni (spec 0018 / B-118).</summary>
    /// <param name="id">Kimlik.</param>
    /// <returns>Bileşen.</returns>
    public SetupComponent AddNpm(string id) =>
        new(id, id.ToUpperInvariant(), "amaç", "Apache-2.0", "0.9.43", new Uri($"https://registry.example.invalid/{id}.tgz"), id + ".tgz", new string('a', 64), 1000, 2000, null,
            "bin/tool.mjs", null, null, true, false, SetupComponent.Npm);

    /// <inheritdoc />
    public async Task<bool> InstallPackagesAsync(string componentId, string lockSha256, string nodeDirectory, string destination, CancellationToken cancellationToken)
    {
        PackageInstalls.Add((componentId, lockSha256, nodeDirectory));
        if (!PackagesSucceed)
        {
            return false;
        }

        Directory.CreateDirectory(Path.Combine(destination, "bin"));
        await File.WriteAllTextAsync(Path.Combine(destination, "bin", "tool.mjs"), componentId + " cli", cancellationToken);
        return true;
    }
}
