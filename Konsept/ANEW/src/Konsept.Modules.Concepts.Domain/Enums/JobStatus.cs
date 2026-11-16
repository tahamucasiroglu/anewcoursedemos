namespace Konsept.Modules.Concepts.Domain.Enums;

/// <summary>Üretim işinin durumu (spec 0014 Req 17, 19, 21).</summary>
public enum JobStatus
{
    /// <summary>Sürüyor.</summary>
    Running,

    /// <summary>Bitti (kısmi hatalı olabilir).</summary>
    Completed,

    /// <summary>Kullanıcı durdurdu.</summary>
    Stopped,

    /// <summary>Hiçbir şey üretilemedi.</summary>
    Failed,
}
