namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Admin hata bildirimi detayı (spec 0025 Req 4–5): bildirimin kendisi, bildiren (ad/e-posta) ve admin notu. Metin alanları saklanırken
/// maskelenmiştir (0008, H-2); istemci düz metin olarak gösterir.
/// </summary>
/// <param name="Report">Bildirim.</param>
/// <param name="ReporterName">Bildiren kullanıcının adı; yoksa "—".</param>
/// <param name="ReporterEmail">Bildiren kullanıcının e-postası; yoksa "—".</param>
/// <param name="AdminNote">Admin notu.</param>
/// <param name="StatusChangedAt">Durumun son değiştiği an.</param>
public sealed record AdminErrorReportDetailDto(ErrorReportDto Report, string ReporterName, string ReporterEmail, string? AdminNote, DateTimeOffset? StatusChangedAt);
