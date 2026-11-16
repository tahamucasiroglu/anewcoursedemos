using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.AiGateway.Presentation.Controllers;

/// <summary>
/// Model kataloğu (spec 0004 Req 1–6): salt okunur; her model isteği yapan kullanıcının key'lerine göre etiketlidir. Giriş ister
/// (default-deny).
/// </summary>
[ApiController]
[Route("api/ai/catalog")]
public sealed class AiCatalogController : ControllerBase
{
    private readonly ICatalogService _catalog;

    /// <summary>Initializes a new instance of the <see cref="AiCatalogController"/> class.</summary>
    /// <param name="catalog">Katalog servisi.</param>
    public AiCatalogController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    /// <summary>Kataloğu getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 katalog · 401 kimliksiz.</returns>
    [HttpGet]
    public Task<IActionResult> Get(CancellationToken cancellationToken) =>
        _catalog.GetAsync(cancellationToken).ToActionResult();
}
