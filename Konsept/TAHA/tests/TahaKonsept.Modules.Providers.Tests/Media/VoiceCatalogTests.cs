using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Media;

/// <summary>
/// Statik ses kataloğu: her online ses modelinin listesi var, Türkçeye özgü sesler önce, varsayılan ses, doğrulama hataları.
/// </summary>
public sealed class VoiceCatalogTests
{
    private readonly StaticVoiceCatalog _voices = new(TestFixtures.Catalog);

    /// <summary>Her online ses modeli için en az bir ses ve tam bir varsayılan.</summary>
    [Fact]
    public void Every_online_speech_model_has_voices()
    {
        foreach (var model in TestFixtures.Catalog.All.Where(m => m.Capability == ModelCapability.Speech && m.RunsOn == ModelRunsOn.Server))
        {
            var list = _voices.List(model.Key);
            list.IsSuccess.ShouldBeTrue(model.Key);
            list.Value.ShouldNotBeEmpty(model.Key);
            list.Value.Count(v => v.IsDefault).ShouldBe(1, model.Key);
            list.Value.Select(v => v.Id).ShouldBeUnique();
            _voices.DefaultVoiceId(model.Key).ShouldNotBeNullOrWhiteSpace();
        }
    }

    /// <summary>Azure: tr-TR sesleri önce.</summary>
    [Fact]
    public void Turkish_voices_first()
    {
        var list = _voices.List("azure-neural").Value;
        list[0].Id.ShouldBe("tr-TR-AhmetNeural");
        list[1].Id.ShouldBe("tr-TR-EmelNeural");
        list.Take(2).ShouldAllBe(v => v.NativeTurkish);
        _voices.DefaultVoiceId("gemini-3.8-flash-tts").ShouldBe("Kore");
    }

    /// <summary>Boş anahtar, bilinmeyen model, ses olmayan model.</summary>
    [Fact]
    public void Validation()
    {
        _voices.List(" ").Error.Code.ShouldBe(ProviderErrorCodes.InvalidRequest);
        _voices.List("yok").Error.Code.ShouldBe(ProviderErrorCodes.ModelNotFound);
        _voices.List("gpt-image-2").Error.Code.ShouldBe(ProviderErrorCodes.ModelNotSupported);
        _voices.List("supertonic-3").Error.Code.ShouldBe(ProviderErrorCodes.ModelNotSupported);
    }
}
