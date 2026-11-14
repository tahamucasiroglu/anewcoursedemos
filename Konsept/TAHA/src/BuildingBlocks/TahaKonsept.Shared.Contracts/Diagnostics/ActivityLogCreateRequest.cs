using System.Text.Json;

namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// İstemci tarafı (Web / ajan) olayını günlüğe yazma isteği: <c>POST api/diagnostics/activity-logs</c>.
/// </summary>
public sealed record ActivityLogCreateRequest
{
    /// <summary>Seviye.</summary>
    public LogLevelKind Level { get; init; } = LogLevelKind.Information;

    /// <summary>Kaynak (boşsa <c>Web</c>; en çok 64 karakter).</summary>
    public string? Source { get; init; }

    /// <summary>Mesaj (zorunlu, en çok 1000 karakter).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Ek ayrıntılar (herhangi bir JSON değeri; isteğe bağlı).</summary>
    public JsonElement? Details { get; init; }

    /// <summary>İşlem süresi (milisaniye; isteğe bağlı).</summary>
    public long? DurationMs { get; init; }
}
