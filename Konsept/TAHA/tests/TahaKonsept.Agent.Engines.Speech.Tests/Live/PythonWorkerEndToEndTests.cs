using Shouldly;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Engines.Speech.Audio;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Live;

/// <summary>
/// uv + venv + <c>tts_worker.py</c> + <c>dummy</c> adapter ile <c>PythonSpeechEngineBase</c> uçtan uca testi.
/// uv bulunamazsa (<c>.tools-dev/speech/uv/uv.exe</c> ya da TAHA_UV) atlanır.
/// </summary>
public sealed class PythonWorkerEndToEndTests
{
    /// <summary>Kurulum, ping, iki sentez (işçi yeniden kullanılır) ve hata yanıtı uçtan uca çalışır.</summary>
    [Fact]
    public async Task Dummy_engine_installs_and_synthesizes_through_worker()
    {
        var uv = TestEnvironment.UvPath;
        Assert.SkipWhen(uv is null, "uv bulunamadı (.tools-dev/speech/uv/uv.exe ya da TAHA_UV).");
        var ct = TestContext.Current.CancellationToken;
        var root = Path.Combine(TestEnvironment.SpeechToolsDir, "e2e-root");
        var services = TestEnvironment.CreateServices(new TestAgentPaths(root), uv, new SimpleProcessRunner());
        await using var engine = new DummyPythonSpeechEngine(services);

        var setup = await engine.EnsureReadyAsync(null, ct);
        setup.IsSuccess.ShouldBeTrue(setup.Error.Message);
        engine.IsInstalled().ShouldBeTrue();
        (await engine.PingAsync(ct)).IsSuccess.ShouldBeTrue();

        var outDir = TestEnvironment.NewTempDir("e2e");
        var first = await engine.SynthesizeAsync(new SpeechRequest("Merhaba dünya, bu bir denemedir.", "tr", null, null, 1.0, Path.Combine(outDir, "a.wav")), ct);
        first.IsSuccess.ShouldBeTrue(first.Error.Message);
        first.Value.DurationSec.ShouldBe(32 * 0.06, 0.01);
        WavFile.TryReadInfo(first.Value.AudioPath, out var info).ShouldBeTrue();
        info!.SampleRate.ShouldBe(16000);

        var fast = await engine.SynthesizeAsync(new SpeechRequest("Merhaba dünya, bu bir denemedir.", "tr", null, null, 2.0, Path.Combine(outDir, "b.wav")), ct);
        fast.IsSuccess.ShouldBeTrue(fast.Error.Message);
        fast.Value.DurationSec.ShouldBe(first.Value.DurationSec / 2, 0.01);

        await engine.StopWorkerAsync();
        var afterRestart = await engine.SynthesizeAsync(new SpeechRequest("Tekrar.", "tr", null, null, 1.0, Path.Combine(outDir, "c.wav")), ct);
        afterRestart.IsSuccess.ShouldBeTrue(afterRestart.Error.Message);
    }
}
