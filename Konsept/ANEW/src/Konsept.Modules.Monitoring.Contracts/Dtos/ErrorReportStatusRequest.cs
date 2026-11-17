namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Bildirimin durumunu değiştirme isteği (spec 0025 Req 5).</summary>
/// <param name="Status">Yeni durum: <c>new</c> / <c>reviewing</c> / <c>resolved</c> / <c>ignored</c>.</param>
/// <param name="Note">İsteğe bağlı admin notu (en fazla 500 karakter; boşsa not silinir).</param>
public sealed record ErrorReportStatusRequest(string? Status, string? Note);
