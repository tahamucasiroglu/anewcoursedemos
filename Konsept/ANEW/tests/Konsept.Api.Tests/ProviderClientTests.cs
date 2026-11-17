using System.Net;
using System.Text;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Enums;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Infrastructure;
using Konsept.Modules.AiGateway.Infrastructure.Providers.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek sağlayıcı istemcisinin HTTP eşlemesi (spec 0013 AC-4, AC-5, AC-9; plan 0013 Risk 2), sahte HTTP işleyicisiyle — gerçek
/// çağrı yok: her sağlayıcıya doğru uç, doğru kimlik başlığı, model adı, çıktı sınırı ve efor alanı; yanıttan metin, token, "yarım
/// kaldı" ve bildirilen maliyet; hata ve bağlantı durumları; istemcide tekrar ya da yönlendirme yok, zaman aşımı kapıya bırakılır.
/// </summary>
public sealed class ProviderClientTests
{
    private static readonly CatalogSeed Seed = GatewayHarness.Seed();

    /// <summary>Her sağlayıcı biçimi: uç, başlık, gövde alanları ve yanıt ayrıştırma (token, yarım kaldı, bildirilen maliyet).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProviderClient_Dialects_RequestAndUsage()
    {
        var ct = TestContext.Current.CancellationToken;

        var anthropic = await SendAsync(
            "anthropic/claude-opus-5-5",
            """{"content":[{"type":"text","text":"Mer"},{"type":"text","text":"haba"}],"stop_reason":"max_tokens","usage":{"input_tokens":12,"output_tokens":34}}""",
            effort: "high",
            ct: ct);
        anthropic.Request.RequestUri.ShouldBe(new Uri("https://api.anthropic.com/v1/messages"));
        anthropic.Request.Headers.GetValues("x-api-key").ShouldBe(["anahtar-123456"]);
        anthropic.Request.Headers.GetValues("anthropic-version").ShouldBe(["2023-06-01"]);
        anthropic.Request.Headers.Authorization.ShouldBeNull();
        anthropic.Body.GetProperty("model").GetString().ShouldBe("claude-opus-5-5");
        anthropic.Body.GetProperty("max_tokens").GetInt32().ShouldBe(777);
        anthropic.Body.GetProperty("system").GetString().ShouldBe("sistem");
        anthropic.Body.GetProperty("output_config").GetProperty("effort").GetString().ShouldBe("high");
        anthropic.Body.GetProperty("messages")[0].GetProperty("content").GetString().ShouldBe("istem");
        anthropic.Result.ShouldBe(ProviderAttempt.Completed("Merhaba", 12, 34, truncated: true));

        var sonnet = await SendAsync("anthropic/claude-sonnet-5-5", """{"content":[],"stop_reason":"end_turn","usage":{"input_tokens":1,"output_tokens":2}}""", ct: ct);
        sonnet.Body.TryGetProperty("output_config", out _).ShouldBeFalse();
        sonnet.Result.Truncated.ShouldBeFalse();

        var openai = await SendAsync(
            "openai/gpt-6.1-sol",
            """{"choices":[{"message":{"content":"Selam"},"finish_reason":"length"}],"usage":{"prompt_tokens":5,"completion_tokens":6}}""",
            effort: "low",
            ct: ct);
        openai.Request.RequestUri.ShouldBe(new Uri("https://api.openai.com/v1/chat/completions"));
        openai.Request.Headers.Authorization!.ToString().ShouldBe("Bearer anahtar-123456");
        openai.Body.GetProperty("model").GetString().ShouldBe("gpt-6.1-sol");
        openai.Body.GetProperty("max_completion_tokens").GetInt32().ShouldBe(777);
        openai.Body.TryGetProperty("max_tokens", out _).ShouldBeFalse();
        openai.Body.GetProperty("reasoning_effort").GetString().ShouldBe("low");
        openai.Body.GetProperty("messages")[0].GetProperty("role").GetString().ShouldBe("system");
        openai.Result.ShouldBe(ProviderAttempt.Completed("Selam", 5, 6, truncated: true));

        var router = await SendAsync(
            "openrouter/anthropic/claude-sonnet-5.5",
            """{"choices":[{"message":{"content":"x"},"finish_reason":"stop"}],"usage":{"prompt_tokens":5,"completion_tokens":6,"cost":0.000123456}}""",
            ct: ct);
        router.Request.RequestUri.ShouldBe(new Uri("https://openrouter.ai/api/v1/chat/completions"));
        router.Body.GetProperty("model").GetString().ShouldBe("anthropic/claude-sonnet-5.5");
        router.Body.GetProperty("max_tokens").GetInt32().ShouldBe(777);
        router.Body.GetProperty("usage").GetProperty("include").GetBoolean().ShouldBeTrue();
        router.Result.ReportedCostUsd.ShouldBe(0.000123456m);
        router.Result.Truncated.ShouldBeFalse();

        var glm = await SendAsync("glm/glm-5.3", """{"choices":[{"message":{"content":"x"},"finish_reason":"stop"}]}""", ct: ct);
        glm.Request.RequestUri.ShouldBe(new Uri("https://api.z.ai/api/paas/v4/chat/completions"));
        glm.Body.GetProperty("model").GetString().ShouldBe("glm-5.3");
        glm.Body.GetProperty("max_tokens").GetInt32().ShouldBe(777);
        glm.Result.InputTokens.ShouldBeNull();
        glm.Result.ReportedCostUsd.ShouldBeNull();

        var kimi = await SendAsync("kimi/kimi-k3.1-pro", """{"choices":[{"message":{"content":"x"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"cost":9}}""", ct: ct);
        kimi.Request.RequestUri.ShouldBe(new Uri("https://api.moonshot.ai/v1/chat/completions"));
        kimi.Body.GetProperty("model").GetString().ShouldBe("kimi-k3.1-pro");
        kimi.Result.ReportedCostUsd.ShouldBeNull(); // Yalnızca OpenRouter'ın bildirdiği maliyet esas alınır.

        var image = await SendAsync("openai/gpt-image-2.6-flare", """{"data":[{"b64_json":"YQ=="},{"b64_json":"Yg=="}]}""", images: 2, ct: ct);
        image.Request.RequestUri.ShouldBe(new Uri("https://api.openai.com/v1/images/generations"));
        image.Body.GetProperty("model").GetString().ShouldBe("gpt-image-2.6-flare");
        image.Body.GetProperty("n").GetInt32().ShouldBe(2);
        image.Body.GetProperty("quality").GetString().ShouldBe("medium"); // review r1 L-1: katalog fiyatının kalitesi
        image.Body.GetProperty("size").GetString().ShouldBe("1024x1024");
        image.Result.Images.ShouldBe(["YQ==", "Yg=="]);
    }

