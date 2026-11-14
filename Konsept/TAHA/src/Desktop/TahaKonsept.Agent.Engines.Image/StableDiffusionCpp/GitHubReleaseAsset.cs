using System.Text.Json.Serialization;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;

/// <summary>GitHub releases API'deki tek indirilebilir dosya.</summary>
/// <param name="Name">Dosya adı (ör. <c>sd-master-3bad892-bin-win-cuda12-x64.zip</c>).</param>
/// <param name="Size">Boyut (bayt).</param>
/// <param name="BrowserDownloadUrl">İndirme adresi.</param>
public sealed record GitHubReleaseAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
