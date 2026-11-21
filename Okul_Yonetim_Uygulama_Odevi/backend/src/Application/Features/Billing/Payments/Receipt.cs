namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>
/// Makbuz (spec 0015 gereksinim 7): okul ve şube adı, makbuz no, tarih, öğrenci ad-soyad, plan adı (tarife adı), tutar, yöntem,
/// dağıldığı taksitler, kaydeden kişinin adı ve kayıt anı; iptal edildiyse iptal anı ve gerekçesi. TC kimlik no, IBAN ve sağlık
/// bilgisi taşımaz. "Bu belge fatura yerine geçmez." metni istemcide (yerelleştirme dosyası).
/// </summary>
public sealed record Receipt(
    Guid PaymentId,
    long ReceiptNo,
    string SchoolName,
    string BranchName,
    DateOnly Date,
    string StudentName,
    string PlanName,
    decimal Amount,
    string Method,
    IReadOnlyList<ReceiptAllocation> Allocations,
    string RecordedByName,
    DateTimeOffset RecordedAt,
    bool Cancelled,
    DateTimeOffset? CancelledAt,
    string? CancelReason);
