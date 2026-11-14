namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Kullanıcıya görünen işlem günlüğü kaydı (alt Günlük şeridi, yönetim paneli).
/// JSON: <c>{ id, level, source, message, details, durationMs, userId, createdAt }</c>.
/// </summary>
public sealed record ActivityLogDto
{
    /// <summary>Kayıt kimliği (v7 Guid; zamana göre artar).</summary>
    public Guid Id { get; init; }

    /// <summary>Seviye (JSON'da metin: <c>Information</c>, <c>Warning</c>, <c>Error</c>, <c>Critical</c>).</summary>
    public LogLevelKind Level { get; init; } = LogLevelKind.Information;

    /// <summary>Kaynak (ör. <c>Production</c>, <c>Web</c>, <c>Agent</c>).</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Türkçe, kullanıcıya gösterilen mesaj.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Ek ayrıntılar (JSON metni) ya da <c>null</c>.</summary>
    public string? Details { get; init; }

    /// <summary>İşlem süresi (milisaniye) ya da <c>null</c>.</summary>
    public long? DurationMs { get; init; }

    /// <summary>Kaydın sahibi (sistem kayıtlarında <see cref="Guid.Empty"/>).</summary>
    public Guid UserId { get; init; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
