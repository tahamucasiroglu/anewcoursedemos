using System.Text.Json;
using System.Text.Json.Serialization;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;

namespace TahaKonsept.Agent.Engines.Image.Catalog.Base;

/// <summary>
/// Kataloğu bir JSON akışından tembel yükleyen ortak taban. Alt sınıf yalnızca <see cref="OpenCatalogStream"/> sağlar.
/// </summary>
public abstract class ImageModelCatalogBase : IImageModelCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Lazy<IReadOnlyList<ImageModelDefinition>> _models;

    /// <summary>Tembel yükleyiciyi hazırlar.</summary>
    protected ImageModelCatalogBase()
    {
        _models = new Lazy<IReadOnlyList<ImageModelDefinition>>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IReadOnlyList<ImageModelDefinition> Models => _models.Value;

    /// <inheritdoc />
    public ImageModelDefinition? Find(string key) =>
        Models.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Katalog JSON akışını açar.</summary>
    /// <returns>Okunabilir akış.</returns>
    protected abstract Stream OpenCatalogStream();

    private IReadOnlyList<ImageModelDefinition> Load()
    {
        using var stream = OpenCatalogStream();
        var document = JsonSerializer.Deserialize<ImageModelCatalogDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Görsel model kataloğu okunamadı.");
        return document.Models;
    }
}
