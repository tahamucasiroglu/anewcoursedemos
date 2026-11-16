namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Web'den hata bildirimi (spec 0022 Req 1–2): kullanıcının açıklaması, bulunulan sayfa ve varsa son hatanın korelasyon numarası.
/// Gönderen kullanıcı, zaman, versiyon ve son 20 Günlük kaydı sunucuda eklenir.
/// </summary>
/// <param name="Description">Açıklama (10–4000 karakter).</param>
/// <param name="Page">Bulunulan sayfa (yol).</param>
/// <param name="CorrelationId">Son hatanın korelasyon numarası.</param>
public sealed record WebErrorReportRequest(string? Description, string? Page, string? CorrelationId);
