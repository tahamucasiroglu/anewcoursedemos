namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Web'den hata bildirimi (spec 0022 Req 1–2): açıklama, bulunulan sayfa, varsa son hatanın korelasyon numarası.</summary>
/// <param name="Description">Açıklama (10–4000 karakter; Api doğrular).</param>
/// <param name="Page">Bulunulan sayfa (yol).</param>
/// <param name="CorrelationId">Son hatanın korelasyon numarası.</param>
public sealed record ErrorReportApiRequest(string? Description, string? Page, string? CorrelationId);
