using Konsept.Agent.Infrastructure.Settings;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Siteden indirilen tek exe (spec 0009 Req 1): yanında ayar dosyası yokken yayında gömülen Pi adreslerini kullanır; dosya varsa dosya
/// önceliklidir.
/// </summary>
public sealed class AgentSettingsEmbeddedTests
{
    private static readonly Dictionary<string, string?> Embedded = new(StringComparer.Ordinal)
    {
        [AgentSettingsLoader.ServerUrlMetadata] = "http://pi.local:8001",
        [AgentSettingsLoader.SiteUrlMetadata] = "http://pi.local:8000/",
    };

    /// <summary>Ayar dosyası yokken gömülü adresler kullanılır (eğik çizgi eklenir).</summary>
    [Fact]
    public void Settings_NoFile_UsesEmbeddedPiAddresses()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-agent-embedded-");
        try
        {
            var settings = AgentSettingsLoader.Load(dir.FullName, Embedded);

            settings.ServerUrl.ShouldBe(new Uri("http://pi.local:8001/"));
            settings.SiteUrl.ShouldBe(new Uri("http://pi.local:8000/"));
            AgentSettingsLoader.Load(dir.FullName).ServerUrl.ShouldBe(new Uri(AgentSettingsLoader.DefaultServerUrl));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Exe yanında ayar dosyası varsa gömülü adreslerin önüne geçer.</summary>
    [Fact]
    public void Settings_FileOverridesEmbedded()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-agent-embedded-");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "agentsettings.json"), """{ "serverUrl": "http://10.0.0.5:8001/" }""");

            var settings = AgentSettingsLoader.Load(dir.FullName, Embedded);

            settings.ServerUrl.ShouldBe(new Uri("http://10.0.0.5:8001/"));
            settings.SiteUrl.ShouldBe(new Uri("http://pi.local:8000/"));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
