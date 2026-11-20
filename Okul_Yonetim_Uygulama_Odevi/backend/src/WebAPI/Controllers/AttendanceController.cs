using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Attendance;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Yoklama uçları (spec 0010; okul seçilmiş olmalı): "Sınıflarım", bugünkü liste ve kaydetme. Öğretmen rolü olmayan 403;
/// atanmadığı, başka şubenin/okulun ya da pasif sınıf 404. Gün kuralı ve okul günü ihlali 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly ISender sender;

    public AttendanceController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("classrooms")]
    [ProducesResponseType<IReadOnlyList<MyClassroomRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyClassroomRow>>> Classrooms(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListMyClassroomsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("classrooms/{id:guid}/today")]
    [ProducesResponseType<AttendanceSheetView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceSheetView>> Today(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetTodayAttendanceQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPut("classrooms/{id:guid}/today")]
    [ProducesResponseType<AttendanceSaveResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceSaveResult>> Save(Guid id, SaveAttendanceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SaveTodayAttendanceCommand(id, request.Day, request.Entries), cancellationToken).ConfigureAwait(false));
    }
}
