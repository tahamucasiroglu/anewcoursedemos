using Shouldly;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.TextModels;

/// <summary>
/// OpenRouter, GLM ve Kimi'nin OpenAI-uyumlu tabandan ayrıldığı noktalar.
/// </summary>
public sealed class OpenAiCompatibleVariantsTests
{
    private readonly FakeHttpMessageHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>OpenRouter: geçiş kimliği, usage.include, web eklentisi, maliyet usage.cost'tan.</summary>
    [Fact]
    public async Task OpenRouter_uses_passthrough_id_and_reported_cost()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("Merhaba", prompt: 5000, completion: 1000, cost: 0.0421m));
        var model = new OpenRouterTextModel(
            TestFixtures.Model("openrouter:anthropic/claude-sonnet-5.5"),
            TestFixtures.Credential(ProviderPlatforms.OpenRouter),
            TestFixtures.Dependencies(_recorder),
            _http.CreateClient());

        var result = await model.RunTextAsync(TestFixtures.Request() with { UseWebSearch = true, Effort = EffortLevel.Medium }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.CostUsd.ShouldBe(0.0421m);
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.ToString().ShouldBe("https://openrouter.ai/api/v1/chat/completions");
        var body = request.Json;
        body["model"]!.GetValue<string>().ShouldBe("anthropic/claude-sonnet-5.5");
        body["usage"]!["include"]!.GetValue<bool>().ShouldBeTrue();
        body["plugins"]![0]!["id"]!.GetValue<string>().ShouldBe("web");
        body["reasoning"]!["effort"]!.GetValue<string>().ShouldBe("medium");
        body["max_tokens"]!.GetValue<int>().ShouldBe(2000);
    }

    /// <summary>GLM: json_object + sistemde şema, efor açıkça, web_search aracı.</summary>
    [Fact]
    public async Task Glm_uses_json_object_and_explicit_effort()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("{\"title\":\"T\",\"sceneCount\":1,\"tags\":[],\"note\":null}"));
        var model = new GlmTextModel(TestFixtures.Model("glm-5.3"), TestFixtures.Credential(ProviderPlatforms.Zai), TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request() with { UseWebSearch = true }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.ToString().ShouldBe("https://api.z.ai/api/paas/v4/chat/completions");
        var body = request.Json;
        body["response_format"]!["type"]!.GetValue<string>().ShouldBe("json_object");
        body["reasoning_effort"]!.GetValue<string>().ShouldBe("high");
        body["tools"]![0]!["type"]!.GetValue<string>().ShouldBe("web_search");
        body["messages"]![0]!["content"]!.GetValue<string>().ShouldContain("JSON şeması");
    }

    /// <summary>Kimi: efor gönderilmez, json_object.</summary>
    [Fact]
    public async Task Kimi_has_no_effort()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("{\"title\":\"T\",\"sceneCount\":1,\"tags\":[],\"note\":null}"));
        var model = new KimiTextModel(TestFixtures.Model("kimi-k3"), TestFixtures.Credential(ProviderPlatforms.Moonshot), TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request() with { Effort = EffortLevel.Max }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        var body = _http.Requests.ShouldHaveSingleItem().Json;
        _http.Requests[0].Uri.ToString().ShouldBe("https://api.moonshot.ai/v1/chat/completions");
        body.ContainsKey("reasoning_effort").ShouldBeFalse();
        body["response_format"]!["type"]!.GetValue<string>().ShouldBe("json_object");
    }
}
