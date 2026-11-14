using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Web'in Warning+ loglarını veritabanındaki teknik loga ileten iç uç (spec 0008 Req 6; Web veritabanına dokunmaz,
/// Y-5). Servis anahtarı ister (plan 0008, Risk 2): kullanıcıya değil servis anahtarı kimlik şemasına bağlıdır, anonim
/// değildir (plan 0002, Risk 11); filtre ek savunma olarak kalır.
/// </summary>
[ApiController]
[Authorize(Policy = AuthPolicies.InternalService)]
[ServiceFilter(typeof(ServiceKeyAuthorizationFilter))]
public sealed class InternalLogsController : ControllerBase
{
    private readonly ITechnicalLogService _logs;

    /// <summary>Initializes a new instance of the <see cref="InternalLogsController"/> class.</summary>
    /// <param name="logs">Teknik log servisi.</param>
    public InternalLogsController(ITechnicalLogService logs)
    {
        _logs = logs;
    }

    /// <summary>Web loglarını kabul eder; kaynak her zaman Web olarak yazılır.</summary>
    /// <param name="entries">Log kayıtları (en fazla 200).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yazılan kayıt sayısı.</returns>
    [HttpPost("/api/internal/logs")]
    public Task<IActionResult> Forward([FromBody] IReadOnlyList<TechnicalLogEntry> entries, CancellationToken cancellationToken) =>
        _logs.AcceptWebLogsAsync(entries, cancellationToken).ToActionResult();
}
