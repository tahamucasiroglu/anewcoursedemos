namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>
/// Öğrenci carisindeki plan (spec 0015): açılıştaki tarife adı/tutarı, indirim adı/yüzdesi/tutarı, taksit sayısı, durum
/// (<c>Active</c>/<c>Cancelled</c>), açılış ve iptal anı, plan toplamları ve taksitler. İptal plan toplamları cariye girmez.
/// </summary>
public sealed record PlanView(
    Guid Id,
    string FeeName,
    decimal FeeAmount,
    string? DiscountName,
    int? DiscountPercent,
    decimal DiscountAmount,
    int InstallmentCount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    AccountTotals Totals,
    IReadOnlyList<InstallmentView> Installments);
