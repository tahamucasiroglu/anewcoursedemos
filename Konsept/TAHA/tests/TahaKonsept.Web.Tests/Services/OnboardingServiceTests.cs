using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shouldly;
using TahaKonsept.Shared.Contracts.Providers;
using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Onboarding.Concrete;
using Xunit;

namespace TahaKonsept.Web.Tests.Services;

/// <summary>
/// <see cref="OnboardingService"/> testleri (sahte <see cref="IApiClient"/>): LLM anahtarı yalnızca metin modeli olan
/// platformlarda sayılır, yerel LLM sayılır, ajan çevrimiçi sayısı, API hatasında Unknown, önbellek ve <c>refresh</c>.
/// </summary>
public sealed class OnboardingServiceTests
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <summary>Varsayılan cevaplar: boş kurulum (Anthropic metin modeli katalogda).</summary>
    public OnboardingServiceTests()
    {
        Keys(Key(ProviderPlatforms.Anthropic, "Anthropic (Claude)", false), Key(ProviderPlatforms.ElevenLabs, "ElevenLabs", false));
        Json("api/providers/models?capability=Text", """[{"platform":"anthropic","key":"claude-opus-5-5"},{"platform":"openai","key":"gpt-6"}]""");
        Json("api/agents", "[]");
        Json("api/profiles", "[]");
        Json("api/concepts", "[]");
        Json("api/production/videos?take=1", "[]");
    }

    /// <summary>Yalnızca ses platformunun anahtarı LLM adımını tamamlamaz.</summary>
    [Fact]
    public async Task Speech_only_key_does_not_complete_llm_step()
    {
        Keys(Key(ProviderPlatforms.ElevenLabs, "ElevenLabs", true));

        var status = await CreateService().GetStatusAsync(refresh: true, TestContext.Current.CancellationToken);

        status.Steps[0].State.ShouldBe(OnboardingStepState.Waiting);
    }

    /// <summary>Metin modeli olan platformun anahtarı ve yerel LLM sunucusu adımı tamamlar.</summary>
    [Fact]
    public async Task Text_platform_and_local_llm_keys_complete_llm_step()
    {
        Keys(Key(ProviderPlatforms.Anthropic, "Anthropic (Claude)", true), Key(ProviderPlatforms.LocalOpenAi, "Yerel LLM", true));

        var status = await CreateService().GetStatusAsync(refresh: true, TestContext.Current.CancellationToken);

        status.Steps[0].State.ShouldBe(OnboardingStepState.Done);
        status.Steps[0].Detail.ShouldBe("Anthropic (Claude), Yerel LLM kayıtlı.");
    }

    /// <summary>Ajan, profil, konsept ve video sayıları cevaplardan okunur (sayfalı <c>{ items }</c> de olur).</summary>
    [Fact]
    public async Task Counts_agents_profiles_concepts_and_videos()
    {
        Json("api/agents", """[{"id":"1","isOnline":true},{"id":"2","isOnline":false}]""");
        Json("api/profiles", """[{"id":"p"}]""");
        Json("api/concepts", """{"items":[{"id":"c1"},{"id":"c2"}]}""");
        Json("api/production/videos?take=1", """[{"id":"v"}]""");

        var status = await CreateService().GetStatusAsync(refresh: true, TestContext.Current.CancellationToken);

        status.Steps[1].Detail.ShouldBe("2 bilgisayar eşli · 1 çevrimiçi.");
        status.Steps[2].State.ShouldBe(OnboardingStepState.Done);
        status.Steps[3].Detail.ShouldBe("2 konsept.");
        status.Steps[4].State.ShouldBe(OnboardingStepState.Done);
    }

    /// <summary>API hatası adımı Unknown yapar ve sonuç önbelleğe yazılmaz.</summary>
    [Fact]
    public async Task Api_failure_is_unknown_and_not_cached()
    {
        _api.GetAsync<JsonElement>("api/agents", Arg.Any<CancellationToken>())
            .Returns(ApiCallResult<JsonElement>.Failure(503, "API'ye ulaşılamadı."));
        var service = CreateService();

        var first = await service.GetStatusAsync(refresh: false, TestContext.Current.CancellationToken);
        await service.GetStatusAsync(refresh: false, TestContext.Current.CancellationToken);

        first.Steps[1].State.ShouldBe(OnboardingStepState.Unknown);
        await _api.Received(2).GetAsync<JsonElement>("api/agents", Arg.Any<CancellationToken>());
    }

    /// <summary>Önbellek ikinci çağrıda API'ye gitmez; <c>refresh</c> önbelleği atlar.</summary>
    [Fact]
    public async Task Caches_until_refresh_is_requested()
    {
        var service = CreateService();

        await service.GetStatusAsync(refresh: false, TestContext.Current.CancellationToken);
        await service.GetStatusAsync(refresh: false, TestContext.Current.CancellationToken);
        await _api.Received(1).GetAsync<JsonElement>("api/profiles", Arg.Any<CancellationToken>());

        await service.GetStatusAsync(refresh: true, TestContext.Current.CancellationToken);
        await _api.Received(2).GetAsync<JsonElement>("api/profiles", Arg.Any<CancellationToken>());
    }

    private OnboardingService CreateService()
    {
        var http = Substitute.For<IHttpContextAccessor>();
        http.HttpContext.Returns(new DefaultHttpContext());
        return new OnboardingService(_api, http, _cache);
    }

    private static ApiKeyInfo Key(string platform, string name, bool configured) =>
        new() { Platform = platform, DisplayName = name, Configured = configured };

    private void Keys(params ApiKeyInfo[] keys) =>
        _api.GetAsync<List<ApiKeyInfo>>("api/providers/keys", Arg.Any<CancellationToken>())
            .Returns(ApiCallResult<List<ApiKeyInfo>>.Success([.. keys]));

    private void Json(string url, string json) =>
        _api.GetAsync<JsonElement>(url, Arg.Any<CancellationToken>())
            .Returns(ApiCallResult<JsonElement>.Success(JsonDocument.Parse(json).RootElement.Clone()));
}
