namespace Konsept.Modules.Monitoring.Contracts.Enums;

/// <summary>Teknik logun kaynak uygulaması.</summary>
public enum LogEntrySource
{
    /// <summary>Web uygulaması.</summary>
    Web = 0,

    /// <summary>Api uygulaması.</summary>
    Api = 1,

    /// <summary>Masaüstü ajanı.</summary>
    Agent = 2,
}
