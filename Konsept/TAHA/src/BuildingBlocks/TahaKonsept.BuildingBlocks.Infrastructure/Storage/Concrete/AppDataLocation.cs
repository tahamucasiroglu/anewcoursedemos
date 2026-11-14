using Microsoft.Extensions.Configuration;
using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Storage.Concrete;

/// <summary>
/// <see cref="IAppDataLocation"/> uygulaması. <see cref="Resolve"/> sırası: <c>App:DataDirectory</c> yapılandırması →
/// <c>TAHAKONSEPT_DATA</c> ortam değişkeni → <c>&lt;contentRoot&gt;/App_Data</c>.
/// </summary>
public sealed class AppDataLocation : IAppDataLocation
{
    /// <summary>Veri klasörü ortam değişkeni adı.</summary>
    public const string EnvironmentVariable = "TAHAKONSEPT_DATA";

    /// <summary>Veri klasörü yapılandırma anahtarı.</summary>
    public const string ConfigurationKey = "App:DataDirectory";

    /// <summary>Verilen kök klasörle oluşturur.</summary>
    /// <param name="root">Kök klasör (göreli ise mutlak yola çevrilir).</param>
    public AppDataLocation(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
    }

    /// <summary>Veri klasörünün mutlak yolu.</summary>
    public string Root { get; }

    /// <summary>Yapılandırma/ortamdan veri klasörünü çözer.</summary>
    /// <param name="configuration">Yapılandırma (opsiyonel).</param>
    /// <param name="contentRootPath">Uygulama içerik kökü.</param>
    /// <returns>Konum.</returns>
    public static AppDataLocation Resolve(IConfiguration? configuration, string contentRootPath)
    {
        var configured = configuration?[ConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return new AppDataLocation(Path.IsPathRooted(configured) ? configured : Path.Combine(contentRootPath, configured));
        }

        var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return new AppDataLocation(fromEnv);
        }

        return new AppDataLocation(Path.Combine(contentRootPath, "App_Data"));
    }

    /// <summary>Veri klasörü altında bir yol üretir.</summary>
    /// <param name="segments">Alt yol parçaları.</param>
    /// <returns>Mutlak yol.</returns>
    public string GetPath(params string[] segments) => Path.Combine([Root, .. segments]);

    /// <summary>Klasör yolunu döner, yoksa oluşturur.</summary>
    /// <param name="segments">Alt yol parçaları.</param>
    /// <returns>Mutlak klasör yolu.</returns>
    public string EnsureDirectory(params string[] segments)
    {
        var path = GetPath(segments);
        Directory.CreateDirectory(path);
        return path;
    }
}
