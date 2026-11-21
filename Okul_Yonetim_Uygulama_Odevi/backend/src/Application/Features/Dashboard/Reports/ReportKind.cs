namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Temel raporlar (spec 0017 C-4): devamsızlık, tahsilat ve borç/gecikme.</summary>
public enum ReportKind
{
    Attendance = 1,
    Payments = 2,
    Balances = 3,
}
