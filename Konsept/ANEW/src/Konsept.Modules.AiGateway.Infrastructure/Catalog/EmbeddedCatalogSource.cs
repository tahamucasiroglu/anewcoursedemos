using System.Text.Json;
using System.Text.Json.Serialization;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Modules.AiGateway.Infrastructure.Catalog;

/// <summary>
/// Derlemeye gömülü sürümlü katalog seed'i (<c>Catalog/catalog.json</c>; spec 0004 Req 5). Token limiti, zaman aşımı, fiyat ve
/// doğrulama uçları yalnızca bu dosyadadır; davranışta sabit değer yoktur (AC-2).
/// </summary>
public sealed class EmbeddedCatalogSource : ICatalogSource
{
    /// <summary>Gömülü kaynağın adı.</summary>
    public const string ResourceName = "Konsept.AiGateway.catalog.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    private static readonly Lazy<CatalogSeed> Embedded = new(LoadEmbedded);

    /// <inheritdoc />
    public CatalogSeed Catalog => Embedded.Value;

    /// <summary>Seed JSON'unu okur (testler değiştirilmiş seed verir).</summary>
    /// <param name="json">JSON.</param>
    /// <returns>Katalog.</returns>
    public static CatalogSeed Parse(string json) =>
        JsonSerializer.Deserialize<CatalogSeed>(json, Json) ?? throw new InvalidOperationException("Katalog seed'i boş.");

    /// <summary>Gömülü seed'in ham JSON'u.</summary>
    /// <returns>JSON.</returns>
    public static string ReadEmbeddedJson()
    {
        using var stream = typeof(EmbeddedCatalogSource).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Katalog seed'i derlemede yok.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static CatalogSeed LoadEmbedded() => Parse(ReadEmbeddedJson());
}
