using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.Calendar;
using OkulYonetim.Application.Features.Administration.Terms;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Dönem ve dönem takvimi yönetimi (spec 0008): yalnızca seçili okulun kurucusu/yöneticisi (değilse 403). Başka okulun
/// dönemi 404; doğrulama hatası ve iş kuralı (aynı ad, çakışma, dışarıda kalan takvim kaydı) 400 ve Türkçe mesaj.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/terms")]
public sealed class AdminTermsController : ControllerBase
{
    private readonly ISender sender;

    public AdminTermsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TermAdminView>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TermAdminView>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListAdminTermsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<TermAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TermAdminView>> Create(TermRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateTermCommand(request.Name, request.StartDate, request.EndDate), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TermAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TermAdminView>> Update(Guid id, TermRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateTermCommand(id, request.Name, request.StartDate, request.EndDate), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{id:guid}/calendar-entries")]
    [ProducesResponseType<IReadOnlyList<CalendarEntryView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CalendarEntryView>>> ListCalendar(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListTermCalendarQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/calendar-entries")]
    [ProducesResponseType<CalendarEntryView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarEntryView>> CreateCalendarEntry(Guid id, CalendarEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateCalendarEntryCommand(id, request.Title, request.Kind, request.StartDate, request.EndDate), cancellationToken).ConfigureAwait(false));
    }
}
