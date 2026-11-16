namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Saklanan bir hata bildirimi (spec 0022 Req 11–12): yalnızca <c>admin.panel</c> yetkisiyle okunur (listeleme 0025).
/// </summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Number">Bildirim numarası.</param>
/// <param name="Source">Kaynak: <c>web</c> / <c>agent</c> / <c>api</c>.</param>
/// <param name="Status">Durum: <c>new</c>.</param>
/// <param name="CreatedAt">Zaman (UTC).</param>
/// <param name="UserId">Gönderen kullanıcı.</param>
/// <param name="ProductVersion">Ürün versiyonu.</param>
/// <param name="Description">Açıklama ya da hata özeti (maskeli).</param>
/// <param name="Page">Sayfa (web).</param>
/// <param name="CorrelationId">Korelasyon numarası.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="OperatingSystem">İşletim sistemi.</param>
/// <param name="Component">Bileşen.</param>
/// <param name="Step">Adım.</param>
/// <param name="Attachments">Ekli satırlar (Günlük ya da ajan logu; maskeli).</param>
public sealed record ErrorReportDto(
    Guid Id,
    long Number,
    string Source,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? UserId,
    string ProductVersion,
    string? Description,
    string? Page,
    string? CorrelationId,
    string? AgentVersion,
    string? OperatingSystem,
    string? Component,
    string? Step,
    string? Attachments);
