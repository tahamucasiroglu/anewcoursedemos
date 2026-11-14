using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Modules.Agents.Api.Controllers.Base;
using TahaKonsept.Modules.Agents.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Api.Controllers;

/// <summary>
/// Eşlenmiş masaüstü programları: <c>GET api/agents</c> (liste + canlı çevrimiçi durumu), <c>DELETE api/agents/{id}</c> (eşlemeyi kaldır).
/// </summary>
[Route("api/agents")]
public sealed class AgentDevicesController : AgentsControllerBase
{
    private readonly IAgentDeviceService _service;

    /// <summary>Oluşturur.</summary>
    /// <param name="service">Cihaz servisi.</param>
    public AgentDevicesController(IAgentDeviceService service)
    {
        _service = service;
    }

    /// <summary>Cihazları listeler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>AgentDeviceInfo[]</c>.</returns>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => ToActionResult(await _service.ListAsync(ct));

    /// <summary>Eşlemeyi kaldırır.</summary>
    /// <param name="id">Cihaz.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) => ToActionResult(await _service.RemoveAsync(id, ct));
}
