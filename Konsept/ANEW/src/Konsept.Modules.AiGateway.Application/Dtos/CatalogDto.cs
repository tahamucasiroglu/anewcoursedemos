namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>Katalog listesi (spec 0004 Req 6): seed sürümü, online sağlayıcılar ve modeller (kullanıcıya göre etiketli).</summary>
/// <param name="Version">Seed sürümü.</param>
/// <param name="Providers">Sağlayıcılar.</param>
/// <param name="Models">Modeller.</param>
public sealed record CatalogDto(string Version, IReadOnlyList<CatalogProviderDto> Providers, IReadOnlyList<CatalogModelDto> Models);
