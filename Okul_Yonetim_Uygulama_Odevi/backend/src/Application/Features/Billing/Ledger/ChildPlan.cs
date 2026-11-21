namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Velinin gördüğü plan (spec 0015): plan adı, toplamlar, taksitler (vade, tutar, durum) ve tahsilatlar.</summary>
public sealed record ChildPlan(Guid Id, string FeeName, AccountTotals Totals, IReadOnlyList<InstallmentView> Installments, IReadOnlyList<ChildPayment> Payments);
