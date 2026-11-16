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

        catalog.Select(c => c.Id).ShouldBe(["node", "ffmpeg", "chromium"]);
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

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha();
}
