using TahaKonsept.Agent.Startup.Base;

namespace TahaKonsept.Agent.Runtime;

/// <summary>Komut satırından okunan başlatma seçenekleri.</summary>
/// <param name="StartedFromLogin">Windows oturum açılışında (<c>--tray</c>) mı başlatıldı; öyleyse kurulum gerekmedikçe pencere açılmaz.</param>
public sealed record DesktopLaunchOptions(bool StartedFromLogin)
{
    /// <summary>Argümanlardan seçenekleri üretir.</summary>
    /// <param name="args">Komut satırı argümanları.</param>
    /// <returns>Seçenekler.</returns>
    public static DesktopLaunchOptions Parse(IReadOnlyList<string>? args) =>
        new(args?.Any(a => string.Equals(a, StartupRegistrationBase.TrayArgument, StringComparison.OrdinalIgnoreCase)) == true);
}
