namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Öğrencinin carisi (spec 0015 gereksinim 8): öğrenci etiketi, toplamlar, planlar (iptal dahil), tahsilatlar (iptal dahil) ve tarih sıralı hareket dökümü.</summary>
public sealed record StudentAccount(
    Guid StudentId,
    string FullName,
    string BranchName,
    string? ClassroomName,
    bool IsActive,
    AccountTotals Totals,
    IReadOnlyList<PlanView> Plans,
    IReadOnlyList<PaymentRow> Payments,
    IReadOnlyList<LedgerMovement> Movements);
