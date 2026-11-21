using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Appointments.Availability;
using OkulYonetim.Application.Features.Appointments.Requests;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Randevu uçları (spec 0016; okul seçilmiş olmalı). Öğretmen: "Takvimim", müsaitlik açma, boş dilimi kaldırma, talebi yanıtlama.
/// Veli: çocuğun öğretmenleri, öğretmenin boş saatleri, talep ve "Randevularım". İkisi: iptal. Rol yoksa 403; başka okulun, ilişkisi
/// olmayan öğretmenin/çocuğun ya da başkasının kaydı 404; çocuğun öteki velisinin talebini iptal 403; alan hataları ve iş kuralları 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly ISender sender;

    public AppointmentsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("schedule")]
    [ProducesResponseType<TeacherSchedule>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TeacherSchedule>> Schedule(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyScheduleQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost("availability")]
    [ProducesResponseType<AvailabilityOpened>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AvailabilityOpened>> OpenAvailability(OpenAvailabilityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new OpenAvailabilityCommand(request.Day, request.Start, request.End, request.DurationMinutes), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("slots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSlot(Guid id, CancellationToken cancellationToken)
    {
        await this.sender.Send(new RemoveSlotCommand(id), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }

    [HttpPost("{id:guid}/respond")]
    [ProducesResponseType<AppointmentChanged>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentChanged>> Respond(Guid id, RespondAppointmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new RespondAppointmentCommand(id, request.Decision, request.Reason), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("teachers")]
    [ProducesResponseType<IReadOnlyList<BookableTeacher>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookableTeacher>>> Teachers([FromQuery] Guid studentId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListBookableTeachersQuery(studentId), cancellationToken).ConfigureAwait(false));

    [HttpGet("teachers/{teacherId:guid}/slots")]
    [ProducesResponseType<IReadOnlyList<OpenSlot>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OpenSlot>>> Slots(Guid teacherId, [FromQuery] Guid studentId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListOpenSlotsQuery(teacherId, studentId), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<AppointmentChanged>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentChanged>> Create(AppointmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new RequestAppointmentCommand(request.SlotId, request.StudentId, request.Topic), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("mine")]
    [ProducesResponseType<MyAppointments>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MyAppointments>> Mine(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListMyAppointmentsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<AppointmentChanged>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentChanged>> Cancel(Guid id, CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CancelAppointmentCommand(id, request.Reason), cancellationToken).ConfigureAwait(false));
    }
}
