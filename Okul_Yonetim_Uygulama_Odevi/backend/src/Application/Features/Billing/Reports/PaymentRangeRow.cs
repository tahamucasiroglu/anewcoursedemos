namespace OkulYonetim.Application.Features.Billing.Reports;

/// <summary>Aralıktaki tahsilat (spec 0017 tahsilat raporu): tarih, makbuz no, öğrenci, şube, tutar, yöntem, iptal mi ve kaydeden (not ve gerekçe yok).</summary>
public sealed record PaymentRangeRow(Guid PaymentId, DateOnly Date, long ReceiptNo, Guid StudentId, string StudentName, string BranchName, decimal Amount, string Method, bool Cancelled, string RecordedByName);
