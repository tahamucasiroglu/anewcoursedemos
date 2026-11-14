using System.Net;
using Shouldly;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.KeyTesting.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.KeyTesting;

/// <summary>
/// Anahtar testi: platforma özgü uç ve başlık, HTTP durumunun Türkçe sonuca çevrilmesi.
/// </summary>
public sealed class HttpApiKeyTesterTests
{
    private readonly FakeHttpMessageHandler _http = new();

    /// <summary>ElevenLabs: xi-api-key başlığı, 200 → geçerli.</summary>
    [Fact]
    public async Task ElevenLabs_valid()
    {
        _http.EnqueueJson("{}");
        var result = await new HttpApiKeyTester(_http.CreateClient()).TestAsync(new ProviderCredential("elevenlabs", "el-key-123456", null), TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.ToString().ShouldBe("https://api.elevenlabs.io/v1/user");
        request.Headers["xi-api-key"].ShouldBe("el-key-123456");
    }

    /// <summary>OpenAI 401 → reddedildi.</summary>
    [Fact]
    public async Task OpenAi_rejected()
    {
        _http.EnqueueJson("{\"error\":{}}", HttpStatusCode.Unauthorized);
        var result = await new HttpApiKeyTester(_http.CreateClient()).TestAsync(new ProviderCredential("openai", "sk-bad-1234567", null), TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(401);
        result.Message.ShouldContain("reddedildi");
    }

    /// <summary>Azure: bölgeye göre issueToken ucu.</summary>
    [Fact]
    public async Task Azure_uses_region_endpoint()
    {
        _http.EnqueueJson("token");
        var result = await new HttpApiKeyTester(_http.CreateClient()).TestAsync(new ProviderCredential("azure-speech", "az-key-123456", "westeurope"), TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.ToString().ShouldBe("https://westeurope.api.cognitive.microsoft.com/sts/v1.0/issueToken");
    }

    /// <summary>Anthropic: SDK ile model listesi (SDK anahtarı Bearer olarak gönderir).</summary>
    [Fact]
    public async Task Anthropic_uses_sdk_models_list()
    {
        _http.EnqueueJson("{\"data\":[],\"has_more\":false,\"first_id\":null,\"last_id\":null}");
        var result = await new HttpApiKeyTester(_http.CreateClient()).TestAsync(new ProviderCredential("anthropic", "sk-ant-123456", null), TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue(result.Message);
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.AbsolutePath.ShouldBe("/v1/models");
        request.Headers["Authorization"].ShouldBe("Bearer sk-ant-123456");
    }
}
