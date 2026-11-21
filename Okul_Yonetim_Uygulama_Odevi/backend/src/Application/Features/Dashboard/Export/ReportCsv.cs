using OkulYonetim.Application.Features.Billing.Ledger;
using OkulYonetim.Application.Features.Dashboard.Reports;

namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>
/// Raporların CSV sütunları ve hücreleri (spec 0017 gereksinim 7–10): Türkçe başlıklar ve durum/yöntem adları YAML.dan (<c>text</c>
/// anahtar → metin); ad, şube, sınıf, plan ve kaydeden metin hücresidir (formül kaçışından geçer), tutar/sayı/gün biçimlenir. Son satır(lar)
/// toplamlardır. Kişisel veri olarak yalnızca ad-soyad ve okul no (TC, telefon, e-posta, sağlık yok — C-6).
/// </summary>
public static class ReportCsv
{
    public static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CsvCell>> Rows) Attendance(IReadOnlyList<AttendanceReportRow> rows, Func<string, string> text)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(text);
        AttendanceReportTotals totals = DashboardReports.AttendanceTotals(rows);
        List<IReadOnlyList<CsvCell>> cells = [.. rows.Select(r => (IReadOnlyList<CsvCell>)
            [CsvCell.Of(r.FullName), CsvCell.Of(r.SchoolNumber), CsvCell.Of(r.BranchName), CsvCell.Of(r.ClassroomName), CsvCell.Number(r.AbsentDays), CsvCell.Number(r.LateDays)])];
        cells.Add([CsvCell.Of(text(DashboardMessages.TotalRow)), CsvCell.Empty, CsvCell.Empty, CsvCell.Empty, CsvCell.Number(totals.AbsentDays), CsvCell.Number(totals.LateDays)]);
        return (
            [text(DashboardMessages.ColStudent), text(DashboardMessages.ColSchoolNumber), text(DashboardMessages.ColBranch), text(DashboardMessages.ColClassroom), text(DashboardMessages.ColAbsent), text(DashboardMessages.ColLate)],
            cells);
    }

    public static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CsvCell>> Rows) Payments(IReadOnlyList<PaymentReportRow> rows, Func<string, string> text)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(text);
        PaymentsReportTotals totals = DashboardReports.PaymentTotals(rows);
        List<IReadOnlyList<CsvCell>> cells = [.. rows.Select(r => (IReadOnlyList<CsvCell>)
        [
            CsvCell.Date(r.Date),
            CsvCell.Number(r.ReceiptNo),
            CsvCell.Of(r.StudentName),
            CsvCell.Of(r.BranchName),
            CsvCell.Money(r.Amount),
            CsvCell.Of(MethodText(r.Method, text)),
            CsvCell.Of(text(r.Cancelled ? DashboardMessages.StatusCancelled : DashboardMessages.StatusValid)),
            CsvCell.Of(r.RecordedByName),
        ])];
        cells.Add(TotalRow(text(DashboardMessages.TotalValidRow), totals.ValidAmount));
        foreach (PaymentMethodTotal method in totals.ByMethod)
        {
            cells.Add(TotalRow(text(DashboardMessages.TotalMethodRow).Replace("{method}", MethodText(method.Method, text), StringComparison.Ordinal), method.Amount));
        }

        return (
            [
                text(DashboardMessages.ColDate), text(DashboardMessages.ColReceiptNo), text(DashboardMessages.ColStudent), text(DashboardMessages.ColBranch),
                text(DashboardMessages.ColAmount), text(DashboardMessages.ColMethod), text(DashboardMessages.ColStatus), text(DashboardMessages.ColRecordedBy),
            ],
            cells);
    }

    public static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CsvCell>> Rows) Balances(IReadOnlyList<BalanceReportRow> rows, Func<string, string> text)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(text);
        AccountTotals totals = DashboardReports.BalanceTotals(rows);
        List<IReadOnlyList<CsvCell>> cells = [.. rows.Select(r => (IReadOnlyList<CsvCell>)
        [
            CsvCell.Of(r.FullName),
            CsvCell.Of(r.BranchName),
            CsvCell.Of(r.ClassroomName),
            CsvCell.Of(r.PlanNames),
            CsvCell.Money(r.Net),
            CsvCell.Money(r.Paid),
            CsvCell.Money(r.Remaining),
            CsvCell.Money(r.Overdue),
            r.OldestOverdueDue is { } due ? CsvCell.Date(due) : CsvCell.Empty,
        ])];
        cells.Add(
        [
            CsvCell.Of(text(DashboardMessages.TotalRow)), CsvCell.Empty, CsvCell.Empty, CsvCell.Empty,
            CsvCell.Money(totals.Net), CsvCell.Money(totals.Paid), CsvCell.Money(totals.Remaining), CsvCell.Money(totals.Overdue), CsvCell.Empty,
        ]);
        return (
            [
                text(DashboardMessages.ColStudent), text(DashboardMessages.ColBranch), text(DashboardMessages.ColClassroom), text(DashboardMessages.ColPlan),
                text(DashboardMessages.ColNet), text(DashboardMessages.ColPaid), text(DashboardMessages.ColRemaining), text(DashboardMessages.ColOverdue),
                text(DashboardMessages.ColOldestDue),
            ],
            cells);
    }

    private static string MethodText(string method, Func<string, string> text) => method switch
    {
        "Cash" => text(DashboardMessages.MethodCash),
        "BankTransfer" => text(DashboardMessages.MethodBankTransfer),
        "CardPos" => text(DashboardMessages.MethodCardPos),
        _ => method,
    };

    private static IReadOnlyList<CsvCell> TotalRow(string label, decimal amount) =>
        [CsvCell.Of(label), CsvCell.Empty, CsvCell.Empty, CsvCell.Empty, CsvCell.Money(amount), CsvCell.Empty, CsvCell.Empty, CsvCell.Empty];
}
