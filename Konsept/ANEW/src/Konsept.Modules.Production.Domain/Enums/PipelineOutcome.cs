namespace Konsept.Modules.Production.Domain.Enums;

/// <summary>Bir işin hat koşusunun sonucu (spec 0017): tamamlandı · hata · durduruldu · ajan bekleniyor.</summary>
public enum PipelineOutcome
{
    /// <summary>Tamamlandı (video kaydı oluştu).</summary>
    Completed = 0,

    /// <summary>Hata.</summary>
    Failed = 1,

    /// <summary>Durdur ile kesildi.</summary>
    Stopped = 2,

    /// <summary>Ajan bekleniyor.</summary>
    Waiting = 3,

    /// <summary>Aşama tamamlandı, hat sürer.</summary>
    Continue = 4,
}
