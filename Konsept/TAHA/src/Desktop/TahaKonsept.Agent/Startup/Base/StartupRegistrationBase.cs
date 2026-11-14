using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Startup.Base;

/// <summary>
/// Başlangıç kaydı tabanı: başlatma komutunu üretir (<see cref="BuildCommand"/>; Velopack kurulumunda sürüm klasörü yerine
/// kök dizindeki kalıcı başlatıcı exe kullanılır, <see cref="ResolveLauncherPath"/>) ve okuma/yazma/silme işini türetilen sınıfa bırakır.
/// </summary>
public abstract class StartupRegistrationBase : IStartupRegistration
{
    /// <summary>Oturum açılışında pencere açmadan başlatma argümanı.</summary>
    public const string TrayArgument = "--tray";

    /// <summary>Kayıt değeri adı.</summary>
    public const string ValueName = "TahaKonsept.Agent";

    /// <inheritdoc />
    public bool IsEnabled => !string.IsNullOrWhiteSpace(ReadValue());

    /// <inheritdoc />
    public Result SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                var processPath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(processPath))
                {
                    return Result.Failure(Error.Failure("startup.no_path", "Uygulama yolu bulunamadı."));
                }

                WriteValue(BuildCommand(ResolveLauncherPath(processPath)));
            }
            else
            {
                DeleteValue();
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            return Result.Failure(Error.Failure("startup.write_failed", "Başlangıç ayarı kaydedilemedi: " + ex.Message));
        }
    }

    /// <summary>
    /// Velopack kurulumunda exe <c>kök\current\X.exe</c> altındadır; kökte aynı adlı kalıcı başlatıcı varsa o döner
    /// (güncellemeden sonra da geçerli kalır). Aksi halde verilen yol aynen döner.
    /// </summary>
    /// <param name="processPath">Çalışan exe yolu.</param>
    /// <returns>Başlangıçta çalıştırılacak exe yolu.</returns>
    public static string ResolveLauncherPath(string processPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processPath);
        var dir = Path.GetDirectoryName(processPath);
        if (dir is null || !string.Equals(Path.GetFileName(dir), "current", StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        var root = Path.GetDirectoryName(dir);
        if (root is null)
        {
            return processPath;
        }

        var launcher = Path.Combine(root, Path.GetFileName(processPath));
        return File.Exists(launcher) ? launcher : processPath;
    }

    /// <summary>Kayıt komut satırını üretir: tırnaklı exe yolu + <see cref="TrayArgument"/>.</summary>
    /// <param name="exePath">Exe yolu.</param>
    /// <returns>Komut satırı.</returns>
    public static string BuildCommand(string exePath) => "\"" + exePath + "\" " + TrayArgument;

    /// <summary>Kayıtlı komutu okur (yoksa <c>null</c>).</summary>
    /// <returns>Komut ya da <c>null</c>.</returns>
    protected abstract string? ReadValue();

    /// <summary>Komutu yazar.</summary>
    /// <param name="command">Komut satırı.</param>
    protected abstract void WriteValue(string command);

    /// <summary>Kaydı siler.</summary>
    protected abstract void DeleteValue();
}
