using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Dashboard.Export;
using OkulYonetim.Application.Features.Dashboard.Reports;
using OkulYonetim.Application.Features.Dashboard.Summary;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Yönetici paneli uçları (spec 0017; yalnızca seçili okulun kurucusu — <c>TenantFounder</c> politikası + <c>DashboardGuard</c>, değilse 403,
/// girişsiz 401): günlük özetin üç kartı (`summary/attendance|overdue|announcements`, şube süzgeciyle), üç rapor (`reports/attendance|payments|balances`,
/// 50'lik sayfa) ve aynı süzgeçle CSV dışa aktarma (`.../export` → <c>text/csv; charset=utf-8</c>, ek olarak iner). Başka okulun şubesi 404;
/// süzgeç alan hataları ve "Sonuç çok büyük" 400 (Türkçe). Panel yalnızca okur; dışa aktarma audit'e yazılır.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private const string CsvContentType = "text/csv; charset=utf-8";
    private readonly ISender sender;

    public DashboardController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("summary/attendance")]
    [ProducesResponseType<AttendanceCard>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceCard>> AttendanceCard([FromQuery] Guid? branchId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetAttendanceCardQuery(branchId), cancellationToken).ConfigureAwait(false));

    [HttpGet("summary/overdue")]
    [ProducesResponseType<OverdueCard>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OverdueCard>> OverdueCard([FromQuery] Guid? branchId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetOverdueCardQuery(branchId), cancellationToken).ConfigureAwait(false));

    [HttpGet("summary/announcements")]
    [ProducesResponseType<UnreadAnnouncementsCard>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnreadAnnouncementsCard>> AnnouncementsCard([FromQuery] Guid? branchId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetUnreadAnnouncementsCardQuery(branchId), cancellationToken).ConfigureAwait(false));

    [HttpGet("reports/attendance")]
    [ProducesResponseType<AttendanceReportPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceReportPage>> AttendanceReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? classroomId,
        [FromQuery] int? minAbsent,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1) =>
        this.Ok(await this.sender.Send(new GetAttendanceReportQuery(startDate, endDate, branchId, classroomId, minAbsent, page), cancellationToken).ConfigureAwait(false));

    [HttpGet("reports/payments")]
    [ProducesResponseType<PaymentsReportPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentsReportPage>> PaymentsReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1) =>
        this.Ok(await this.sender.Send(new GetPaymentsReportQuery(startDate, endDate, branchId, page), cancellationToken).ConfigureAwait(false));

    [HttpGet("reports/balances")]
    [ProducesResponseType<BalancesReportPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalancesReportPage>> BalancesReport(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] int page = 1) =>
        this.Ok(await this.sender.Send(new GetBalancesReportQuery(branchId, overdueOnly, page), cancellationToken).ConfigureAwait(false));

    [HttpGet("reports/attendance/export")]
    [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK, CsvContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<FileContentResult> ExportAttendance(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? classroomId,
        [FromQuery] int? minAbsent,
        CancellationToken cancellationToken) =>
        this.ExportAsync(new ExportReportCommand(ReportKind.Attendance, startDate, endDate, branchId, classroomId, minAbsent, false), cancellationToken);

    [HttpGet("reports/payments/export")]
    [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK, CsvContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<FileContentResult> ExportPayments(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken) =>
        this.ExportAsync(new ExportReportCommand(ReportKind.Payments, startDate, endDate, branchId, null, null, false), cancellationToken);

    [HttpGet("reports/balances/export")]
    [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK, CsvContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<FileContentResult> ExportBalances(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken,
        [FromQuery] bool overdueOnly = false) =>
        this.ExportAsync(new ExportReportCommand(ReportKind.Balances, null, null, branchId, null, null, overdueOnly), cancellationToken);

    private async Task<FileContentResult> ExportAsync(ExportReportCommand command, CancellationToken cancellationToken)
    {
        ReportFile file = await this.sender.Send(command, cancellationToken).ConfigureAwait(false);
        return this.File(file.Content, CsvContentType, file.FileName);
    }
}
