using System.Text.Json.Serialization;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;

/// <summary>GitHub releases API yanıtının kullanılan kısmı.</summary>
/// <param name="TagName">Sürüm etiketi (ör. <c>master-1094-3bad892</c>).</param>
/// <param name="Assets">Dosyalar.</param>
public sealed record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("assets")] IReadOnlyList<GitHubReleaseAsset> Assets);
