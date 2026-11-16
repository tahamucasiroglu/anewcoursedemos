namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Model kataloğu (<c>GET api/ai/catalog</c>; spec 0004 Req 6).</summary>
/// <param name="Version">Seed sürümü.</param>
/// <param name="Providers">Sağlayıcılar.</param>
/// <param name="Models">Modeller.</param>
public sealed record CatalogResponse(string Version, IReadOnlyList<CatalogProviderResponse> Providers, IReadOnlyList<CatalogModelResponse> Models);
