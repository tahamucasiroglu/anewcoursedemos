namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Borç/gecikme raporu satırı (spec 0017 gereksinim 9): öğrenci, şube, sınıf, geçerli plan adları, net, ödenen, kalan, geciken ve en eski geciken vade (0015 carisiyle aynı hesap).</summary>
public sealed record BalanceReportRow(Guid StudentId, string FullName, string BranchName, string? ClassroomName, string PlanNames, decimal Net, decimal Paid, decimal Remaining, decimal Overdue, DateOnly? OldestOverdueDue);
