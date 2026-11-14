using System.Reflection;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Settings;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan bilgisi ve ayar dosyası.
/// </summary>
public sealed class AgentInfrastructureTests
{
    /// <summary>Ajan versiyonu sabit metin değil, derleme bilgisinden (tek ürün versiyonu) okunur.</summary>
    [Fact]
    public void AgentInfo_Version_ReadFromAssembly()
    {
        var assembly = typeof(App).Assembly;
        var info = new AssemblyAgentInfoProvider(assembly);

        info.Version.ShouldBe(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]);
        info.Version.ShouldBe(assembly.GetName().Version!.ToString(3));
        info.MachineName.ShouldBe(Environment.MachineName);
    }

    /// <summary>Yerel ayar dosyası temel dosyanın üzerine yazar; adreslere eğik çizgi eklenir.</summary>
    [Fact]
    public void SettingsLoader_LocalFile_OverridesBase()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-agent-settings-");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "agentsettings.json"), """{ "serverUrl": "http://localhost:8001", "siteUrl": "http://localhost:8000/", "reconnectDelaySeconds": 3 }""");
            File.WriteAllText(Path.Combine(dir.FullName, "agentsettings.Local.json"), """{ "serverUrl": "http://192.168.1.10:8001" }""");

            var settings = AgentSettingsLoader.Load(dir.FullName);

            settings.ServerUrl.ShouldBe(new Uri("http://192.168.1.10:8001/"));
            settings.SiteUrl.ShouldBe(new Uri("http://localhost:8000/"));
            settings.ReconnectDelay.ShouldBe(TimeSpan.FromSeconds(3));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Tarayıcı açıcı yalnızca http/https adreslerini kabul eder.</summary>
    [Fact]
    public void BrowserLauncher_NonHttpAddress_IsRejected() =>
        Should.Throw<ArgumentException>(() => new ShellBrowserLauncher().Open(new Uri("file:///C:/Windows/notepad.exe")));
}
