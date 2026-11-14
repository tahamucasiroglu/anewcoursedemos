namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Üretim işinin (bir video) genel durumu.
/// </summary>
public enum JobStatus
{
    /// <summary>Sırada bekliyor.</summary>
    Queued = 0,

    /// <summary>Çalışıyor.</summary>
    Running = 1,

    /// <summary>Durduruldu; kaldığı yerden devam edebilir.</summary>
    Paused = 2,

    /// <summary>Tamamlandı.</summary>
    Completed = 3,

    /// <summary>Hata ile bitti.</summary>
    Failed = 4,

    /// <summary>İptal edildi.</summary>
    Cancelled = 5,
}
