using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.AiGateway.Presentation.Controllers;

/// <summary>Aylık harcama (spec 0013 Req 15): isteği yapan kullanıcının bu ayki yuvarlanmamış toplamı. Giriş ister.</summary>
[ApiController]
[Route("api/ai/spend")]
public sealed class SpendController : ControllerBase
{
    private readonly ISpendService _spend;

    /// <summary>Initializes a new instance of the <see cref="SpendController"/> class.</summary>
    /// <param name="spend">Harcama servisi.</param>
    public SpendController(ISpendService spend)
    {
        _spend = spend;
    }

    /// <summary>Bu ayın toplamı.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Toplam.</returns>
    [HttpGet("month")]
    public Task<IActionResult> Month(CancellationToken cancellationToken) =>
        _spend.GetMonthlyAsync(cancellationToken).ToActionResult();
}
