namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Cari hareketi (spec 0015 gereksinim 8, tarih sıralı döküm): tür (<c>Plan</c> borç / <c>Payment</c> geçerli tahsilat), plan adı, makbuz no, borç, alacak ve yürüyen bakiye.</summary>
public sealed record LedgerMovement(DateOnly Date, string Kind, string PlanName, long? ReceiptNo, decimal Debit, decimal Credit, decimal Balance);
