using Shouldly;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Media;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.ImageModels;

/// <summary>
/// fal (eşzamanlı uç, Key yetkisi, data URI çözümü, 16'nın katı boyut) ve Z.ai (Bearer, GxY boyut, URL indirme) görsel modelleri.
/// </summary>
public sealed class FalAndZaiImageModelTests
{
    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>fal: sync_mode data URI + ikinci görsel URL'den indirilir.</summary>
    [Fact]
    public async Task Fal_sync_endpoint()
    {
        _http.Json($$$"""
                {"images":[{"url":"data:image/png;base64,{{{MediaTestData.PngBase64}}}","width":1088,"height":1920},
                           {"url":"https://v3.fal.media/files/x.png"}],"seed":42}
                """)
            .Bytes(MediaTestData.Png, "image/png");
        var model = new FalImageModel(
            TestFixtures.Model("fal-z-image-turbo"), TestFixtures.Credential(ProviderPlatforms.Fal), TestFixtures.Dependencies(_recorder), _http.CreateClient(), MediaTestData.NoWait);

        var result = await model.GenerateAsync(MediaTestData.Image(count: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.Images.Count.ShouldBe(2);
        result.Value.Images.ShouldAllBe(i => i.SequenceEqual(MediaTestData.Png));
        result.Value.CostUsd.ShouldBe(0.02m);
        var request = _http.Requests[0];
        request.Uri.ToString().ShouldBe("https://fal.run/fal-ai/z-image/turbo");
        request.Headers["Authorization"].ShouldBe("Key sk-test-1234567890");
        request.Json["image_size"]!["width"]!.GetValue<int>().ShouldBe(1088);
        request.Json["image_size"]!["height"]!.GetValue<int>().ShouldBe(1920);
        request.Json["num_images"]!.GetValue<int>().ShouldBe(2);
        request.Json["sync_mode"]!.GetValue<bool>().ShouldBeTrue();
        _http.Requests[1].Headers.ContainsKey("Authorization").ShouldBeFalse();
    }

    /// <summary>Z.ai CogView-4: istek biçimi, URL indirme, görsel başı maliyet.</summary>
    [Fact]
    public async Task Zai_generations()
    {
        _http.Json("""{"created":1,"data":[{"url":"https://cdn.z.ai/img/1.png"}]}""").Bytes(MediaTestData.Png, "image/png");
        var model = new ZaiImageModel(
            TestFixtures.Model("cogview-4"), TestFixtures.Credential(ProviderPlatforms.Zai), TestFixtures.Dependencies(_recorder), _http.CreateClient(), MediaTestData.NoWait);

        var result = await model.GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.Images.Single().ShouldBe(MediaTestData.Png);
        result.Value.CostUsd.ShouldBe(0.01m);
        var request = _http.Requests[0];
        request.Uri.ToString().ShouldBe("https://api.z.ai/api/paas/v4/images/generations");
        request.Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
        request.Json["model"]!.GetValue<string>().ShouldBe("cogview-4");
        request.Json["size"]!.GetValue<string>().ShouldBe("1088x1920");
        _http.Requests[1].Uri.ToString().ShouldBe("https://cdn.z.ai/img/1.png");
    }
}
