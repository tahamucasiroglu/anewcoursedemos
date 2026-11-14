using Shouldly;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.TextModels;

/// <summary>
/// Anthropic (resmi SDK, sahte HttpMessageHandler): istek biçimi (system cache_control, output_config.effort/format, web_search),
/// usage → maliyet, akış.
/// </summary>
public sealed class AnthropicTextModelTests
{
    private readonly FakeHttpMessageHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>Yapılandırılmış istek biçimi ve maliyet.</summary>
    [Fact]
    public async Task Structured_request_uses_cache_control_effort_and_json_schema()
    {
        _http.EnqueueJson(Message("{\"title\":\"Bizans\",\"sceneCount\":7,\"tags\":[\"tarih\"],\"note\":null}", input: 100, output: 50, cacheRead: 1000));
        var model = Create("claude-sonnet-5-5");

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request() with { Effort = EffortLevel.High }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Value.SceneCount.ShouldBe(7);
        result.Value.Usage.CacheReadTokens.ShouldBe(1000);
        result.Value.CostUsd.ShouldBe(0.0008m);

        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.AbsolutePath.ShouldBe("/v1/messages");
        request.Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
        request.Headers.ShouldContainKey("anthropic-version");
        var body = request.Json;
        body["model"]!.GetValue<string>().ShouldBe("claude-sonnet-5-5");
        body["max_tokens"]!.GetValue<int>().ShouldBe(2000);
        body["system"]![0]!["cache_control"]!["type"]!.GetValue<string>().ShouldBe("ephemeral");
        body["output_config"]!["effort"]!.GetValue<string>().ShouldBe("high");
        body["output_config"]!["format"]!["type"]!.GetValue<string>().ShouldBe("json_schema");
        body["output_config"]!["format"]!["schema"]!["additionalProperties"]!.GetValue<bool>().ShouldBeFalse();
        body.ContainsKey("tools").ShouldBeFalse();
        body.ContainsKey("thinking").ShouldBeFalse();
    }

    /// <summary>Web araması: sunucu aracı eklenir, şema sistem talimatına taşınır (atıflar format ile birlikte kullanılamaz).</summary>
    [Fact]
    public async Task Web_search_adds_server_tool_and_moves_schema_to_system()
    {
        _http.EnqueueJson(Message("Kaynaklara göre: {\"title\":\"Malazgirt\",\"sceneCount\":3,\"tags\":[],\"note\":null}", input: 2000, output: 300, webSearches: 2));
        var model = Create("claude-haiku-5-5");

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request() with { UseWebSearch = true, CacheSystemPrompt = false }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Value.Title.ShouldBe("Malazgirt");
        result.Value.Usage.WebSearchRequests.ShouldBe(2);
        var body = _http.Requests.ShouldHaveSingleItem().Json;
        body["tools"]![0]!["type"]!.GetValue<string>().ShouldBe("web_search_20260209");
        body["tools"]![0]!["max_uses"]!.GetValue<int>().ShouldBe(5);
        body["system"]![0]!["text"]!.GetValue<string>().ShouldContain("JSON şeması");
        body["system"]![0]!.AsObject().ContainsKey("cache_control").ShouldBeFalse();
        body["output_config"]!.AsObject().ContainsKey("format").ShouldBeFalse();
        body["output_config"]!["effort"]!.GetValue<string>().ShouldBe("low");
    }

    /// <summary>Akış (video kodu): metin parçaları ve message_delta usage birleştirilir.</summary>
    [Fact]
    public async Task Streaming_assembles_text_and_usage()
    {
        _http.EnqueueSse(
            "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_1\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-opus-5-5\",\"content\":[],\"stop_reason\":null,\"stop_sequence\":null,\"usage\":{\"input_tokens\":500,\"output_tokens\":1,\"cache_read_input_tokens\":2000,\"cache_creation_input_tokens\":0}}}\n\n" +
            "event: content_block_start\ndata: {\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}\n\n" +
            "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"<div>\"}}\n\n" +
            "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"Sahne</div>\"}}\n\n" +
            "event: content_block_stop\ndata: {\"type\":\"content_block_stop\",\"index\":0}\n\n" +
            "event: message_delta\ndata: {\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"end_turn\",\"stop_sequence\":null},\"usage\":{\"output_tokens\":30000}}\n\n" +
            "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n");
        var model = Create("claude-opus-5-5");

        var result = await model.RunTextAsync(TestFixtures.Request(64000) with { Stream = true, Effort = EffortLevel.High }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Value.ShouldBe("<div>Sahne</div>");
        result.Value.Usage.InputTokens.ShouldBe(500);
        result.Value.Usage.OutputTokens.ShouldBe(30000);
        result.Value.Usage.CacheReadTokens.ShouldBe(2000);
        // 500*4 + 30000*20 + 2000*0.2 (per 1M) = 0.002 + 0.6 + 0.0004
        result.Value.CostUsd.ShouldBe(0.6024m);
        var body = _http.Requests.ShouldHaveSingleItem().Json;
        body["stream"]!.GetValue<bool>().ShouldBeTrue();
        body["max_tokens"]!.GetValue<int>().ShouldBe(64000);
    }

    /// <summary>Anthropic mesaj yanıtı.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="input">Giriş token'ı.</param>
    /// <param name="output">Çıkış token'ı.</param>
    /// <param name="cacheRead">Önbellek okuma.</param>
    /// <param name="webSearches">Web araması sayısı.</param>
    private static string Message(string text, int input, int output, int cacheRead = 0, int webSearches = 0)
    {
        var escaped = System.Text.Json.JsonSerializer.Serialize(text);
        var serverTool = webSearches > 0 ? ",\"server_tool_use\":{\"web_search_requests\":" + webSearches + ",\"web_fetch_requests\":0}" : string.Empty;
        return "{\"id\":\"msg_1\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-sonnet-5-5\",\"content\":[{\"type\":\"text\",\"text\":" + escaped +
            "}],\"stop_reason\":\"end_turn\",\"stop_sequence\":null,\"usage\":{\"input_tokens\":" + input + ",\"output_tokens\":" + output +
            ",\"cache_read_input_tokens\":" + cacheRead + ",\"cache_creation_input_tokens\":0" + serverTool + "}}";
    }

    /// <summary>Modeli oluşturur.</summary>
    /// <param name="key">Katalog anahtarı.</param>
    private AnthropicTextModel Create(string key) =>
        new(TestFixtures.Model(key), TestFixtures.Credential(ProviderPlatforms.Anthropic), TestFixtures.Dependencies(_recorder), _http.CreateClient());
}
