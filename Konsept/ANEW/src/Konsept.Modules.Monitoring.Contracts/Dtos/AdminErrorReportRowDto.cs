namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Admin hata bildirimi listesinin bir satırı (spec 0025 Req 1): içerik yalnızca bildirimin kendi kısa mesajıdır (BR-1).</summary>
/// <param name="Number">Bildirim numarası.</param>
/// <param name="CreatedAt">Zaman (UTC).</param>
/// <param name="Source">Kaynak: <c>web</c> / <c>agent</c> / <c>api</c>.</param>
/// <param name="ReporterName">Bildiren kullanıcının adı; yoksa "—".</param>
/// <param name="ReporterEmail">Bildiren kullanıcının e-postası; yoksa "—".</param>
/// <param name="Summary">Kısa mesaj (açıklamanın başı, maskeli).</param>
/// <param name="Status">Durum: <c>new</c> / <c>reviewing</c> / <c>resolved</c> / <c>ignored</c>.</param>
public sealed record AdminErrorReportRowDto(long Number, DateTimeOffset CreatedAt, string Source, string ReporterName, string ReporterEmail, string Summary, string Status);
