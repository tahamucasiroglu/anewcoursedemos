using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Modules.Agents.Api.Controllers.Base;
using TahaKonsept.Modules.Agents.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Api.Controllers;

/// <summary>
/// Online üretilen geçici dosyalar (ajan indirir): <c>GET api/agents/artifacts/{id}</c> (dosya, Range destekli),
/// <c>DELETE api/agents/artifacts/{id}</c> (indirildi onayı → dosya silinir). Oturum (ajan JWT'si) gerekli; sahiplik kontrol edilir.
/// </summary>
[Route("api/agents/artifacts")]
public sealed class AgentArtifactsController : AgentsControllerBase
{
    private readonly IAgentArtifactService _service;

    /// <summary>Oluşturur.</summary>
    /// <param name="service">Artefakt servisi.</param>
    public AgentArtifactsController(IAgentArtifactService service)
    {
        _service = service;
    }

    /// <summary>Dosyayı indirir.</summary>
    /// <param name="id">Artefakt.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Dosya.</returns>
    [HttpGet("{id:guid}")]
    [Produces("application/octet-stream", "application/problem+json")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct) => ToFileResult(await _service.DownloadAsync(id, ct));

    /// <summary>İndirmeyi onaylar (dosya silinir).</summary>
    /// <param name="id">Artefakt.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct) => ToActionResult(await _service.AcknowledgeAsync(id, ct));
}
