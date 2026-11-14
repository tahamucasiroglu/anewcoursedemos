using Shouldly;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Concrete;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Pipeline;

/// <summary>
/// Saf yardımcılar: WAV birleştirme (süreler + sessizlik), sahne kodu ayıklama/doğrulama, sahne planı süreleri ve görsel adları, ayar düzeltme.
/// </summary>
public sealed class PipelineHelperTests
{
    /// <summary>WAV'lar birleşir, boş sahneye sessizlik eklenir, süreler doğru hesaplanır.</summary>
    [Fact]
    public void Wav_combiner_concatenates_and_inserts_silence()
    {
        var combiner = new PcmWavCombiner();

        var result = combiner.Combine([TestWav.Silence(1.0), null, TestWav.Silence(0.5)], 0.6);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DurationsSec.ShouldBe([1.0, 0.6, 0.5], tolerance: 0.001);
        result.Value.TotalSec.ShouldBe(2.1, tolerance: 0.001);
        result.Value.Audio.Length.ShouldBe(44 + (int)Math.Round(2.1 * 24000) * 2);
    }

    /// <summary>Farklı biçimdeki ya da WAV olmayan ses reddedilir.</summary>
    [Fact]
    public void Wav_combiner_rejects_mixed_or_invalid_audio()
    {
        var combiner = new PcmWavCombiner();

        combiner.Combine([TestWav.Silence(1, 24000), TestWav.Silence(1, 44100)], 0.5).IsFailure.ShouldBeTrue();
        combiner.Combine([[1, 2, 3, 4]], 0.5).IsFailure.ShouldBeTrue();
    }

    /// <summary>Kod çiti ayıklanır; motor işaretleri ve yasak ifadeler denetlenir.</summary>
    [Fact]
    public void Scene_code_validator_extracts_and_checks_markers()
    {
        var validator = new EngineSceneCodeValidator();

        validator.Extract("hyperframes", "Açıklama\n```html\n" + FakeProviderGateway.ValidCode + "\n```\nson").Value.ShouldBe(FakeProviderGateway.ValidCode);
        validator.Extract("remotion", FakeProviderGateway.ValidCode).IsFailure.ShouldBeTrue();
        validator.Extract("hyperframes", FakeProviderGateway.ValidCode.Replace("</body>", "<script>fetch('x')</script></body>", StringComparison.Ordinal)).IsFailure.ShouldBeTrue();
        validator.Extract("bilinmeyen", FakeProviderGateway.ValidCode).Error.Code.ShouldBe("production.unknown_engine");
        validator.Extract("revideo", "import { makeScene2D } from '@revideo/2d'; export default makeScene2D('scene', function* (view) { yield; });").IsSuccess.ShouldBeTrue();
    }

    /// <summary>Plan süreleri seslendirme uzunluğundan (ipucu daha büyükse ipucu), görsel adları yalnız görsel açıkken.</summary>
    [Fact]
    public void Scene_planner_estimates_durations_and_image_names()
    {
        var planner = new NarrationScenePlanner();
        var document = FakeConceptSource.Sample();

        var withImages = planner.Plan(document, imagesEnabled: true);
        var withoutImages = planner.Plan(document with { Scenes = [document.Scenes[0] with { DurationHintSec = 20 }] }, imagesEnabled: false);

        withImages.Scenes.Count.ShouldBe(3);
        withImages.Scenes[0].ImageNames.ShouldBe(["scene-0-1.png"]);
        withImages.Scenes[1].DurationSec.ShouldBeGreaterThan(withImages.Scenes[0].DurationSec);
        withImages.Scenes.ShouldAllBe(s => s.DurationSec >= 2.5);
        withoutImages.Scenes[0].DurationSec.ShouldBe(20);
        withoutImages.Scenes[0].ImageNames.ShouldBeEmpty();
    }

    /// <summary>Ayar düzeltme: bilinmeyen motor → hyperframes, aralık dışı fps/hız → varsayılan, boş kod modeli → Opus.</summary>
    [Fact]
    public void Settings_are_normalized()
    {
        var normalized = ProductionSettingsDefaults.Normalize(new ProductionSettingsDto { VideoEngine = " REMOTION ", Fps = 500, SpeechSpeed = 9, CodeModelKey = " " });

        normalized.VideoEngine.ShouldBe("remotion");
        normalized.Fps.ShouldBe(30);
        normalized.SpeechSpeed.ShouldBe(1.0);
        normalized.CodeModelKey.ShouldBe("claude-opus-5-5");
        ProductionSettingsDefaults.Normalize(new ProductionSettingsDto { VideoEngine = "manim" }).VideoEngine.ShouldBe("hyperframes");
        ProductionSettingsDefaults.Resolution(Shared.Contracts.Concepts.VideoFormat.Horizontal).ShouldBe((1920, 1080));
    }
}