    /// <summary>Hata kodu → <see cref="ProviderAttemptKind.HttpError"/>; kurulamayan bağlantı → ConnectionFailed; yanıt sırasında kopma → NoResponse.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProviderClient_ErrorsAndConnection_MappedSingleRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        foreach (var status in new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError, HttpStatusCode.PaymentRequired })
        {
            var handler = new StubProviderHandler(_ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("ham gövde") }));
            var result = await new HttpProviderClient(new StubHttpClientFactory(handler)).SendAsync(Call("anthropic/claude-haiku-5-5"), ct);
            result.ShouldBe(ProviderAttempt.Failed((int)status));
            handler.Requests.Count.ShouldBe(1);
        }

        var refused = new StubProviderHandler(_ => throw new HttpRequestException(HttpRequestError.ConnectionError, "refused"));
        (await new HttpProviderClient(new StubHttpClientFactory(refused)).SendAsync(Call("openai/gpt-6.1-sol"), ct)).Kind.ShouldBe(ProviderAttemptKind.ConnectionFailed);
        var dns = new StubProviderHandler(_ => throw new HttpRequestException(HttpRequestError.NameResolutionError, "dns"));
        (await new HttpProviderClient(new StubHttpClientFactory(dns)).SendAsync(Call("openai/gpt-6.1-sol"), ct)).Kind.ShouldBe(ProviderAttemptKind.ConnectionFailed);
        var ended = new StubProviderHandler(_ => throw new HttpRequestException(HttpRequestError.ResponseEnded, "ended"));
        (await new HttpProviderClient(new StubHttpClientFactory(ended)).SendAsync(Call("openai/gpt-6.1-sol"), ct)).Kind.ShouldBe(ProviderAttemptKind.NoResponse);
        var notJson = new StubProviderHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>") }));
        (await new HttpProviderClient(new StubHttpClientFactory(notJson)).SendAsync(Call("openai/gpt-6.1-sol"), ct)).ShouldBe(ProviderAttempt.Completed(null, null, null));
    }

    /// <summary>
    /// Modülün çağrı istemcisi: HttpClient zaman aşımı kapalı (katalogdaki süreyi kapı uygular; 100 sn'lik varsayılan 10 dk'lık
    /// çağrıyı kesmez), yönlendirme izlenmez, tekrar/dayanıklılık işleyicisi yok.
    /// </summary>
    [Fact]
    public void Module_CallClient_NoTimeoutNoRedirectNoRetry()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAiGatewayModule("Host=127.0.0.1;Port=1;Database=unused", new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpProviderClient.ClientName);
        client.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
        HttpMessageHandler? handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(HttpProviderClient.ClientName);
        var chain = new List<HttpMessageHandler>();
        while (handler is not null)
        {
            chain.Add(handler);
            handler = (handler as DelegatingHandler)?.InnerHandler;
        }

        chain.ShouldNotContain(h => h.GetType().Name.Contains("Retry", StringComparison.OrdinalIgnoreCase) || h.GetType().Name.Contains("Resilience", StringComparison.OrdinalIgnoreCase));
        chain[^1].ShouldBeOfType<SocketsHttpHandler>().AllowAutoRedirect.ShouldBeFalse();
    }

    private static ProviderCall Call(string modelId, string? effort = null, int images = 1)
    {
        var model = Seed.Models.Single(m => m.Id == modelId);
        var provider = Seed.Providers.Single(p => p.Id == model.ProviderId);
        return new ProviderCall(provider, model, "anahtar-123456", effort, model.OutputTokenLimit is null ? null : 777, "sistem", "istem", images);
    }

    private static async Task<(HttpRequestMessage Request, JsonElement Body, ProviderAttempt Result)> SendAsync(
        string modelId, string response, string? effort = null, int images = 1, CancellationToken ct = default)
    {
        var handler = new RecordingProviderHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") });

        var result = await new HttpProviderClient(new StubHttpClientFactory(handler)).SendAsync(Call(modelId, effort, images), ct);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        return (request, JsonDocument.Parse(handler.Bodies.Single()).RootElement.Clone(), result);
    }
}
