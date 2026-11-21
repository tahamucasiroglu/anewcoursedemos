namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Personelin gördüğü tahsilat satırı (spec 0015): plan, makbuz no, tarih, tutar, yöntem, not, kaydeden; iptal edildiyse anı ve gerekçesi.</summary>
public sealed record PaymentRow(
    Guid Id,
    Guid PlanId,
    string PlanName,
    long ReceiptNo,
    DateOnly Date,
    decimal Amount,
    string Method,
    string? Note,
    string RecordedByName,
    bool Cancelled,
    DateTimeOffset? CancelledAt,
    string? CancelReason);
