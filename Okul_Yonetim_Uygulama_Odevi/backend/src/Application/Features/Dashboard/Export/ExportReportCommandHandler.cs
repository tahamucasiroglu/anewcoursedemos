using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Dashboard.Reports;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>
/// Raporu CSV olarak verir (spec 0017 gereksinim 10–11): <see cref="DashboardReports"/>'tan ekrandakiyle aynı süzgeçle bütün satırlar; veri
/// satırı <see cref="MaxRows"/>'u aşarsa 400 "Sonuç çok büyük…" ve hiçbir şey yazılmaz (audit yok). Başarıda dosya <see cref="CsvWriter"/> ile
/// bellekte kurulur (sunucuda saklanmaz), audit'e <see cref="AuditAction.ReportExported"/> yazılır (kim, ne zaman, IP; ayrıntıda rapor türü,
/// süzgeçler ve satır sayısı — ad yok) ve kimliklerle bir Information log satırı düşer (dosya içeriği ve adlar loglanmaz). Dosya adı
/// <c>&lt;rapor&gt;-GG.AA.YYYY.csv</c>, gün Türkiye günü.
/// </summary>
public sealed class ExportReportCommandHandler : IRequestHandler<ExportReportCommand, ReportFile>
{
    public const int MaxRows = 10_000;

    private readonly DashboardReports reports;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClientInfo client;
    private readonly TimeProvider timeProvider;
    private readonly SchoolCalendar calendar;
    private readonly ILocalizationService localization;
    private readonly ILogger<ExportReportCommandHandler> logger;

    public ExportReportCommandHandler(
        DashboardReports reports,
        IAuditLog audit,
        IUnitOfWork unitOfWork,
        IClientInfo client,
        TimeProvider timeProvider,
        SchoolCalendar calendar,
        ILocalizationService localization,
        ILogger<ExportReportCommandHandler> logger)
    {
        this.reports = reports;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.client = client;
        this.timeProvider = timeProvider;
        this.calendar = calendar;
        this.localization = localization;
        this.logger = logger;
    }

    public static string FileName(ReportKind kind, DateOnly today) =>
        (kind switch
        {
            ReportKind.Attendance => "devamsizlik-raporu",
            ReportKind.Payments => "tahsilat-raporu",
            _ => "borc-gecikme-raporu",
        }) + "-" + today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ".csv";

    public async Task<ReportFile> Handle(ExportReportCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid userId) = await this.reports.RequireAsync(request, cancellationToken).ConfigureAwait(false);
        Func<string, string> text = this.localization.DashboardText;
        (IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<CsvCell>> rows, int count) = request.Kind switch
        {
            ReportKind.Attendance => Shape(ReportCsv.Attendance, await this.reports.AttendanceAsync(request, cancellationToken).ConfigureAwait(false), text),
            ReportKind.Payments => Shape(ReportCsv.Payments, await this.reports.PaymentsAsync(request, cancellationToken).ConfigureAwait(false), text),
            _ => Shape(ReportCsv.Balances, (await this.reports.BalancesAsync(request, cancellationToken).ConfigureAwait(false)).Rows, text),
        };
        if (count > MaxRows)
        {
            throw new BusinessException(await this.localization.DashboardTextAsync(DashboardMessages.TooManyRows).ConfigureAwait(false));
        }

        byte[] content = CsvWriter.Write(headers, rows);
        this.audit.Add(AuditEntry.Record(
            AuditAction.ReportExported,
            this.timeProvider.GetUtcNow(),
            tenantId,
            userId,
            targetType: "Report",
            ipAddress: this.client.IpAddress,
            detail: Detail(request, count)));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation("Rapor dışa aktarıldı: {Kind}, {RowCount} satır, kişi {UserId}, okul {TenantId}", request.Kind, count, userId, tenantId);
        return new ReportFile(FileName(request.Kind, this.calendar.Today()), content, count);
    }

    /// <summary>Audit ayrıntısı: rapor türü, verilen süzgeçler (kimlik ve sayı) ve satır sayısı — 256 karakterin altında.</summary>
    public static string Detail(ExportReportCommand request, int rowCount)
    {
        ArgumentNullException.ThrowIfNull(request);
        StringBuilder detail = new("report=" + request.Kind);
        if (request.StartDate is { } start && request.EndDate is { } end && request.Kind != ReportKind.Balances)
        {
            detail.Append(";range=").Append(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("..").Append(end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (request.BranchId is { } branch)
        {
            detail.Append(";branch=").Append(branch.ToString("D"));
        }

        if (request.ClassroomId is { } classroom && request.Kind == ReportKind.Attendance)
        {
            detail.Append(";classroom=").Append(classroom.ToString("D"));
        }

        if (request.MinAbsent is { } min && request.Kind == ReportKind.Attendance)
        {
            detail.Append(";minAbsent=").Append(min.ToString(CultureInfo.InvariantCulture));
        }

        if (request.Kind == ReportKind.Balances)
        {
            detail.Append(";overdueOnly=").Append(request.OverdueOnly ? "true" : "false");
        }

        return detail.Append(";rows=").Append(rowCount.ToString(CultureInfo.InvariantCulture)).ToString();
    }

    private static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CsvCell>> Rows, int Count) Shape<T>(
        Func<IReadOnlyList<T>, Func<string, string>, (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CsvCell>> Rows)> shape,
        IReadOnlyList<T> rows,
        Func<string, string> text)
    {
        if (rows.Count > MaxRows)
        {
            return ([], [], rows.Count);
        }

        (IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<CsvCell>> cells) = shape(rows, text);
        return (headers, cells, rows.Count);
    }
}
