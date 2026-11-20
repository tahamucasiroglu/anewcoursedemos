using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.Calendar;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Takvim kaydı düzenleme ve silme (spec 0008): yalnızca seçili okulun kurucusu/yöneticisi (değilse 403). Başka okulun ya
/// da silinmiş kayıt 404; kayıt dönem dışına çıkarsa 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/calendar-entries")]
public sealed class AdminCalendarEntriesController : ControllerBase
{
    private readonly ISender sender;

    public AdminCalendarEntriesController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CalendarEntryView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarEntryView>> Update(Guid id, CalendarEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateCalendarEntryCommand(id, request.Title, request.Kind, request.StartDate, request.EndDate), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await this.sender.Send(new DeleteCalendarEntryCommand(id), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }
}
