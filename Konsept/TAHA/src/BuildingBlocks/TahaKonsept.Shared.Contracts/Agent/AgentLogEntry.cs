using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın sunucuya ilettiği günlük kaydı (<c>IAgentHub.Log</c>); alt Günlük şeridinde ve diagnostics'te görünür.
/// </summary>
public sealed record AgentLogEntry
{
    /// <summary>Kayıt zamanı (UTC).</summary>
    public DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Seviye.</summary>
    public LogLevelKind Level { get; init; } = LogLevelKind.Information;

    /// <summary>Kategori / kaynak (ör. <c>Speech</c>, <c>Installer</c>).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Mesaj.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>İstisna metni (varsa).</summary>
    public string? Exception { get; init; }

    /// <summary>İlgili üretim işi (varsa).</summary>
    public Guid? JobId { get; init; }
}
