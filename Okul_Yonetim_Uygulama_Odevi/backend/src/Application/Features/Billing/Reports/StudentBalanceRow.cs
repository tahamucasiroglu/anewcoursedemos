using OkulYonetim.Application.Features.Billing.Ledger;

namespace OkulYonetim.Application.Features.Billing.Reports;

/// <summary>
/// Geçerli planı olan öğrencinin bugünkü carisi (spec 0017 borç raporu ve geciken ödemeler kartı): etiket, geçerli planların adları,
/// cari toplamları (0015 <see cref="BillingLedger"/> hesabı) ve en eski geciken vade (gecikme yoksa boş).
/// </summary>
public sealed record StudentBalanceRow(
    Guid StudentId,
    string FullName,
    Guid BranchId,
    string BranchName,
    string? ClassroomName,
    bool IsActive,
    IReadOnlyList<string> PlanNames,
    AccountTotals Totals,
    DateOnly? OldestOverdueDue);
