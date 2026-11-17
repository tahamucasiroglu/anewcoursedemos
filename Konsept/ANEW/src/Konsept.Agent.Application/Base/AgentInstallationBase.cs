using System.Text.Json;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// <see cref="IAgentInstallation"/> mantığı (spec 0023 Req 9–11, B-77). Her şey kurulum yerinin (<c>%LOCALAPPDATA%\anewconcept</c>) altındadır:
/// <c>app\anewconcept-ajan.exe</c> (çalışan sürüm), <c>app\previous\</c> (tek önceki sürüm), <c>update-staging\</c>, <c>update-state.json</c>.
/// Kayıtlar yalnızca HKCU'dadır (başlangıç: <c>Run</c>; "Uygulamalar": <c>Uninstall\anewconcept</c>) — yönetici izni gerekmez (Req 10).
/// Kaldırma başka klasörlere, Node/Python/FFmpeg kurulumlarına ya da ortam değişkenlerine dokunmaz (Req 11, AC-12).
/// </summary>
public abstract class AgentInstallationBase : IAgentInstallation
{
    /// <summary>Başlangıç kaydının anahtarı (HKCU).</summary>
    public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>"Uygulamalar" kaydının anahtarı (HKCU).</summary>
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\anewconcept";

    /// <summary>Kayıtlarda görünen ad.</summary>
    public const string DisplayName = "ANEW Konsept";

    /// <summary>Başlangıç kaydının değer adı.</summary>
    public const string RunValueName = "anewconcept";

    /// <summary>Kaldırma argümanı ("Uygulamalar" → Kaldır).</summary>
    public const string UninstallArgument = "--kaldir";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly IInstallHost _host;
    private readonly string _startMenuDirectory;

