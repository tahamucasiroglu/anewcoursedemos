using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Rapor ekranının sayfaları (spec 0017 gereksinim 6–9): <see cref="DashboardReports"/>'tan bütün satırlar, 50'lik sayfa; toplamlar bütün
/// sonuç üzerinden. Süzgeç doğrulaması istek doğrulayıcısında (alan hatası 400).
/// </summary>
public sealed class ReportHandlers :
    IRequestHandler<GetAttendanceReportQuery, AttendanceReportPage>,
    IRequestHandler<GetPaymentsReportQuery, PaymentsReportPage>,
    IRequestHandler<GetBalancesReportQuery, BalancesReportPage>
{
    private readonly DashboardReports reports;

    public ReportHandlers(DashboardReports reports)
    {
        this.reports = reports;
    }

    public async Task<AttendanceReportPage> Handle(GetAttendanceReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<AttendanceReportRow> rows = await this.reports.AttendanceAsync(request, cancellationToken).ConfigureAwait(false);
        return new AttendanceReportPage(DashboardReports.Page(rows, request.Page), request.Page, DashboardReports.PageSize, rows.Count, DashboardReports.AttendanceTotals(rows));
    }

    public async Task<PaymentsReportPage> Handle(GetPaymentsReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<PaymentReportRow> rows = await this.reports.PaymentsAsync(request, cancellationToken).ConfigureAwait(false);
        return new PaymentsReportPage(DashboardReports.Page(rows, request.Page), request.Page, DashboardReports.PageSize, rows.Count, DashboardReports.PaymentTotals(rows));
    }

    public async Task<BalancesReportPage> Handle(GetBalancesReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (DateOnly today, IReadOnlyList<BalanceReportRow> rows) = await this.reports.BalancesAsync(request, cancellationToken).ConfigureAwait(false);
        return new BalancesReportPage(DashboardReports.Page(rows, request.Page), request.Page, DashboardReports.PageSize, rows.Count, DashboardReports.BalanceTotals(rows), today);
    }
}
