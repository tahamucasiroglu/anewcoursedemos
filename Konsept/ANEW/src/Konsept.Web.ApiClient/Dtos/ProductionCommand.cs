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

    /// <summary>Üretilen Videolar: videonun sürümleri (spec 0007).</summary>
    ProducedVideo,

    /// <summary>Üretilen Videolar: Klasörde aç / Sistem oynatıcısında aç (spec 0007).</summary>
    OpenFile,

    /// <summary>Üretilen Videolar: Sil — videoyu tüm sürümleriyle Silinenler'e gönder (kimlik: video; spec 0019).</summary>
    DeleteVideo,

    /// <summary>Silinenler — Videolar: Kalıcı sil (kimlik: video; spec 0019).</summary>
    PurgeVideo,

    /// <summary>Yeni sürüm önizlemesi (kimlik: video; spec 0020).</summary>
    VersionPreview,

    /// <summary>Yeni sürümü başlat (kimlik: video; spec 0020).</summary>
    StartVersion,

    /// <summary>Yeniden üretimi başlat (kimlik: video; spec 0020).</summary>
    StartRegeneration,

    /// <summary>Hata veren sürümü tekrar dene (kimlik: video; spec 0020).</summary>
    RetryVersion,

    /// <summary>Hata veren sürümü kaldır (kimlik: video; spec 0020).</summary>
    RemoveFailedVersion,
}
