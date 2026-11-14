using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.AgentGateway.Presentation.Controllers;

/// <summary>
/// Ajan komutları. Bu spec'te kimliksiz (yalnızca yerel ağ, D5); 0002 giriş ister hale getirir.
/// </summary>
[ApiController]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IAgentTestService _testService;

    /// <summary>Initializes a new instance of the <see cref="AgentController"/> class.</summary>
    /// <param name="testService">"Ajanı test et" servisi.</param>
    public AgentController(IAgentTestService testService)
    {
        _testService = testService;
    }

    /// <summary>Ajana test komutu gönderir; ajanın versiyonunu ve makine adını döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 ajan bilgisi · 503 ajan bağlı değil · 504 zaman aşımı.</returns>
    [HttpPost("test")]
    public Task<IActionResult> Test(CancellationToken cancellationToken) =>
        _testService.TestAsync(cancellationToken).ToActionResult();
}
