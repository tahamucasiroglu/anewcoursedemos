namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Görsel / ses üretim kipi: kapalı, yerel (masaüstü ajanı) ya da online (API sağlayıcısı).
/// </summary>
public enum MediaMode
{
    /// <summary>Kapalı (görsel yok: koddan çizim; ses yok: sessiz video).</summary>
    Off = 0,

    /// <summary>Yerel model; masaüstü ajanında çalışır.</summary>
    Local = 1,

    /// <summary>Online sağlayıcı; API'de çalışır, sonucu ajan indirir.</summary>
    Online = 2,
}