    /// <summary>Initializes a new instance of the <see cref="AgentInstallationBase"/> class.</summary>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <param name="startMenuDirectory">Kullanıcının Başlat menüsü "Programlar" klasörü.</param>
    /// <param name="host">Kayıt defteri, kısayol, süreç.</param>
    protected AgentInstallationBase(string installRoot, string startMenuDirectory, IInstallHost host)
    {
        InstallRoot = installRoot ?? throw new ArgumentNullException(nameof(installRoot));
        _startMenuDirectory = startMenuDirectory ?? throw new ArgumentNullException(nameof(startMenuDirectory));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <inheritdoc />
    public string InstallRoot { get; }

    /// <inheritdoc />
    public string InstalledExe => Path.Combine(AppDirectory, UpdateRules.InstalledFileName);

    /// <inheritdoc />
    public string StagingDirectory => Path.Combine(InstallRoot, "update-staging");

    /// <summary>Gets Başlat menüsü kısayolu.</summary>
    public string ShortcutPath => Path.Combine(_startMenuDirectory, DisplayName + ".lnk");

    /// <summary>Gets saklanan önceki sürümün exe'si.</summary>
    public string PreviousExe => Path.Combine(AppDirectory, "previous", UpdateRules.InstalledFileName);

    private string AppDirectory => Path.Combine(InstallRoot, "app");

    private string FailedExe => Path.Combine(AppDirectory, "failed-" + UpdateRules.InstalledFileName);

    private string StatePath => Path.Combine(InstallRoot, "update-state.json");

    /// <inheritdoc />
    public UpdateState LoadState()
    {
        try
        {
            return File.Exists(StatePath) ? JsonSerializer.Deserialize<UpdateState>(File.ReadAllText(StatePath), Json) ?? new UpdateState() : new UpdateState();
        }
        catch (JsonException)
        {
            return new UpdateState(); // Bozuk durum dosyası güncellemeyi kilitlemez.
        }
    }

    /// <inheritdoc />
    public void SaveState(UpdateState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Directory.CreateDirectory(InstallRoot);
        var temporary = StatePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, Json));
        File.Move(temporary, StatePath, overwrite: true);
    }

    /// <inheritdoc />
    public void Swap(string stagedExe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagedExe);
        TryDeleteFile(FailedExe); // Önceki geri dönüşün kalıntısı (review r1 L-2).
        Directory.CreateDirectory(Path.GetDirectoryName(PreviousExe)!);
        if (File.Exists(PreviousExe))
        {
            File.Delete(PreviousExe); // Yalnızca bir önceki sürüm saklanır (Req 9).
        }

        // Çalışan exe silinemez ama yeniden adlandırılabilir: önce "önceki" olur, sonra yeni exe yerine geçer.
        if (File.Exists(InstalledExe))
        {
            File.Move(InstalledExe, PreviousExe);
        }

        File.Move(stagedExe, InstalledExe);
    }

    /// <inheritdoc />
    public bool RollBack()
    {
        if (!File.Exists(PreviousExe))
        {
            return false;
        }

        var failed = FailedExe;
        TryDeleteFile(failed);
        if (File.Exists(InstalledExe))
        {
            File.Move(InstalledExe, failed, overwrite: true);
        }

        File.Move(PreviousExe, InstalledExe);
        TryDeleteFile(failed); // Başarısız sürüm hâlâ çalışıyorsa silinemez; sonraki güncelleme/kaldırma temizler.
        return true;
    }

    /// <inheritdoc />
    public void LaunchInstalled(string arguments) => _host.Launch(InstalledExe, arguments ?? string.Empty);

    /// <inheritdoc />
    public bool EnsureInstalled(string runningExe, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runningExe);
        var copied = false;
        if (!SamePath(runningExe, InstalledExe))
        {
            Directory.CreateDirectory(AppDirectory);
            File.Copy(runningExe, InstalledExe, overwrite: true);
            copied = true;
        }

        var quoted = "\"" + InstalledExe + "\"";
        _host.SetUserValue(RunKey, RunValueName, quoted);
        _host.SetUserValue(UninstallKey, "DisplayName", DisplayName);
        _host.SetUserValue(UninstallKey, "DisplayVersion", version ?? string.Empty);
        _host.SetUserValue(UninstallKey, "Publisher", DisplayName);
        _host.SetUserValue(UninstallKey, "DisplayIcon", InstalledExe);
        _host.SetUserValue(UninstallKey, "InstallLocation", InstallRoot);
        _host.SetUserValue(UninstallKey, "UninstallString", quoted + " " + UninstallArgument);
        _host.SetUserValue(UninstallKey, "NoModify", 1);
        _host.SetUserValue(UninstallKey, "NoRepair", 1);
        if (copied || !File.Exists(ShortcutPath))
        {
            _host.CreateShortcut(ShortcutPath, InstalledExe);
        }

        return copied;
    }

    /// <inheritdoc />
    public bool Uninstall(bool deleteVideos, string? videoFolder)
    {
        _host.StopRunning(InstalledExe); // Ajan çalışıyorsa önce kapatılır.
        _host.DeleteUserValue(RunKey, RunValueName);
        _host.DeleteUserKey(UninstallKey);
        var ok = TryDeleteFile(ShortcutPath);
        ok &= TryDeleteDirectory(InstallRoot);
        if (deleteVideos && SetupRules.IsValidVideoFolder(videoFolder, InstallRoot) && Directory.Exists(videoFolder))
        {
            // Video klasöründeki .mp4 videolar silinir; klasör boş kalırsa o da. Kullanıcının başka türden dosyalarına dokunulmaz
            // (klasördeki ajan dışı .mp4'leri ayırt etmek B-136, review r1 L-3).
            foreach (var video in Directory.EnumerateFiles(videoFolder, "*.mp4", SearchOption.TopDirectoryOnly))
            {
                ok &= TryDeleteFile(video);
            }

            if (!Directory.EnumerateFileSystemEntries(videoFolder).Any())
            {
                ok &= TryDeleteDirectory(videoFolder);
            }
        }

        return ok;
    }

    private static bool SamePath(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private static bool TryDeleteFile(string file)
    {
        try
        {
            File.Delete(file);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryDeleteDirectory(string directory)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(200 * (attempt + 1)); // Kapatılan ajanın dosya kilitleri bırakılırken.
            }
        }

        return false;
    }
}
