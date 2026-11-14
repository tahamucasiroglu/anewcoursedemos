using System.Text;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Account.Base;
using TahaKonsept.Agent.Core.Hub;
using TahaKonsept.Agent.EngineList;
using TahaKonsept.Agent.EngineList.Concrete;
using TahaKonsept.Agent.Runtime;
using TahaKonsept.Agent.Settings.Concrete;
using TahaKonsept.Agent.Startup.Base;
using TahaKonsept.Agent.Tray.Base;
using TahaKonsept.Agent.WebSite.Base;
using Xunit;

namespace TahaKonsept.Agent.Tests.Services;

/// <summary>Yardımcı servisler: başlangıç komutu, web adresi türetme, JWT hesap adı, argümanlar, motor kataloğu, ayar deposu.</summary>
public sealed class HelperServiceTests
{
    /// <summary>Test: Startup command quotes path and adds tray argument.</summary>
    [Fact]
    public void Startup_command_quotes_path_and_adds_tray_argument()
    {
        StartupRegistrationBase.BuildCommand(@"C:\Apps\TahaKonsept.Agent.exe").ShouldBe("\"C:\\Apps\\TahaKonsept.Agent.exe\" --tray");
    }

    /// <summary>Test: Launcher path prefers velopack root stub.</summary>
    [Fact]
    public void Launcher_path_prefers_velopack_root_stub()
    {
        var root = Directory.CreateTempSubdirectory("tk-agent-").FullName;
        try
        {
            var current = Directory.CreateDirectory(Path.Combine(root, "current")).FullName;
            var exe = Path.Combine(current, "TahaKonsept.Agent.exe");
            StartupRegistrationBase.ResolveLauncherPath(exe).ShouldBe(exe);

            var stub = Path.Combine(root, "TahaKonsept.Agent.exe");
            File.WriteAllText(stub, string.Empty);
            StartupRegistrationBase.ResolveLauncherPath(exe).ShouldBe(stub);

            StartupRegistrationBase.ResolveLauncherPath(@"C:\dev\bin\TahaKonsept.Agent.exe").ShouldBe(@"C:\dev\bin\TahaKonsept.Agent.exe");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Test: Web url is derived from api url.</summary>
    [Theory]
    [InlineData("http://192.168.1.102:7111", "http://192.168.1.102:7110/")]
    [InlineData("http://192.168.1.102:7111/", "http://192.168.1.102:7110/")]
    [InlineData("https://pi.local:8443", "https://pi.local:8443/")]
    [InlineData("bozuk", "http://localhost:7110/")]
    public void Web_url_is_derived_from_api_url(string api, string expected)
    {
        WebSiteLocatorBase.DeriveFromServerUrl(api).ShouldBe(expected);
    }

    /// <summary>Test: Account name is read from jwt name claim then sub.</summary>
    [Fact]
    public void Account_name_is_read_from_jwt_name_claim_then_sub()
    {
        PairedAccountReaderBase.ReadNameFromJwt(Jwt("{\"sub\":\"42\",\"name\":\"taha\"}")).ShouldBe("taha");
        PairedAccountReaderBase.ReadNameFromJwt(Jwt("{\"sub\":\"42\"}")).ShouldBe("42");
        PairedAccountReaderBase.ReadNameFromJwt("bozuk").ShouldBeNull();
        PairedAccountReaderBase.ReadNameFromJwt("a.!!!.c").ShouldBeNull();
        PairedAccountReaderBase.ReadNameFromJwt(null).ShouldBeNull();
    }

    /// <summary>Test: Launch options detect tray argument.</summary>
    [Fact]
    public void Launch_options_detect_tray_argument()
    {
        DesktopLaunchOptions.Parse(["--tray"]).StartedFromLogin.ShouldBeTrue();
        DesktopLaunchOptions.Parse(["--TRAY"]).StartedFromLogin.ShouldBeTrue();
        DesktopLaunchOptions.Parse([]).StartedFromLogin.ShouldBeFalse();
        DesktopLaunchOptions.Parse(null).StartedFromLogin.ShouldBeFalse();
    }

    /// <summary>Test: Tray tooltip shows connection state.</summary>
    [Fact]
    public void Tray_tooltip_shows_connection_state()
    {
        TrayIconControllerBase.BuildToolTip(HubConnectionStatus.Connected).ShouldBe("Taha Konsept — Sunucuya bağlı");
        TrayIconControllerBase.BuildToolTip(HubConnectionStatus.Unauthorized).ShouldContain("Eşleme");
    }

    /// <summary>Test: Engine catalog groups orders and deduplicates.</summary>
    [Fact]
    public void Engine_catalog_groups_orders_and_deduplicates()
    {
        var video = new[] { Engine<IVideoEngine>("remotion", "Remotion"), Engine<IVideoEngine>("hyperframes", "HyperFrames") };
        var speech = new[] { Engine<ISpeechEngine>("supertonic-3", "Supertonic"), Engine<ISpeechEngine>("hyperframes", "Kopya") };
        var image = new[] { Engine<IImageEngine>("z-image-turbo", "Z-Image Turbo") };

        var list = new RegisteredEngineCatalog(video, speech, image).GetEngines();

        list.Select(e => e.Engine.Key).ShouldBe(["hyperframes", "remotion", "supertonic-3", "z-image-turbo"]);
        list.Select(e => e.Category).ShouldBe([EngineCategory.Video, EngineCategory.Video, EngineCategory.Speech, EngineCategory.Image]);
    }

    /// <summary>Test: Desktop settings round trip through json file.</summary>
    [Fact]
    public void Desktop_settings_round_trip_through_json_file()
    {
        var root = Directory.CreateTempSubdirectory("tk-agent-").FullName;
        try
        {
            var paths = Substitute.For<IAgentPaths>();
            paths.ConfigDir.Returns(root);
            var store = new JsonDesktopAppSettingsStore(paths);
            store.Current.SetupCompleted.ShouldBeFalse();

            store.Update(s => s with { SetupCompleted = true, WebBaseUrl = "http://pi:7110/" });

            var reloaded = new JsonDesktopAppSettingsStore(paths);
            reloaded.Current.SetupCompleted.ShouldBeTrue();
            reloaded.Current.WebBaseUrl.ShouldBe("http://pi:7110/");
            File.Exists(Path.Combine(root, JsonDesktopAppSettingsStore.FileName)).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static T Engine<T>(string key, string name)
        where T : class, IAgentEngine
    {
        var engine = Substitute.For<T>();
        engine.Key.Returns(key);
        engine.DisplayName.Returns(name);
        return engine;
    }

    private static string Jwt(string payloadJson)
    {
        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return B64("{\"alg\":\"none\"}") + "." + B64(payloadJson) + ".sig";
    }
}
