namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Personelin cari listesindeki satır (spec 0015 gereksinim 8): öğrenci, şube, sınıf, etkin mi ve toplamlar.</summary>
public sealed record StudentAccountRow(Guid StudentId, string FullName, string BranchName, string? ClassroomName, bool IsActive, AccountTotals Totals);
