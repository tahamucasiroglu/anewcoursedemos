namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Kaydedilen hata bildiriminin kullanıcıya gösterilen numarası (spec 0022 Req 1: "Bildiriminiz alındı (No: …)").</summary>
/// <param name="Number">Bildirim numarası.</param>
public sealed record ErrorReportReceiptDto(long Number);
