using System.Text.Json.Nodes;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Sahte işleyicinin kaydettiği istek (yöntem, adres, başlıklar, gövde).
/// </summary>
/// <param name="Method">HTTP yöntemi.</param>
/// <param name="Uri">Adres.</param>
/// <param name="Headers">Başlıklar.</param>
/// <param name="Body">Gövde.</param>
public sealed record CapturedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string Body)
{
    /// <summary>Gövdeyi JSON nesnesi olarak çözer.</summary>
    public JsonObject Json => JsonNode.Parse(Body)!.AsObject();
}
