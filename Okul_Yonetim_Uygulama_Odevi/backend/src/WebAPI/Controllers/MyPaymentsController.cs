using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Billing.Ledger;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Velinin "Ödemeler" ucu (spec 0015 gereksinim 10; okul seçilmiş olmalı): seçili okuldaki bağlı çocuklarının geçerli planları, taksitleri,
/// tahsilatları ve toplam kalan. Salt okunur; veli rolü olmayan 403.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/my/payments")]
public sealed class MyPaymentsController : ControllerBase
{
    private readonly ISender sender;

    public MyPaymentsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<MyPayments>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MyPayments>> Get(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListMyChildrenAccountsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("{studentId:guid}")]
    [ProducesResponseType<ChildAccount>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildAccount>> Child(Guid studentId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyChildAccountQuery(studentId), cancellationToken).ConfigureAwait(false));
}
