using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Media;

/// <summary>
/// MediaGateway uçtan uca (gerçek fabrika + sahte HTTP): katalog → anahtar → model → CallLog; varsayılan ses; doğrulama hataları.
/// </summary>
public sealed class MediaGatewayTests
{
    private readonly IApiKeyVault _vault = Substitute.For<IApiKeyVault>();
    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();
    private readonly MediaGateway _gateway;

    /// <summary>Kurucu.</summary>
    public MediaGatewayTests()
    {
        _vault.GetCredentialAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(c => Result.Success(new ProviderCredential(c.ArgAt<string>(1), "k-12345678", "westeurope")));
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(_ => _http.CreateClient());
        var factory = new MediaModelFactory(httpFactory, _recorder, NullLoggerFactory.Instance);
        _gateway = new MediaGateway(
            TestFixtures.Catalog, _vault, factory, new StaticVoiceCatalog(TestFixtures.Catalog), new FixedClock(), NullLogger<MediaGateway>.Instance);
    }

    /// <summary>Görsel: fal üzerinden üretilir, CallLog'a kullanıcı ve amaçla yazılır.</summary>
    [Fact]
    public async Task Image_flow_records_call()
    {
        _http.Json($$$"""{"images":[{"url":"data:image/png;base64,{{{MediaTestData.PngBase64}}}"}]}""");
        var request = MediaTestData.Image();

        var result = await _gateway.GenerateImageAsync("fal-flux-2-flash", request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.ModelKey.ShouldBe("fal-flux-2-flash");
        result.Value.CostUsd.ShouldBe(0.01m);
        _recorder.Records.Single().Context.ShouldBe(request.Context);
        await _vault.Received(1).GetCredentialAsync(request.Context.UserId, ProviderPlatforms.Fal, Arg.Any<CancellationToken>());
    }

    /// <summary>Ses: ses verilmezse katalog varsayılanı (Azure → tr-TR-AhmetNeural) kullanılır; bölge kasadan gelir.</summary>
    [Fact]
    public async Task Speech_uses_default_voice()
    {
        _http.Bytes(MediaTestData.Mp3(frames: 10), "audio/mpeg");

        var result = await _gateway.SynthesizeSpeechAsync("azure-neural", MediaTestData.Speech(voice: null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.ModelKey.ShouldBe("azure-neural");
        _http.Requests[0].Uri.Host.ShouldBe("westeurope.tts.speech.microsoft.com");
        _http.Requests[0].Body.ShouldContain("tr-TR-AhmetNeural");
    }

    /// <summary>Bilinmeyen model, yanlış yetenek, yerel model, boş kullanıcı ve eksik anahtar.</summary>
    [Fact]
    public async Task Validation_errors()
    {
        var ct = TestContext.Current.CancellationToken;
        (await _gateway.GenerateImageAsync("yok", MediaTestData.Image(), ct)).Error.Code.ShouldBe(ProviderErrorCodes.ModelNotFound);
        (await _gateway.GenerateImageAsync("azure-neural", MediaTestData.Image(), ct)).Error.Code.ShouldBe(ProviderErrorCodes.ModelNotSupported);
        (await _gateway.SynthesizeSpeechAsync("supertonic-3", MediaTestData.Speech(), ct)).Error.Code.ShouldBe(ProviderErrorCodes.ModelNotSupported);
        (await _gateway.GenerateImageAsync("cogview-4", MediaTestData.Image() with { Context = new CallContext(Guid.Empty, "image") }, ct))
            .Error.Code.ShouldBe(ProviderErrorCodes.InvalidRequest);

        _vault.GetCredentialAsync(Arg.Any<Guid>(), ProviderPlatforms.ElevenLabs, Arg.Any<CancellationToken>())
            .Returns(Error.Validation(ProviderErrorCodes.KeyMissing, "yok"));
        (await _gateway.SynthesizeSpeechAsync("elevenlabs-v4", MediaTestData.Speech(), ct)).Error.Code.ShouldBe(ProviderErrorCodes.KeyMissing);
        _http.Requests.ShouldBeEmpty();
    }

    /// <summary>Katalogdaki her online görsel/ses modeli için fabrika somut bir model üretir.</summary>
    [Fact]
    public void Factory_covers_every_online_media_model()
    {
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(_ => _http.CreateClient());
        var factory = new MediaModelFactory(httpFactory, _recorder, NullLoggerFactory.Instance);
        var online = TestFixtures.Catalog.All.Where(m => m.RunsOn == ModelRunsOn.Server).ToList();
        foreach (var model in online.Where(m => m.Capability == ModelCapability.Image))
        {
            factory.CreateImage(model, new ProviderCredential(model.Platform, "k", null)).IsSuccess.ShouldBeTrue(model.Key);
        }

        foreach (var model in online.Where(m => m.Capability == ModelCapability.Speech))
        {
            factory.CreateSpeech(model, new ProviderCredential(model.Platform, "k", null)).IsSuccess.ShouldBeTrue(model.Key);
        }

        var image = TestFixtures.Model("cogview-4");
        factory.CreateImage(image, new ProviderCredential(ProviderPlatforms.OpenAi, "k", null)).Error.Code.ShouldBe(ProviderErrorCodes.InvalidRequest);
        factory.CreateSpeech(image, new ProviderCredential(image.Platform, "k", null)).Error.Code.ShouldBe(ProviderErrorCodes.ModelNotSupported);
    }
}
