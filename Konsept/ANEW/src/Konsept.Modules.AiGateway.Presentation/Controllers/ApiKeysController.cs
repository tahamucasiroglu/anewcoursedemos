using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.AiGateway.Presentation.Controllers;

/// <summary>
/// İsteği yapan kullanıcının API key'leri (spec 0004 Req 7–13, H-2). Yolda yalnızca sağlayıcı adı vardır; satır her zaman oturumdaki
/// kullanıcınındır (BR-1, admin dahil başka kullanıcıya yol yok). Yanıtlarda tam key yoktur. Giriş ister (default-deny).
/// </summary>
[ApiController]
[Route("api/ai/keys")]
public sealed class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _keys;

    /// <summary>Initializes a new instance of the <see cref="ApiKeysController"/> class.</summary>
    /// <param name="keys">API key servisi.</param>
    public ApiKeysController(IApiKeyService keys)
    {
        _keys = keys;
    }

    /// <summary>Katalogdaki her online sağlayıcı için kullanıcının satırı.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 satırlar · 401 kimliksiz.</returns>
    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _keys.ListAsync(cancellationToken).ToActionResult();

    /// <summary>Key'i doğrular ve kaydeder (varsa eskisinin yerine).</summary>
    /// <param name="provider">Sağlayıcı kimliği.</param>
    /// <param name="request">Key.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 satır · 400 biçim / sağlayıcı reddi · 404 bilinmeyen sağlayıcı · 401 kimliksiz.</returns>
    [HttpPut("{provider}")]
    public Task<IActionResult> Save(string provider, [FromBody] SaveApiKeyRequest request, CancellationToken cancellationToken) =>
        _keys.SaveAsync(provider, request, cancellationToken).ToActionResult();

    /// <summary>Key'i kalıcı siler.</summary>
    /// <param name="provider">Sağlayıcı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 "Girilmemiş" satırı · 404 bilinmeyen sağlayıcı · 401 kimliksiz.</returns>
    [HttpDelete("{provider}")]
    public Task<IActionResult> Delete(string provider, CancellationToken cancellationToken) =>
        _keys.DeleteAsync(provider, cancellationToken).ToActionResult();
}
