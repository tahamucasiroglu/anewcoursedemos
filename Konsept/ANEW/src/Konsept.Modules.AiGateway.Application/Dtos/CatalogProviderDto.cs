namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>Katalogdaki online sağlayıcı (filtre ve satır listesi için).</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Name">Görünen ad.</param>
public sealed record CatalogProviderDto(string Id, string Name);
