using Konsept.Agent.Application.Base;
using Konsept.Agent.Infrastructure.Concrete;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Kurulum kaydı ve kaldırma (spec 0023 AC-10, AC-11, AC-12; B-77): geçici klasörde sahte "LOCALAPPDATA", sahte kayıt defteri/kısayol/süreç.
/// Gerçek kayıt defterine, Başlat menüsüne ya da bu PC'deki kurulu ajana dokunulmaz.
/// </summary>
public sealed class AgentInstallationTests : IDisposable
{
    private readonly DirectoryInfo _temp = Directory.CreateTempSubdirectory("anewconcept-install-");
    private readonly RecordingInstallHost _host = new();
    private readonly AgentInstallation _installation;

    /// <summary>Initializes a new instance of the <see cref="AgentInstallationTests"/> class.</summary>
    public AgentInstallationTests()
    {
        _installation = new AgentInstallation(Path.Combine(_temp.FullName, "LocalAppData", "anewconcept"), Path.Combine(_temp.FullName, "Başlat"), _host);
    }

    private string VideoFolder => Path.Combine(_temp.FullName, "Videolar", "anewconcept");

    /// <inheritdoc />
    public void Dispose() => _temp.Delete(recursive: true);

    /// <summary>
    /// AC-10, B-77: indirilen exe kurulum yerine kopyalanır; yalnızca kullanıcıya ait (HKCU) başlangıç ve "Uygulamalar" kaydı ("ANEW Konsept",
    /// kaldırma komutu <c>--kaldir</c>) ve kullanıcının Başlat menüsüne kısayol yazılır — yönetici izni gerektiren yer yok. Kurulu kopyadan
    /// açılınca yeniden kopyalanmaz.
    /// </summary>
    [Fact]
    public void Installation_OnlyCurrentUserRegistryAndLocalAppData()
    {
        var downloaded = Path.Combine(_temp.FullName, "İndirilenler", "anewconcept-ajan-0.5.0-win-x64.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(downloaded)!);
        File.WriteAllText(downloaded, "ajan");

        _installation.EnsureInstalled(downloaded, "0.5.0").ShouldBeTrue();

        File.ReadAllText(_installation.InstalledExe).ShouldBe("ajan");
        _installation.InstalledExe.ShouldStartWith(_installation.InstallRoot);
        var quoted = "\"" + _installation.InstalledExe + "\"";
        _host.Values[AgentInstallationBase.RunKey + "|anewconcept"].ShouldBe(quoted);
        _host.Values[AgentInstallationBase.UninstallKey + "|DisplayName"].ShouldBe("ANEW Konsept");
        _host.Values[AgentInstallationBase.UninstallKey + "|DisplayVersion"].ShouldBe("0.5.0");
        _host.Values[AgentInstallationBase.UninstallKey + "|UninstallString"].ShouldBe(quoted + " --kaldir");
        _host.Values.Keys.ShouldAllBe(k => k.StartsWith(@"Software\Microsoft\Windows\CurrentVersion\", StringComparison.Ordinal));
        File.Exists(_installation.ShortcutPath).ShouldBeTrue();
        _installation.ShortcutPath.ShouldStartWith(Path.Combine(_temp.FullName, "Başlat"));

        _host.Calls.Clear();
        _installation.EnsureInstalled(_installation.InstalledExe, "0.5.0").ShouldBeFalse();
        _host.Calls.ShouldNotContain(c => c.StartsWith("shortcut", StringComparison.Ordinal)); // kısayol zaten var
    }

    /// <summary>
    /// AC-11: kaldırmada varsayılan Hayır — ajan önce kapatılır; kurulum yeri, kısayol, başlangıç ve kaldırma kaydı silinir; video klasörü ve
    /// videolar yerinde kalır.
    /// </summary>
    [Fact]
    public void Uninstall_DefaultNo_KeepsVideos_RemovesInstallAndRegistry()
    {
        Install();
        Directory.CreateDirectory(VideoFolder);
        File.WriteAllText(Path.Combine(VideoFolder, "video-1.mp4"), "v");
        _host.Calls.Clear();

        _installation.Uninstall(deleteVideos: false, VideoFolder).ShouldBeTrue();

        Directory.Exists(_installation.InstallRoot).ShouldBeFalse();
        File.Exists(_installation.ShortcutPath).ShouldBeFalse();
        _host.Values.ShouldBeEmpty();
        File.Exists(Path.Combine(VideoFolder, "video-1.mp4")).ShouldBeTrue();
        _host.Calls[0].ShouldBe("stop " + _installation.InstalledExe); // ajan çalışıyorsa önce kapatılır
        _host.Calls.ShouldContain($"delete-value {AgentInstallationBase.RunKey}|anewconcept");
        _host.Calls.ShouldContain($"delete-key {AgentInstallationBase.UninstallKey}");
    }

    /// <summary>AC-11: "Evet" ile videolar silinir; klasörde kullanıcının başka dosyası varsa yalnızca videolar silinir, klasör kalır.</summary>
    [Fact]
    public void Uninstall_Yes_DeletesVideos()
    {
        Install();
        Directory.CreateDirectory(VideoFolder);
        File.WriteAllText(Path.Combine(VideoFolder, "video-1.mp4"), "v");
        File.WriteAllText(Path.Combine(VideoFolder, "video-2.mp4"), "v");

        _installation.Uninstall(deleteVideos: true, VideoFolder).ShouldBeTrue();
        Directory.Exists(VideoFolder).ShouldBeFalse();

        Install();
        Directory.CreateDirectory(VideoFolder);
        File.WriteAllText(Path.Combine(VideoFolder, "video-3.mp4"), "v");
        File.WriteAllText(Path.Combine(VideoFolder, "notlarım.txt"), "kullanıcının");
        _installation.Uninstall(deleteVideos: true, VideoFolder).ShouldBeTrue();
        Directory.GetFiles(VideoFolder).Select(Path.GetFileName).ShouldBe(["notlarım.txt"]);
    }

    /// <summary>
    /// AC-12: kaldırma, kurulum yerinin yanındaki başka sürüm Node/Python klasörlerine, "Konsept" adlı başka programa ve ortam ayarlarına (PATH)
    /// dokunmaz; kurulum yerini içeren bir klasör video klasörü olarak verilse bile silinmez.
    /// </summary>
    [Fact]
    public void Uninstall_LeavesOtherFoldersUntouched()
    {
        Install();
        var local = Path.GetDirectoryName(_installation.InstallRoot)!;
        var others = new[]
        {
            Path.Combine(local, "Programs", "nodejs", "node.exe"),
            Path.Combine(local, "Programs", "Python", "Python312", "python.exe"),
            Path.Combine(local, "Konsept", "konsept.exe"),
            Path.Combine(_temp.FullName, "Konsept", "ayarlar.json"),
        };
        foreach (var file in others)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "başka program");
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

        _installation.Uninstall(deleteVideos: true, local).ShouldBeTrue();

        foreach (var file in others)
        {
            File.ReadAllText(file).ShouldBe("başka program");
        }

        Environment.GetEnvironmentVariable("PATH").ShouldBe(path);
        Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User).ShouldBe(userPath);
        Directory.Exists(_installation.InstallRoot).ShouldBeFalse();
        _host.Calls.ShouldAllBe(c => !c.Contains("Environment", StringComparison.OrdinalIgnoreCase));
    }

    private void Install()
    {
        var exe = Path.Combine(_temp.FullName, "anewconcept-ajan-0.5.0-win-x64.exe");
        File.WriteAllText(exe, "ajan");
        _installation.EnsureInstalled(exe, "0.5.0");
        foreach (var folder in new[] { "components\\node", "logs" })
        {
            Directory.CreateDirectory(Path.Combine(_installation.InstallRoot, folder));
            File.WriteAllText(Path.Combine(_installation.InstallRoot, folder, "x.txt"), "x");
        }
    }
}
