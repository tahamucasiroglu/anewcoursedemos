namespace TahaKonsept.Agent.Core.Http;

/// <summary><c>IHttpClientFactory</c> adlandırılmış istemci adları.</summary>
public static class AgentHttpClientNames
{
    /// <summary>Kimlik doğrulamasız API çağrıları (eşleme, yenileme).</summary>
    public const string Anonymous = "TahaKonsept.Agent.Anonymous";

    /// <summary>Bearer token'lı API çağrıları.</summary>
    public const string Api = "TahaKonsept.Agent.Api";

    /// <summary>Ön gereksinim indirmeleri (internet).</summary>
    public const string Downloads = "TahaKonsept.Agent.Downloads";
}
