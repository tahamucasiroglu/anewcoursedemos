using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetSchoolCalendar;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>Okul takvimi (spec 0008, salt okunur): okul seçilmiş oturumda her rol; yalnızca seçili okulun takvimi.</summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/calendar")]
public sealed class SchoolCalendarController : ControllerBase
{
    private readonly ISender sender;

    public SchoolCalendarController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<SchoolCalendarView>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SchoolCalendarView>> Get(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetSchoolCalendarQuery(), cancellationToken).ConfigureAwait(false));
}
