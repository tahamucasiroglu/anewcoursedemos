using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Attendance;
using OkulYonetim.Application.Features.Billing.Ledger;
using OkulYonetim.Application.Features.Billing.Reports;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Üç raporun sayfasız bütün satırları ve toplamları (spec 0017 gereksinim 7–9): ekran sorguları sayfalar, dışa aktarma bütününü yazar — ikisi
/// de aynı süzgeçle bu sınıftan geçer (dosya = ekrandaki satırlar). Her okuma önce <see cref="DashboardGuard"/>'dan (kurucu + şube) geçer, sonra
/// yalnızca diğer modüllerin sorgularını çağırır. Devamsızlık eşiği verilmezse 0 (yok ya da geç kaydı olan herkes).
/// </summary>
public sealed class DashboardReports
{
    public const int PageSize = 50;

    private readonly DashboardGuard guard;
    private readonly ISender sender;

    public DashboardReports(DashboardGuard guard, ISender sender)
    {
        this.guard = guard;
        this.sender = sender;
    }

    public static AttendanceReportTotals AttendanceTotals(IReadOnlyCollection<AttendanceReportRow> rows) =>
        new(rows.Sum(r => r.AbsentDays), rows.Sum(r => r.LateDays));

    /// <summary>Geçerli tahsilat toplamı ve yönteme göre toplamlar (üç yöntem her zaman, tanım sırasıyla); iptaller yalnızca sayılır.</summary>
    public static PaymentsReportTotals PaymentTotals(IReadOnlyCollection<PaymentReportRow> rows)
    {
        List<PaymentReportRow> valid = [.. rows.Where(r => !r.Cancelled)];
        return new PaymentsReportTotals(
            valid.Sum(r => r.Amount),
            valid.Count,
            rows.Count - valid.Count,
            [.. Enum.GetValues<PaymentMethod>().Select(m => m.ToString()).Select(m => new PaymentMethodTotal(
                m,
                valid.Where(r => r.Method == m).Sum(r => r.Amount),
                valid.Count(r => r.Method == m)))]);
    }

    public static AccountTotals BalanceTotals(IReadOnlyCollection<BalanceReportRow> rows) =>
        new(rows.Sum(r => r.Net), rows.Sum(r => r.Paid), rows.Sum(r => r.Remaining), rows.Sum(r => r.Overdue));

    public static IReadOnlyList<T> Page<T>(IReadOnlyList<T> rows, int page) => [.. rows.Skip((Math.Max(page, 1) - 1) * PageSize).Take(PageSize)];

    public async Task<(Guid TenantId, Guid UserId)> RequireAsync(IReportFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return await this.guard.RequireAsync(filter.BranchId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Devamsızlık raporu satırları; tarih aralığı doğrulayıcıdan geçmiştir.</summary>
    public async Task<IReadOnlyList<AttendanceReportRow>> AttendanceAsync(IReportFilter filter, CancellationToken cancellationToken)
    {
        await this.RequireAsync(filter, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<StudentAttendanceTotal> totals = await this.sender.Send(
            new ListAttendanceTotalsQuery(filter.StartDate!.Value, filter.EndDate!.Value, filter.BranchId, filter.ClassroomId, filter.MinAbsent ?? 0),
            cancellationToken).ConfigureAwait(false);
        return [.. totals.Select(t => new AttendanceReportRow(t.StudentId, t.FullName, t.SchoolNumber, t.BranchName, t.ClassroomName, t.AbsentDays, t.LateDays))];
    }

    public async Task<IReadOnlyList<PaymentReportRow>> PaymentsAsync(IReportFilter filter, CancellationToken cancellationToken)
    {
        await this.RequireAsync(filter, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<PaymentRangeRow> payments = await this.sender.Send(
            new ListPaymentsInRangeQuery(filter.StartDate!.Value, filter.EndDate!.Value, filter.BranchId), cancellationToken).ConfigureAwait(false);
        return [.. payments.Select(p => new PaymentReportRow(p.PaymentId, p.Date, p.ReceiptNo, p.StudentName, p.BranchName, p.Amount, p.Method, p.Cancelled, p.RecordedByName))];
    }

    public async Task<(DateOnly Today, IReadOnlyList<BalanceReportRow> Rows)> BalancesAsync(IReportFilter filter, CancellationToken cancellationToken)
    {
        await this.RequireAsync(filter, cancellationToken).ConfigureAwait(false);
        StudentBalances balances = await this.sender.Send(new ListStudentBalancesQuery(filter.BranchId, filter.OverdueOnly), cancellationToken).ConfigureAwait(false);
        return (balances.Today, [.. balances.Rows.Select(r => new BalanceReportRow(
            r.StudentId,
            r.FullName,
            r.BranchName,
            r.ClassroomName,
            string.Join(", ", r.PlanNames),
            r.Totals.Net,
            r.Totals.Paid,
            r.Totals.Remaining,
            r.Totals.Overdue,
            r.OldestOverdueDue))]);
    }
}
