using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Plan formunun önizlemesi (spec 0015 UX: kaydetmeden önce taksit önizlemesi): tarife adı/tutarı, indirim adı/yüzdesi/tutarı, net ve taksitler (sıra, vade, tutar).</summary>
public sealed record PlanPreview(string FeeName, decimal FeeAmount, string? DiscountName, int? DiscountPercent, decimal DiscountAmount, decimal Net, IReadOnlyList<InstallmentLine> Installments);
