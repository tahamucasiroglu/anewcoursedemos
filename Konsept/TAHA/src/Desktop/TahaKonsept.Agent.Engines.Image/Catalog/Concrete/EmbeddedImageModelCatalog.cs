using TahaKonsept.Agent.Engines.Image.Catalog.Base;

namespace TahaKonsept.Agent.Engines.Image.Catalog.Concrete;

/// <summary>Derlemeye gömülü <c>Catalog/image-models.json</c> kataloğu.</summary>
public sealed class EmbeddedImageModelCatalog : ImageModelCatalogBase
{
    /// <summary>Gömülü kaynağın mantıksal adı.</summary>
    public const string ResourceName = "TahaKonsept.Agent.Engines.Image.Catalog.image-models.json";

    /// <inheritdoc />
    protected override Stream OpenCatalogStream() =>
        typeof(EmbeddedImageModelCatalog).Assembly.GetManifestResourceStream(ResourceName)
        ?? throw new InvalidOperationException($"Gömülü kaynak bulunamadı: {ResourceName}");
}
