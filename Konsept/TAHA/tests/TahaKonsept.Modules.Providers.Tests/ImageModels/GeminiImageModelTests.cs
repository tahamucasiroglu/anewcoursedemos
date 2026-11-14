using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Media;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.ImageModels;

/// <summary>
/// Gemini görsel: generateContent istek biçimi (x-goog-api-key, IMAGE modalitesi, 9:16 oran, tohum), inlineData çözümü,
/// adet kadar tekrar, görsel başı maliyet, güvenlik engeli.
/// </summary>
public sealed class GeminiImageModelTests
{
    private const string Reply = """
        {"candidates":[{"content":{"parts":[{"text":"işte"},{"inlineData":{"mimeType":"image/png","data":"DATA"}}]},"finishReason":"STOP"}],
         "usageMetadata":{"promptTokenCount":12,"candidatesTokenCount":1290}}
        """;

    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>İki görsel → iki çağrı, istek biçimi ve maliyet.</summary>
    [Fact]
    public async Task Generates_images_with_expected_request()
    {
        var reply = Reply.Replace("DATA", MediaTestData.PngBase64, StringComparison.Ordinal);
        _http.Json(reply).Json(reply);
        var model = Create("gemini-nano-banana-2.1");

        var result = await model.GenerateAsync(MediaTestData.Image(count: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.Images.Count.ShouldBe(2);
        result.Value.Images[0].ShouldBe(MediaTestData.Png);
        result.Value.MimeType.ShouldBe("image/png");
        result.Value.CostUsd.ShouldBe(0.0672m);
        _http.Requests.Count.ShouldBe(2);
        var request = _http.Requests[0];
        request.Uri.ToString().ShouldBe("https://generativelanguage.googleapis.com/v1beta/models/gemini-nano-banana-2.1:generateContent");
        request.Headers["x-goog-api-key"].ShouldBe("sk-test-1234567890");
        var config = request.Json["generationConfig"]!.AsObject();
        config["responseModalities"]![0]!.GetValue<string>().ShouldBe("IMAGE");
        config["imageConfig"]!["aspectRatio"]!.GetValue<string>().ShouldBe("9:16");
        config["seed"]!.GetValue<int>().ShouldBe(42);
        request.Json["contents"]![0]!["parts"]![0]!["text"]!.GetValue<string>().ShouldContain("İstanbul");
        _recorder.Records.Count.ShouldBe(1);
        _recorder.Records[0].Usage.Images.ShouldBe(2);
    }

    /// <summary>Güvenlik engeli → providers.refused (yeniden denenmez).</summary>
    [Fact]
    public async Task Safety_block_is_refused()
    {
        _http.Json("""{"promptFeedback":{"blockReason":"SAFETY"}}""");

        var result = await Create("gemini-3-pro-image").GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(ProviderErrorCodes.Refused);
        _http.Requests.Count.ShouldBe(1);
    }

    /// <summary>Görsel parçası olmayan yanıt → boş yanıt; 401 → anahtar reddedildi.</summary>
    [Fact]
    public async Task Empty_and_unauthorized()
    {
        _http.Json("""{"candidates":[{"content":{"parts":[{"text":"görsel yok"}]},"finishReason":"STOP"}]}""")
            .Json("""{"error":{"code":401,"message":"API key not valid"}}""", System.Net.HttpStatusCode.Unauthorized);
        var model = Create("gemini-3.1-flash-lite-image");
        var ct = TestContext.Current.CancellationToken;

        (await model.GenerateAsync(MediaTestData.Image(), ct)).Error.Code.ShouldBe(ProviderErrorCodes.EmptyResponse);
        var unauthorized = await model.GenerateAsync(MediaTestData.Image(), ct);
        unauthorized.Error.Code.ShouldBe(ProviderErrorCodes.Unauthorized);
        unauthorized.Error.Message.ShouldContain("API key not valid");
    }

    /// <summary>Model kurar.</summary>
    /// <param name="key">Katalog anahtarı.</param>
    private GeminiImageModel Create(string key) =>
        new(TestFixtures.Model(key), TestFixtures.Credential(ProviderPlatforms.Google), TestFixtures.Dependencies(_recorder), _http.CreateClient(), MediaTestData.NoWait);
}
