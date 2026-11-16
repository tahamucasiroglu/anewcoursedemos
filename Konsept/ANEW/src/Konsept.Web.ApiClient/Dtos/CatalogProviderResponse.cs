namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Katalogdaki online sağlayıcı (<c>GET api/ai/catalog</c>).</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Name">Görünen ad.</param>
public sealed record CatalogProviderResponse(string Id, string Name);
