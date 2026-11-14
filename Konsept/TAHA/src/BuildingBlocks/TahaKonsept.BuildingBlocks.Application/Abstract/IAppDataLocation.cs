namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// Uygulama veri klasörü (<c>App_Data</c>): anahtarlar, geçici artefaktlar vb.
/// Çözüm sırası: yapılandırma <c>App:DataDirectory</c> → ortam değişkeni <c>TAHAKONSEPT_DATA</c> → <c>&lt;ContentRoot&gt;/App_Data</c>.
/// </summary>
public interface IAppDataLocation
{
    /// <summary>Veri klasörünün mutlak yolu.</summary>
    string Root { get; }

    /// <summary>Veri klasörü altında bir yol üretir (klasörü oluşturmaz).</summary>
    /// <param name="segments">Alt yol parçaları.</param>
    /// <returns>Mutlak yol.</returns>
    string GetPath(params string[] segments);

    /// <summary>Veri klasörü altında bir klasör yolunu döner ve yoksa oluşturur.</summary>
    /// <param name="segments">Alt yol parçaları.</param>
    /// <returns>Mutlak klasör yolu.</returns>
    string EnsureDirectory(params string[] segments);
}
