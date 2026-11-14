using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using TahaKonsept.Shared.Contracts.Providers;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Onboarding.Base;

namespace TahaKonsept.Web.Services.Onboarding.Concrete;

/// <summary>
/// Başlangıç bilgilerini var olan API uçlarından toplar (yeni API ucu yok; çağrılar sırayla yapılır ki token yenileme
/// yarışmasın): <c>GET api/providers/keys</c> + <c>GET api/providers/models?capability=Text</c> (anahtarı kayıtlı ve metin
/// modeli olan platformlar; yerel OpenAI uyumlu sunucu katalogda dinamik olduğu için ayrıca sayılır), <c>GET api/agents</c>
/// (<c>isOnline</c>), <c>GET api/profiles</c>, <c>GET api/concepts</c> (silinmemiş hepsi), <c>GET api/production/videos?take=1</c>.
/// </summary>
public sealed class OnboardingService : OnboardingServiceBase
{
    private readonly IApiClient _api;
    private readonly IHttpContextAccessor _http;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="api">API istemcisi.</param>
    /// <param name="http">İstek bağlamı (önbellek kapsamı için kullanıcı).</param>
    /// <param name="cache">Bellek önbelleği.</param>
    public OnboardingService(IApiClient api, IHttpContextAccessor http, IMemoryCache cache)
        : base(cache)
    {
        _api = api;
        _http = http;
    }

    /// <inheritdoc />
    protected override string CacheScope =>
        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? _http.HttpContext?.User.Identity?.Name
        ?? "anonim";

    /// <inheritdoc />
    protected override async Task<OnboardingFacts> CollectFactsAsync(CancellationToken ct)
    {
        var keys = await _api.GetAsync<List<ApiKeyInfo>>("api/providers/keys", ct);
        var models = await _api.GetAsync<JsonElement>("api/providers/models?capability=Text", ct);
        var agents = await _api.GetAsync<JsonElement>("api/agents", ct);
        var profiles = await _api.GetAsync<JsonElement>("api/profiles", ct);
        var concepts = await _api.GetAsync<JsonElement>("api/concepts", ct);
        var videos = await _api.GetAsync<JsonElement>("api/production/videos?take=1", ct);

        var agentItems = agents.IsSuccess ? Items(agents.Data) : null;
        return new OnboardingFacts
        {
            LlmKeyPlatforms = keys.IsSuccess && models.IsSuccess ? LlmKeyPlatforms(keys.Data ?? [], models.Data) : null,
            PairedAgents = agentItems?.Count,
            OnlineAgents = agentItems?.Count(IsOnline),
            Profiles = profiles.IsSuccess ? Items(profiles.Data).Count : null,
            Concepts = concepts.IsSuccess ? Items(concepts.Data).Count : null,
            Videos = videos.IsSuccess ? Items(videos.Data).Count : null,
        };
    }

    private static List<string> LlmKeyPlatforms(List<ApiKeyInfo> keys, JsonElement models)
    {
        var textPlatforms = Items(models)
            .Select(m => ReadString(m, "platform"))
            .OfType<string>()
            .Append(ProviderPlatforms.LocalOpenAi)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return keys
            .Where(k => k.Configured && textPlatforms.Contains(k.Platform))
            .Select(k => string.IsNullOrWhiteSpace(k.DisplayName) ? k.Platform : k.DisplayName)
            .ToList();
    }

    private static bool IsOnline(JsonElement agent) =>
        agent.ValueKind == JsonValueKind.Object && agent.TryGetProperty("isOnline", out var online) && online.ValueKind == JsonValueKind.True;

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static List<JsonElement> Items(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Array)
        {
            return [.. data.EnumerateArray()];
        }

        return data.ValueKind == JsonValueKind.Object && data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
            ? [.. items.EnumerateArray()]
            : [];
    }
}
