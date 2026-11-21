namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Tahsilat raporu satırı (spec 0017 gereksinim 8): tarih, makbuz no, öğrenci, şube, tutar, yöntem, iptal mi ve kaydeden (not ve iptal gerekçesi yok).</summary>
public sealed record PaymentReportRow(Guid PaymentId, DateOnly Date, long ReceiptNo, string StudentName, string BranchName, decimal Amount, string Method, bool Cancelled, string RecordedByName);
