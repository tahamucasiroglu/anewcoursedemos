using System.Text.Json;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.Catalog.Concrete;

/// <summary>
/// Gömülü <c>Catalog/catalog.json</c>'dan yüklenen model kataloğu (tekil; açılışta bir kez okunur, anahtarlar benzersiz olmalı).
/// </summary>
public sealed class EmbeddedModelCatalog : ModelCatalogBase
{
    /// <summary>Gömülü kaynak adı.</summary>
    public const string ResourceName = "TahaKonsept.Providers.catalog.json";

    private readonly Lazy<IReadOnlyList<ModelInfo>> _models = new(Load);

    /// <inheritdoc />
    public override IReadOnlyList<ModelInfo> All => _models.Value;

    /// <summary>Kaynağı okur ve doğrular.</summary>
    /// <exception cref="InvalidOperationException">Kaynak yoksa, bozuksa ya da anahtar tekrarı varsa.</exception>
    private static IReadOnlyList<ModelInfo> Load()
    {
        using var stream = typeof(EmbeddedModelCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Model kataloğu bulunamadı: {ResourceName}");
        var models = JsonSerializer.Deserialize<List<ModelInfo>>(stream, ContractJson.Options)
            ?? throw new InvalidOperationException("Model kataloğu boş.");
        var duplicate = models.GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Model kataloğunda tekrar eden anahtar: {duplicate.Key}");
        }

        return models;
    }
}
