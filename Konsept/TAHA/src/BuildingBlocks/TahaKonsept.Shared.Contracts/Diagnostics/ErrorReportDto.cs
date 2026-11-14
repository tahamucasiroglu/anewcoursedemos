namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Yönetim panelinde listelenen hata bildirimi.
/// </summary>
public sealed record ErrorReportDto
{
    /// <summary>Kimlik.</summary>
    public Guid Id { get; init; }

    /// <summary>Bildiren kullanıcı (oturumsuz kurulum ekranında <see cref="Guid.Empty"/>).</summary>
    public Guid UserId { get; init; }

    /// <summary>Geldiği yer.</summary>
    public ErrorReportSource Source { get; init; }

    /// <summary>Kısa açıklama.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Ayrıntı / yığın izi / ek günlükler.</summary>
    public string? Details { get; init; }

    /// <summary>Bildirimin yapıldığı sayfa adresi (Web).</summary>
    public string? PageUrl { get; init; }

    /// <summary>Tarayıcı bilgisi (Web).</summary>
    public string? UserAgent { get; init; }

    /// <summary>Uygulama sürümü.</summary>
    public string? AppVersion { get; init; }

    /// <summary>Makine adı (ajan / kurulum).</summary>
    public string? Machine { get; init; }

    /// <summary>Durum.</summary>
    public ErrorReportStatus Status { get; init; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Son durum değişikliği (UTC) ya da <c>null</c>.</summary>
    public DateTimeOffset? StatusChangedAt { get; init; }
}
