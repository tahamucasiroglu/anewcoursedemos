using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Katalog servisi.</summary>
/// <param name="catalog">Katalog.</param>
/// <param name="keys">Kullanıcının key durumları.</param>
public sealed class CatalogService(ICatalogSource catalog, IApiKeyService keys) : CatalogServiceBase(catalog, keys);
