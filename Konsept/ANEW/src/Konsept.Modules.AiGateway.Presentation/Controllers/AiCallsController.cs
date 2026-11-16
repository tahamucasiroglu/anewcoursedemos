using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.AiGateway.Presentation.Controllers;

/// <summary>
/// Deneme çağrısı (plan 0013 Risk 3): isteği yapan kullanıcının yetkisi ve kendi key'iyle, kapının tüm kurallarıyla tek kısa çağrı.
/// Giriş ister (default-deny); yetki ve key kararı sunucudadır.
/// </summary>
[ApiController]
[Route("api/ai/calls")]
public sealed class AiCallsController : ControllerBase
{
    private readonly ITrialCallService _trial;

    /// <summary>Initializes a new instance of the <see cref="AiCallsController"/> class.</summary>
    /// <param name="trial">Deneme çağrısı servisi.</param>
    public AiCallsController(ITrialCallService trial)
    {
        _trial = trial;
    }

    /// <summary>Deneme çağrısı yapar.</summary>
    /// <param name="request">Model ve efor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç ya da Türkçe nedenli hata.</returns>
    [HttpPost("trial")]
    public Task<IActionResult> Trial([FromBody] TrialCallRequest? request, CancellationToken cancellationToken) =>
        _trial.CallAsync(request, cancellationToken).ToActionResult();
}
