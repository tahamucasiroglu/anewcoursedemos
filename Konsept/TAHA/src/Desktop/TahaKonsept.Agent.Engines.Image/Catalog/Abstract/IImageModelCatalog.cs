namespace TahaKonsept.Agent.Engines.Image.Catalog.Abstract;

/// <summary>Yerel görsel model kataloğu: model tanımlarını ve dosya listelerini verir.</summary>
public interface IImageModelCatalog
{
    /// <summary>Tüm modeller.</summary>
    IReadOnlyList<ImageModelDefinition> Models { get; }

    /// <summary>Anahtara göre modeli bulur (büyük/küçük harf duyarsız).</summary>
    /// <param name="key">Model anahtarı.</param>
    /// <returns>Model ya da <c>null</c>.</returns>
    ImageModelDefinition? Find(string key);
}
