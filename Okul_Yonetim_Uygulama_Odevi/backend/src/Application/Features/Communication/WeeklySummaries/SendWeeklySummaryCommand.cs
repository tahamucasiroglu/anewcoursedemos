using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.WeeklySummaries;

/// <summary>
/// "Onayla ve gönder" (spec 0014 gereksinim 5, BR-5, BR-6): öğretmenin onayladığı (düzenlenmiş) özet, öğrencinin okulda etkin her
/// velisine "Haftalık gelişim özeti — &lt;hafta&gt;" okul bildirimi olarak etiket ve dayanak satırıyla gider; taslak "onaylandı" olur.
/// Hafta "current"/"previous" (boşsa bu hafta); metin boş olamaz, en fazla 1500 karakter. Özet metni taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record SendWeeklySummaryCommand(Guid StudentId, string? Week, string? Text) : IRequest<WeeklySummarySent>
{
    public const int TextMax = 1500;
}
