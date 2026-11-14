namespace TahaKonsept.Agent.Engines.Image.Catalog;

/// <summary>Gömülü <c>image-models.json</c> dosyasının kök nesnesi.</summary>
/// <param name="Version">Katalog biçim sürümü.</param>
/// <param name="Models">Modeller.</param>
public sealed record ImageModelCatalogDocument(int Version, IReadOnlyList<ImageModelDefinition> Models);
