namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Hata bildirimi gönderme isteği: <c>POST api/diagnostics/error-reports</c> (Web "Hata bildir" penceresi, ajan, kurulum ekranı).
/// <see cref="AppVersion"/> boşsa sunucu kendi sürümünü yazar.
/// </summary>
public sealed record ErrorReportCreateRequest
{
    /// <summary>Geldiği yer.</summary>
    public ErrorReportSource Source { get; init; } = ErrorReportSource.Web;

    /// <summary>Ne oldu (zorunlu, en çok 2000 karakter).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Ayrıntı / yığın izi (isteğe bağlı, en çok 32000 karakter).</summary>
    public string? Details { get; init; }

    /// <summary>Sayfa adresi (Web).</summary>
    public string? PageUrl { get; init; }

    /// <summary>Tarayıcı bilgisi (Web).</summary>
    public string? UserAgent { get; init; }

    /// <summary>Uygulama sürümü.</summary>
    public string? AppVersion { get; init; }

    /// <summary>Makine adı (ajan / kurulum).</summary>
    public string? Machine { get; init; }
}
