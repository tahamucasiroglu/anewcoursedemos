namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>Teknik logun kaynak uygulaması (spec 0008 Req 1).</summary>
public enum LogSource
{
    /// <summary>Web uygulaması.</summary>
    Web = 0,

    /// <summary>Api uygulaması.</summary>
    Api = 1,

    /// <summary>Masaüstü ajanı.</summary>
    Agent = 2,
}
