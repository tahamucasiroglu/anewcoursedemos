using System.Text.Json;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Settings;

/// <summary>
/// Ajan ayarlarını ajan klasöründen okur: <c>agentsettings.json</c>, varsa üzerine
/// <c>agentsettings.Local.json</c> (repoya girmez; geliştiricinin Pi adresi gibi yerel değerler).
/// </summary>
public static class AgentSettingsLoader
{
    /// <summary>Varsayılan API adresi.</summary>
    public const string DefaultServerUrl = "http://localhost:8001/";

    /// <summary>Varsayılan site adresi.</summary>
    public const string DefaultSiteUrl = "http://localhost:8000/";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Ayarları yükler; dosya yoksa varsayılanlar kullanılır.</summary>
    /// <param name="directory">Ajan klasörü.</param>
    /// <returns>Ayarlar.</returns>
    public static AgentSettings Load(string directory)
    {
        var baseFile = Read(Path.Combine(directory, "agentsettings.json"));
        var local = Read(Path.Combine(directory, "agentsettings.Local.json"));
        var server = local?.ServerUrl ?? baseFile?.ServerUrl ?? DefaultServerUrl;
        var site = local?.SiteUrl ?? baseFile?.SiteUrl ?? DefaultSiteUrl;
        var delay = local?.ReconnectDelaySeconds ?? baseFile?.ReconnectDelaySeconds ?? 3;
        return new AgentSettings(WithSlash(server), WithSlash(site), TimeSpan.FromSeconds(delay));
    }

    private static AgentSettingsFile? Read(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<AgentSettingsFile>(File.ReadAllText(path), Json) : null;

    private static Uri WithSlash(string url) =>
        new(url.EndsWith('/') ? url : url + "/", UriKind.Absolute);
}
