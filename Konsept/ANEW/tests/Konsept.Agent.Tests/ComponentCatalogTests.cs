using System.Text.RegularExpressions;
using Konsept.Agent.Infrastructure.Setup;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>Ajana gömülü bileşen listesi (spec 0009 AC-2, Req 7, 11): çekirdek, sabit sürüm, resmi HTTPS adres, SHA-256; Ollama yok.</summary>
public sealed partial class ComponentCatalogTests
{
    /// <summary>Çekirdek araçlar zorunlu, sürümü sabit, adresi HTTPS ve hash'i 64 haneli; listede Ollama yok.</summary>
    [Fact]
    public void Catalog_CoreComponents_PinnedHttpsSha256_NoOllama()
    {
        var catalog = ComponentCatalog.Load();

        catalog.Select(c => c.Id).ShouldBe(["node", "ffmpeg", "chromium", "hyperframes"]); // A-1 (plan 0018): HyperFrames eklendi (B-73/B-118).
        catalog.ShouldAllBe(c => c.Required);
        catalog.ShouldAllBe(c => c.Url.Scheme == "https");
        catalog.ShouldAllBe(c => Sha().IsMatch(c.Sha256));
        catalog.ShouldAllBe(c => c.DownloadBytes > 0 && c.InstalledBytes > 0);
        catalog.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Purpose) && !string.IsNullOrWhiteSpace(c.License));
        catalog.ShouldAllBe(c => !c.Url.AbsoluteUri.Contains("latest", StringComparison.OrdinalIgnoreCase));
        catalog.Single(c => c.Id == "node").CheckExpect.ShouldBe("v26.13.1");
        catalog.Single(c => c.Id == "ffmpeg").License.ShouldStartWith("LGPL");
        catalog.Single(c => c.Id == "ffmpeg").Mirrored.ShouldBeTrue();
        catalog.ShouldNotContain(c => c.Id.Contains("ollama", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Ollama", StringComparison.OrdinalIgnoreCase));
        catalog.Select(c => c.FileName).Distinct(StringComparer.Ordinal).Count().ShouldBe(catalog.Count);
    }

    /// <summary>
    /// Spec 0018 / B-118: HyperFrames sabit sürüm (0.9.43), Node'dan sonra, npm ile ajana gömülü kilit dosyasından kurulur; katalogdaki hash
    /// gömülü kilit dosyasınınkine eşit (kilit dosyası değişirse kurulum reddedilir) ve kilit dosyası yalnızca sabit sürümü çözer.
    /// </summary>
    [Fact]
    public void Catalog_HyperFrames_PinnedNpm_LockHashMatches()
    {
        var catalog = ComponentCatalog.Load().ToList();
        var hf = catalog.Single(c => c.Id == "hyperframes");
        hf.Version.ShouldBe("0.9.43");
        hf.Installer.ShouldBe(Konsept.Agent.Domain.Entities.SetupComponent.Npm);
        catalog.IndexOf(hf).ShouldBeGreaterThan(catalog.FindIndex(c => c.Id == "node"));
        using var stream = typeof(WindowsSetupHost).Assembly.GetManifestResourceStream("Konsept.Agent.Infrastructure.Setup.hyperframes.package-lock.json")!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        WindowsSetupHost.LockSha256(memory.ToArray()).ShouldBe(hf.Sha256);
        var lockText = System.Text.Encoding.UTF8.GetString(memory.ToArray());
        lockText.ShouldContain("\"resolved\": \"https://registry.npmjs.org/hyperframes/-/hyperframes-0.9.43.tgz\"", Case.Sensitive);
        lockText.ShouldContain("\"integrity\": \"sha512-");
    }

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha();
}
