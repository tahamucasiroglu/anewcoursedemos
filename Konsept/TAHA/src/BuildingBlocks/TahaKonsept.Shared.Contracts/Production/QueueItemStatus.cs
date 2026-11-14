namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Üretim sırasındaki bir öğenin durumu.
/// </summary>
public enum QueueItemStatus
{
    /// <summary>Sırada bekliyor.</summary>
    Waiting = 0,

    /// <summary>Üretiliyor.</summary>
    Running = 1,

    /// <summary>Durduruldu; devam edince kaldığı yerden sürer.</summary>
    Paused = 2,

    /// <summary>Video hazır.</summary>
    Done = 3,

    /// <summary>Hata ile bitti.</summary>
    Failed = 4,

    /// <summary>İptal edildi / sıradan çıkarıldı.</summary>
    Cancelled = 5,
}
