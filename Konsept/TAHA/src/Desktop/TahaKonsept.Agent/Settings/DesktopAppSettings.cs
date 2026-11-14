namespace TahaKonsept.Agent.Settings;

/// <summary>
/// Masaüstü uygulamasına özel ayarlar (<c>Config/desktop-settings.json</c>); sunucu adresi ise çekirdeğin <c>AgentSettings</c>'indedir.
/// Önemli üyeler: <see cref="WebBaseUrl"/> (boşsa sunucu adresinden türetilir), <see cref="SetupCompleted"/>,
/// <see cref="StartupDefaultApplied"/> (ilk açılışta "Windows ile başlat" bir kez açılır).
/// </summary>
public sealed record DesktopAppSettings
{
    /// <summary>Web arayüzü adresi ("Siteye git"); boşsa API adresinin portu 7111 → 7110 yapılarak türetilir.</summary>
    public string? WebBaseUrl { get; init; }

    /// <summary>İlk kurulum (ön gereksinimler + eşleme) bitirildi mi.</summary>
    public bool SetupCompleted { get; init; }

    /// <summary>"Windows ile başlat" varsayılanı (açık) bir kez uygulandı mı; kullanıcı kapattıysa tekrar açılmaz.</summary>
    public bool StartupDefaultApplied { get; init; }
}
