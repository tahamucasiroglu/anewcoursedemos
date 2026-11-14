using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Pi'deki Web ve Api loglarını yalnızca <c>/opt/anewconcept</c> altına yazar (spec 0008 AC-5, Pi kısmı): servis
/// dosyalarındaki log klasörü ayarı ve kurulum betiğinin klasörleri.
/// </summary>
public sealed class DeployLogDirectoryTests
{
    /// <summary>Her iki servis dosyası log klasörünü <c>/opt/anewconcept/logs/&lt;uygulama&gt;</c> olarak verir.</summary>
    /// <param name="app">Uygulama.</param>
    [Theory]
    [InlineData("api")]
    [InlineData("web")]
    public void DeployUnit_LogDirectory_UnderOptAnewconcept(string app)
    {
        var unit = File.ReadAllLines(Path.Combine(RepositoryRoot(), "deploy", "pi", $"anewconcept-{app}.service"));

        unit.Where(l => l.StartsWith("Environment=Logging__Directory=", StringComparison.Ordinal))
            .ShouldHaveSingleItem()
            .ShouldBe($"Environment=Logging__Directory=/opt/anewconcept/logs/{app}");
        string.Join('\n', unit).ShouldNotContain("/var/log");
    }

    /// <summary>Kurulum betiği log klasörlerini <c>/opt/anewconcept/logs</c> altında oluşturur, başka log klasörü açmaz.</summary>
    [Fact]
    public void Provision_CreatesLogFoldersOnlyUnderOpt()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "deploy", "pi", "provision.sh"));

        script.ShouldContain("install -d -o anewconcept -g anewconcept -m 750 /opt/anewconcept/logs /opt/anewconcept/logs/api /opt/anewconcept/logs/web");
        script.ShouldNotContain("install -d -o anewconcept -g anewconcept -m 750 /var/log");
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Konsept.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Konsept.sln bulunamadı.");
    }
}
