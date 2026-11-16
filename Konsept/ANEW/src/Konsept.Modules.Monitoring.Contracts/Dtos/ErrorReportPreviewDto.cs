namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// "Hata bildir" modalının gönderimden önce gösterdiği otomatik bilgiler (spec 0022 Req 1): ürün versiyonu, zaman ve kullanıcının son
/// 20 Günlük kaydı (sayfa ve korelasyon tarayıcıdadır).
/// </summary>
/// <param name="Version">Ürün versiyonu.</param>
/// <param name="Now">Sunucu zamanı (UTC).</param>
/// <param name="Journal">Son Günlük kayıtları (en yenisi üstte).</param>
public sealed record ErrorReportPreviewDto(string Version, DateTimeOffset Now, IReadOnlyList<SystemEventDto> Journal);
