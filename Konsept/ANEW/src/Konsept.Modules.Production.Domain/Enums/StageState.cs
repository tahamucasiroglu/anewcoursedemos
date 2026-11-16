namespace Konsept.Modules.Production.Domain.Enums;

/// <summary>Aşama durumu (spec 0017 Req 6): bekliyor · çalışıyor · tamamlandı · atlandı · hata.</summary>
public enum StageState
{
    /// <summary>Bekliyor.</summary>
    Pending = 0,

    /// <summary>Çalışıyor.</summary>
    Running = 1,

    /// <summary>Tamamlandı.</summary>
    Done = 2,

    /// <summary>Atlandı (görsel/ses modeli kapalı).</summary>
    Skipped = 3,

    /// <summary>Hata.</summary>
    Failed = 4,
}
