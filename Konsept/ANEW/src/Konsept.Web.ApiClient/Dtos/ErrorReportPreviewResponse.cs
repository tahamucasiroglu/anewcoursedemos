namespace Konsept.Web.ApiClient.Dtos;

/// <summary>"Hata bildir" modalının gösterdiği otomatik bilgiler: versiyon, zaman ve son 20 Günlük kaydı (spec 0022 Req 1).</summary>
/// <param name="Version">Ürün versiyonu.</param>
/// <param name="Now">Sunucu zamanı (UTC).</param>
/// <param name="Journal">Son Günlük kayıtları.</param>
public sealed record ErrorReportPreviewResponse(string Version, DateTimeOffset Now, IReadOnlyList<SystemEventResponse> Journal);
