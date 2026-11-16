namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Kaydedilen hata bildiriminin numarası (spec 0022 Req 1).</summary>
/// <param name="Number">Bildirim numarası.</param>
public sealed record ErrorReportReceiptResponse(long Number);
