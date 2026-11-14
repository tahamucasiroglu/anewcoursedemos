namespace Konsept.Modules.Monitoring.Contracts.Enums;

/// <summary>
/// Günlük olayının seviyesi; Günlük şeridinde rengini belirler.
/// </summary>
public enum SystemEventLevel
{
    /// <summary>Olağan bilgi kaydı.</summary>
    Info = 0,

    /// <summary>Dikkat edilmesi gereken durum.</summary>
    Warning = 1,

    /// <summary>Hata.</summary>
    Error = 2,
}
