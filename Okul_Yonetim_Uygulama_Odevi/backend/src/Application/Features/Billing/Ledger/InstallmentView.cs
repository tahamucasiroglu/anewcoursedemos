namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Taksit satırı (spec 0015): sıra, vade, tutar, ödenen, kalan ve bugünkü durum (<c>Paid</c>/<c>PartiallyPaid</c>/<c>Pending</c>/<c>Overdue</c>).</summary>
public sealed record InstallmentView(int Sequence, DateOnly DueDate, decimal Amount, decimal Paid, decimal Remaining, string State);
