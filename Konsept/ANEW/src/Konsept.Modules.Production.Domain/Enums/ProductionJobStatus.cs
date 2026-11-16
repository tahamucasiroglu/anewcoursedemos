namespace Konsept.Modules.Production.Domain.Enums;

/// <summary>
/// Üretim işinin durumu — tek kaynak (spec 0006 Req 13, Ç-4/Ç-5): Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata · Tamamlandı.
/// Ajan bekleniyor, Hata ve Tamamlandı geçişlerini 0017 tetikler.
/// </summary>
public enum ProductionJobStatus
{
    /// <summary>Sırada.</summary>
    Queued = 0,

    /// <summary>Üretiliyor.</summary>
    Producing = 1,

    /// <summary>Durduruldu.</summary>
    Stopped = 2,

    /// <summary>Ajan bekleniyor.</summary>
    WaitingForAgent = 3,

    /// <summary>Hata.</summary>
    Failed = 4,

    /// <summary>Tamamlandı (sıradan kalkar).</summary>
    Completed = 5,
}
