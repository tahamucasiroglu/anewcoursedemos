namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Konseptten Video komutları (spec 0006); her biri Api'de bir <c>api/production/…</c> ucuna karşılık gelir.</summary>
public enum ProductionCommand
{
    /// <summary>Ayarları kaydet (kimlik: profil).</summary>
    SaveSettings,

    /// <summary>Üretim sırasına al.</summary>
    Enqueue,

    /// <summary>Sırayla üret.</summary>
    Start,

    /// <summary>Durdur.</summary>
    Stop,

    /// <summary>Yukarı/Aşağı (kimlik: iş).</summary>
    Move,

    /// <summary>Sıradan çıkar (kimlik: iş).</summary>
    Remove,

    /// <summary>Kaldığı yerden devam et (kimlik: iş).</summary>
    Resume,

    /// <summary>Üretilen videolar (kimlik: profil; spec 0017).</summary>
    Videos,

    /// <summary>Videonun yerel akış adresi (kimlik: video; spec 0017).</summary>
    Stream,
}
