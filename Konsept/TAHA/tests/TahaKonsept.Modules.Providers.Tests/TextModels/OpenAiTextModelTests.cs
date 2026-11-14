using System.Net;
using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.TextModels;

/// <summary>
/// OpenAI (Chat Completions) istek biçimi, usage → maliyet, JSON onarımı, akış ve hata eşlemesi.
/// </summary>
public sealed class OpenAiTextModelTests
{
    private readonly FakeHttpMessageHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>Yapılandırılmış istek: json_schema strict, max_completion_tokens, reasoning_effort, Bearer.</summary>
    [Fact]
    public async Task Structured_request_shape_and_usage_cost()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("{\"title\":\"Bizans\",\"sceneCount\":6,\"tags\":[],\"note\":null}", prompt: 1000, completion: 500, cached: 200));
        var model = Create("gpt-6.1-sol");

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request() with { Effort = EffortLevel.High }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Value.Title.ShouldBe("Bizans");
        result.Value.Repaired.ShouldBeFalse();
        result.Value.Usage.InputTokens.ShouldBe(800);
        result.Value.Usage.CacheReadTokens.ShouldBe(200);
        result.Value.CostUsd.ShouldBe(0.00664m);

        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.ToString().ShouldBe("https://api.openai.com/v1/chat/completions");
        request.Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
        var body = request.Json;
        body["model"]!.GetValue<string>().ShouldBe("gpt-6.1-sol");
        body["max_completion_tokens"]!.GetValue<int>().ShouldBe(2000);
        body["reasoning_effort"]!.GetValue<string>().ShouldBe("high");
        body["messages"]!.AsArray().Count.ShouldBe(2);
        body["messages"]![0]!["role"]!.GetValue<string>().ShouldBe("system");
        var format = body["response_format"]!;
        format["type"]!.GetValue<string>().ShouldBe("json_schema");
        format["json_schema"]!["strict"]!.GetValue<bool>().ShouldBeTrue();
        format["json_schema"]!["schema"]!["additionalProperties"]!.GetValue<bool>().ShouldBeFalse();
        body.ContainsKey("stream").ShouldBeFalse();

        var record = _recorder.Records.ShouldHaveSingleItem();
        record.Error.ShouldBeNull();
        record.Model.Key.ShouldBe("gpt-6.1-sol");
        record.Context.Purpose.ShouldBe("concept");
    }

    /// <summary>Geçersiz JSON → tek onarım çağrısı; kullanım ve maliyet toplanır.</summary>
    [Fact]
    public async Task Invalid_json_is_repaired_once()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("Tabii! {\"title\": \"Bizans\", \"sceneCount\": ", prompt: 1000, completion: 100))
            .EnqueueJson(OpenAiResponses.Completion("{\"title\":\"Bizans\",\"sceneCount\":4,\"tags\":[\"tarih\"],\"note\":null}", prompt: 300, completion: 80));
        var model = Create("gpt-6-luna");

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Repaired.ShouldBeTrue();
        result.Value.Value.SceneCount.ShouldBe(4);
        result.Value.Usage.InputTokens.ShouldBe(1300);
        result.Value.Usage.OutputTokens.ShouldBe(180);
        _http.Requests.Count.ShouldBe(2);
        _http.Requests[1].Json["messages"]![1]!["content"]!.GetValue<string>().ShouldContain("Hatalı yanıt");
        _recorder.Records.Count.ShouldBe(2);
        _recorder.Records[1].Context.Purpose.ShouldBe("concept:json-repair");
    }

    /// <summary>Onarım da başarısızsa invalid_json.</summary>
    [Fact]
    public async Task Fails_when_repair_also_invalid()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("bozuk")).EnqueueJson(OpenAiResponses.Completion("hala bozuk"));
        var result = await Create("gpt-6-luna").RunAsync<SampleConcept>(TestFixtures.Request(), TestContext.Current.CancellationToken);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ProviderErrorCodes.InvalidJson);
    }

    /// <summary>Kesilmiş JSON onarılmaz, truncated hatası döner.</summary>
    [Fact]
    public async Task Truncated_json_is_not_repaired()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("{\"title\": \"Biz", finish: "length"));
        var result = await Create("gpt-6-luna").RunAsync<SampleConcept>(TestFixtures.Request(), TestContext.Current.CancellationToken);
        result.Error.Code.ShouldBe(ProviderErrorCodes.Truncated);
        _http.Requests.Count.ShouldBe(1);
    }

    /// <summary>401 → anahtar reddedildi, yeniden denenmez ve CallLog'a başarısız yazılır.</summary>
    [Fact]
    public async Task Unauthorized_is_not_retried()
    {
        _http.EnqueueJson("{\"error\":{\"message\":\"Incorrect API key\"}}", HttpStatusCode.Unauthorized);
        var result = await Create("gpt-6-luna").RunTextAsync(TestFixtures.Request(), TestContext.Current.CancellationToken);
        result.Error.Code.ShouldBe(ProviderErrorCodes.Unauthorized);
        result.Error.Message.ShouldContain("Incorrect API key");
        _recorder.Records.ShouldHaveSingleItem().Error.ShouldNotBeNull();
    }

    /// <summary>503 geçici hata yeniden denenir.</summary>
    [Fact]
    public async Task Transient_error_is_retried()
    {
        _http.EnqueueJson("{\"error\":{\"message\":\"overloaded\"}}", HttpStatusCode.ServiceUnavailable)
            .EnqueueJson(OpenAiResponses.Completion("Merhaba"));
        var result = await Create("gpt-6-luna").RunTextAsync(TestFixtures.Request(), TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("Merhaba");
        _http.Requests.Count.ShouldBe(2);
        _recorder.Records.Count.ShouldBe(2);
    }

    /// <summary>Akış: SSE parçaları birleştirilir, son parçadaki usage okunur.</summary>
    [Fact]
    public async Task Streaming_assembles_text_and_usage()
    {
        _http.EnqueueSse(
            "data: {\"model\":\"gpt-6.1-sol\",\"choices\":[{\"delta\":{\"content\":\"<div>\"}}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{\"content\":\"Sahne</div>\"},\"finish_reason\":\"stop\"}]}\n\n" +
            "data: {\"choices\":[],\"usage\":{\"prompt_tokens\":3000,\"completion_tokens\":40000}}\n\n" +
            "data: [DONE]\n\n");
        var result = await Create("gpt-6.1-sol").RunTextAsync(TestFixtures.Request(40000) with { Stream = true }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Value.ShouldBe("<div>Sahne</div>");
        result.Value.Usage.OutputTokens.ShouldBe(40000);
        var body = _http.Requests.ShouldHaveSingleItem().Json;
        body["stream"]!.GetValue<bool>().ShouldBeTrue();
        body["stream_options"]!["include_usage"]!.GetValue<bool>().ShouldBeTrue();
    }

    /// <summary>Akış ortasında kopan bağlantı (IOException) ağ hatası olarak kayda geçer ve yeniden denenir (500 server.unhandled olmaz).</summary>
    [Fact]
    public async Task Dropped_stream_is_recorded_as_network_error_and_retried()
    {
        _http.EnqueueBrokenSse("data: {\"choices\":[{\"delta\":{\"content\":\"<div>\"}}]}\n\n")
            .EnqueueSse("data: {\"choices\":[{\"delta\":{\"content\":\"Tamam\"},\"finish_reason\":\"stop\"}]}\n\ndata: [DONE]\n\n");

        var result = await Create("gpt-6.1-sol").RunTextAsync(TestFixtures.Request(40000) with { Stream = true }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Value.ShouldBe("Tamam");
        _recorder.Records.Count.ShouldBe(2);
        _recorder.Records[0].Error!.Code.ShouldBe(ProviderErrorCodes.Network);
    }

    /// <summary>Modeli oluşturur.</summary>
    /// <param name="key">Katalog anahtarı.</param>
    private OpenAiTextModel Create(string key) =>
        new(TestFixtures.Model(key), TestFixtures.Credential(ProviderPlatforms.OpenAi), TestFixtures.Dependencies(_recorder), _http.CreateClient());
}
