using System.Text.Json;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Settings;

/// <summary>
/// Ajan ayarlarını ajan klasöründen okur: <c>agentsettings.json</c>, varsa üzerine
/// <c>agentsettings.Local.json</c> (repoya girmez; geliştiricinin Pi adresi gibi yerel değerler). İkisi de yoksa (siteden indirilen
/// tek exe, spec 0009 Req 1) yayında exe'ye gömülen adresler (<c>AgentServerUrl</c>/<c>AgentSiteUrl</c> AssemblyMetadata), o da yoksa
/// localhost.
/// </summary>
public static class AgentSettingsLoader
{
    /// <summary>Varsayılan API adresi.</summary>
    public const string DefaultServerUrl = "http://localhost:8001/";

    /// <summary>Varsayılan site adresi.</summary>
    public const string DefaultSiteUrl = "http://localhost:8000/";

    /// <summary>Ayar dosyası yokken sahne render süresi sınırı (sn; plan 0018 ölçümü: 2 sn'lik sahne bu PC'de ~12 sn).</summary>
    public const double DefaultRenderSceneTimeoutSeconds = 600;

    /// <summary>Ayar dosyası yokken render bellek sınırı (MB).</summary>
    public const long DefaultRenderMemoryMb = 4096;

    /// <summary>Ayar dosyası yokken render süreç sınırı.</summary>
    public const int DefaultRenderMaxProcesses = 64;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Gömülü sunucu adresinin AssemblyMetadata anahtarı.</summary>
    public const string ServerUrlMetadata = "AgentServerUrl";

    /// <summary>Gömülü site adresinin AssemblyMetadata anahtarı.</summary>
    public const string SiteUrlMetadata = "AgentSiteUrl";

    /// <summary>Ayarları yükler; dosya yoksa gömülü adresler, onlar da yoksa varsayılanlar kullanılır.</summary>
    /// <param name="directory">Ajan klasörü.</param>
    /// <param name="embedded">Exe'ye gömülü AssemblyMetadata (anahtar → değer); yoksa <c>null</c>.</param>
    /// <returns>Ayarlar.</returns>
    public static AgentSettings Load(string directory, IReadOnlyDictionary<string, string?>? embedded = null)
    {
        var baseFile = Read(Path.Combine(directory, "agentsettings.json"));
        var local = Read(Path.Combine(directory, "agentsettings.Local.json"));
        var server = local?.ServerUrl ?? baseFile?.ServerUrl ?? Embedded(embedded, ServerUrlMetadata) ?? DefaultServerUrl;
        var site = local?.SiteUrl ?? baseFile?.SiteUrl ?? Embedded(embedded, SiteUrlMetadata) ?? DefaultSiteUrl;
        var delay = local?.ReconnectDelaySeconds ?? baseFile?.ReconnectDelaySeconds ?? 3;
        var render = new RenderLimits(
            TimeSpan.FromSeconds(local?.RenderSceneTimeoutSeconds ?? baseFile?.RenderSceneTimeoutSeconds ?? DefaultRenderSceneTimeoutSeconds),
            (local?.RenderMemoryMb ?? baseFile?.RenderMemoryMb ?? DefaultRenderMemoryMb) * 1024 * 1024,
            local?.RenderMaxProcesses ?? baseFile?.RenderMaxProcesses ?? DefaultRenderMaxProcesses);
        return new AgentSettings(WithSlash(server), WithSlash(site), TimeSpan.FromSeconds(delay), render);
    }

    /// <summary>Derlemenin AssemblyMetadata değerlerini okur.</summary>
    /// <param name="assembly">Derleme (ajan exe'si).</param>
    /// <returns>Anahtar → değer.</returns>
    public static IReadOnlyDictionary<string, string?> ReadMetadata(System.Reflection.Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var attribute in System.Reflection.CustomAttributeExtensions.GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>(assembly))
        {
            values[attribute.Key] = attribute.Value;
        }

        return values;
    }

    private static string? Embedded(IReadOnlyDictionary<string, string?>? embedded, string key) =>
        embedded is not null && embedded.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static AgentSettingsFile? Read(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<AgentSettingsFile>(File.ReadAllText(path), Json) : null;

    private static Uri WithSlash(string url) =>
        new(url.EndsWith('/') ? url : url + "/", UriKind.Absolute);
}
