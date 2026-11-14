using Shouldly;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Media;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.ImageModels;

/// <summary>
/// OpenAI gpt-image: images/generations istek biçimi (Bearer, n, en yakın boyut), b64_json çözümü, token / görsel başı maliyet.
/// </summary>
public sealed class OpenAiImageModelTests
{
    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>gpt-image-2: token fiyatı var ve usage geldi → token bazlı maliyet (görsel başı eklenmez).</summary>
    [Fact]
    public async Task Gpt_image_2_uses_token_usage()
    {
        _http.Json($$$"""
            {"created":1,"data":[{"b64_json":"{{{MediaTestData.PngBase64}}}"},{"b64_json":"{{{MediaTestData.PngBase64}}}"}],
             "usage":{"input_tokens":100000,"output_tokens":100000,"total_tokens":200000}}
            """);

        var result = await Create("gpt-image-2").GenerateAsync(MediaTestData.Image(count: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.Images.Count.ShouldBe(2);
        result.Value.CostUsd.ShouldBe(3.5m); // 0.1M × $5 + 0.1M × $30
        var request = _http.Requests[0];
        request.Uri.ToString().ShouldBe("https://api.openai.com/v1/images/generations");
        request.Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
        request.Json["model"]!.GetValue<string>().ShouldBe("gpt-image-2");
        request.Json["n"]!.GetValue<int>().ShouldBe(2);
        request.Json["size"]!.GetValue<string>().ShouldBe("1024x1536");
        _recorder.Records[0].Usage.Images.ShouldBe(0);
    }

    /// <summary>gpt-image-1-mini: usage yok → görsel sayısı yazılır (fiyat tanımsız → $0).</summary>
    [Fact]
    public async Task Without_usage_counts_images()
    {
        _http.Json($$$"""{"data":[{"b64_json":"{{{MediaTestData.PngBase64}}}"}]}""");

        var result = await Create("gpt-image-1-mini").GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _recorder.Records[0].Usage.Images.ShouldBe(1);
        result.Value.CostUsd.ShouldBe(0m);
    }

    /// <summary>Model kurar.</summary>
    /// <param name="key">Katalog anahtarı.</param>
    private OpenAiImageModel Create(string key) =>
        new(TestFixtures.Model(key), TestFixtures.Credential(ProviderPlatforms.OpenAi), TestFixtures.Dependencies(_recorder), _http.CreateClient(), MediaTestData.NoWait);
}
