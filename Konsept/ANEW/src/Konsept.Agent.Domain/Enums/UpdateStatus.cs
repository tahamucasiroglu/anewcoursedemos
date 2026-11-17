namespace Konsept.Agent.Domain.Enums;

/// <summary>Güncelleme ekranının durumu (spec 0023 UX: güncelleniyor · bileşen hatası · tamamlandı · doğrulanamadı).</summary>
public enum UpdateStatus
{
    /// <summary>Boşta.</summary>
    Idle = 0,

    /// <summary>Yeni sürüm hazır; kullanıcı onayı bekleniyor.</summary>
    Available = 1,

    /// <summary>"Üretiliyor" iş var; iş bitince ya da durdurulunca kendiliğinden başlayacak.</summary>
    WaitingForProduction = 2,

    /// <summary>İndiriliyor.</summary>
    Downloading = 3,

    /// <summary>Doğrulanıyor (SHA-256).</summary>
    Verifying = 4,

    /// <summary>Kuruluyor (dosya değiş-tokuşu).</summary>
    Installing = 5,

    /// <summary>Güncelleme tamamlandı; yeni sürüm başlatılıyor.</summary>
    Completed = 6,

    /// <summary>Bileşen hatası (indirme, disk); Tekrar dene / Hatayı bildir.</summary>
    Failed = 7,

    /// <summary>Doğrulama başarısız; mevcut sürümle devam ediliyor.</summary>
    VerificationFailed = 8,
}
