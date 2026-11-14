using System.Net.Http.Headers;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Models;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.KeyTesting.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.KeyTesting.Concrete;

/// <summary>
/// Platform başına ücretsiz doğrulama çağrıları: Anthropic model listesi (resmi SDK), OpenAI / Moonshot / Z.ai <c>GET models</c>,
/// OpenRouter <c>GET key</c>, ElevenLabs <c>GET user</c>, Gemini <c>GET models</c>, Azure Speech <c>issueToken</c>,
/// Cartesia <c>GET voices</c>, fal <c>GET models</c>, BFL <c>GET credits</c>, Replicate <c>GET account</c>, Yerel LLM <c>GET {kök}/models</c>.
/// </summary>
public sealed class HttpApiKeyTester : ApiKeyTesterBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="http">HTTP istemcisi (kısa zaman aşımı).</param>
    public HttpApiKeyTester(HttpClient http)
        : base(http)
    {
    }

    /// <inheritdoc />
    protected override async Task<int?> TestSpecialAsync(ProviderCredential credential, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(credential);
        if (credential.Platform != ProviderPlatforms.Anthropic)
        {
            return null;
        }

        var client = new AnthropicClient { ApiKey = credential.ApiKey, HttpClient = Http, MaxRetries = 0 };
        try
        {
            await client.Models.List(new ModelListParams { Limit = 1 }, ct);
            return 200;
        }
        catch (AnthropicApiException ex)
        {
            return (int)ex.StatusCode;
        }
    }

    /// <inheritdoc />
    protected override HttpRequestMessage? BuildRequest(ProviderCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var key = credential.ApiKey;
        return credential.Platform switch
        {
            ProviderPlatforms.OpenAi => Bearer("https://api.openai.com/v1/models", key),
            ProviderPlatforms.OpenRouter => Bearer("https://openrouter.ai/api/v1/key", key),
            ProviderPlatforms.Zai => Bearer("https://api.z.ai/api/paas/v4/models", key),
            ProviderPlatforms.Moonshot => Bearer("https://api.moonshot.ai/v1/models", key),
            ProviderPlatforms.Replicate => Bearer("https://api.replicate.com/v1/account", key),
            ProviderPlatforms.ElevenLabs => WithHeader(HttpMethod.Get, "https://api.elevenlabs.io/v1/user", "xi-api-key", key),
            ProviderPlatforms.Google => WithHeader(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models?pageSize=1", "x-goog-api-key", key),
            ProviderPlatforms.Cartesia => CartesiaRequest(key),
            ProviderPlatforms.Fal => FalRequest(key),
            ProviderPlatforms.Bfl => WithHeader(HttpMethod.Get, "https://api.bfl.ai/v1/credits", "x-key", key),
            ProviderPlatforms.AzureSpeech => AzureRequest(key, credential.Region),
            ProviderPlatforms.LocalOpenAi => LocalOpenAiRequest(credential.BaseUrl, key),
            _ => null,
        };
    }

    /// <summary>Bearer yetkili GET isteği.</summary>
    /// <param name="url">Adres.</param>
    /// <param name="key">Anahtar.</param>
    private static HttpRequestMessage Bearer(string url, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return request;
    }

    /// <summary>Özel başlıklı istek.</summary>
    /// <param name="method">Yöntem.</param>
    /// <param name="url">Adres.</param>
    /// <param name="header">Başlık adı.</param>
    /// <param name="key">Anahtar.</param>
    private static HttpRequestMessage WithHeader(HttpMethod method, string url, string header, string key)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation(header, key);
        return request;
    }

    /// <summary>Cartesia isteği (sürüm başlığı gerekir).</summary>
    /// <param name="key">Anahtar.</param>
    private static HttpRequestMessage CartesiaRequest(string key)
    {
        var request = WithHeader(HttpMethod.Get, "https://api.cartesia.ai/voices?limit=1", "X-API-Key", key);
        request.Headers.TryAddWithoutValidation("Cartesia-Version", "2025-04-16");
        return request;
    }

    /// <summary>fal isteği (<c>Authorization: Key …</c>).</summary>
    /// <param name="key">Anahtar.</param>
    private static HttpRequestMessage FalRequest(string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.fal.ai/v1/models?limit=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Key", key);
        return request;
    }

    /// <summary>Yerel LLM isteği: <c>GET {kök}/models</c>; anahtar varsa Bearer (kök adres yoksa null).</summary>
    /// <param name="baseUrl">Kök adres.</param>
    /// <param name="key">Anahtar (boş olabilir).</param>
    private static HttpRequestMessage? LocalOpenAiRequest(string? baseUrl, string key)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var url = baseUrl.TrimEnd('/') + "/models";
        return string.IsNullOrEmpty(key) ? new HttpRequestMessage(HttpMethod.Get, url) : Bearer(url, key);
    }

    /// <summary>Azure Speech jeton isteği (bölge gerekir).</summary>
    /// <param name="key">Anahtar.</param>
    /// <param name="region">Bölge.</param>
    private static HttpRequestMessage? AzureRequest(string key, string? region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            return null;
        }

        var request = WithHeader(HttpMethod.Post, $"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken", "Ocp-Apim-Subscription-Key", key);
        request.Content = new StringContent(string.Empty);
        return request;
    }
}
