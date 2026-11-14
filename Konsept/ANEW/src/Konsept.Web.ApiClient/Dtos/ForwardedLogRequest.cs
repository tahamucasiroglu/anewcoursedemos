namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Web'in API'ye ilettiği teknik log kaydı (spec 0008 Req 6). Mesaj Web'de maskelenmiş olarak gönderilir.</summary>
/// <param name="OccurredAt">Log zamanı.</param>
/// <param name="Level">Seviye: 3 uyarı, 4 hata, 5 kritik.</param>
/// <param name="Source">Kaynak (API her zaman Web olarak yazar).</param>
/// <param name="Message">Maskelenmiş mesaj.</param>
/// <param name="Exception">Maskelenmiş exception metni.</param>
/// <param name="CorrelationId">Web isteğinin korelasyon numarası.</param>
/// <param name="Category">Log kategorisi.</param>
public sealed record ForwardedLogRequest(
    DateTimeOffset OccurredAt,
    int Level,
    int Source,
    string Message,
    string? Exception,
    string? CorrelationId,
    string? Category);
