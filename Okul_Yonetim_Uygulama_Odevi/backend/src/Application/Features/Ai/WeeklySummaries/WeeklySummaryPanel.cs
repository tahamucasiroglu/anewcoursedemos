namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Öğrenci-hafta paneli (spec 0014 gereksinim 2, 3, 5): öğrenci, hafta, durum (<c>None</c>/<c>Draft</c>/<c>Sent</c>), dayanak (taslak
/// varsa taslakla saklanan — BR-6, yoksa şimdi toplanan; öğretmen notu dahil), kısa dayanak satırı, dayanak boş değil mi, bekleyen
/// taslağın kimliği ve metni, gönderilmişse son metin ve gönderim zamanı.
/// </summary>
public sealed record WeeklySummaryPanel(
    Guid StudentId,
    string FullName,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    string WeekLabel,
    string Status,
    WeeklySummarySource Basis,
    string BasisLine,
    bool HasData,
    Guid? DraftId,
    string? DraftText,
    string? FinalText,
    DateTimeOffset? SentAt);
