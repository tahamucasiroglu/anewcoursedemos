using Shouldly;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Engines.Speech.Audio;
using TahaKonsept.Agent.Engines.Speech.Concrete;
using TahaKonsept.Agent.Engines.Speech.Onnx;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Live;

/// <summary>
/// Supertonic 3 canlı sentez testi: <c>.tools-dev/speech/models/supertonic-3/onnx</c> varsa eksik dosyaları (ses stilleri) indirir,
/// Türkçe cümleyi seslendirir ve <c>.tools-dev/speech/out</c> altına yazar. Model yoksa atlanır.
/// </summary>
public sealed class SupertonicLiveTests
{
    private const string TurkishSentence =
        "Merhaba, bu Taha Konsept için bir ses denemesidir. İkinci Mehmed bin dört yüz elli üç yılında İstanbul'u fethetti.";

    /// <summary>Türkçe cümle makul sürede (5–20 sn) ve 44.1 kHz WAV olarak üretilir; ses sessiz değildir.</summary>
    [Fact]
    public async Task Synthesizes_turkish_sentence_to_plausible_wav()
    {
        var modelDir = Path.Combine(TestEnvironment.LiveModelsDir, "supertonic-3", "onnx");
        Assert.SkipUnless(File.Exists(Path.Combine(modelDir, "vector_estimator.onnx")), "Supertonic modeli .tools-dev/speech/models altında yok.");
        var ct = TestContext.Current.CancellationToken;
        var services = TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("st"), TestEnvironment.LiveModelsDir));
        using var engine = new SupertonicSpeechEngine(services);

        var ready = await engine.EnsureReadyAsync(null, ct);
        ready.IsSuccess.ShouldBeTrue(ready.Error.Message);
        engine.IsInstalled().ShouldBeTrue();

        var outDir = Path.Combine(TestEnvironment.SpeechToolsDir, "out");
        var output = Path.Combine(outDir, "supertonic-tr-M1.wav");
        var result = await engine.SynthesizeAsync(new SpeechRequest(TurkishSentence, "tr", "M1", null, 1.0, output), ct);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.DurationSec.ShouldBeInRange(5, 20);
        WavFile.TryReadInfo(output, out var info).ShouldBeTrue();
        info!.SampleRate.ShouldBe(44100);
        info.DurationSec.ShouldBe(result.Value.DurationSec, 0.01);
        RootMeanSquare(output).ShouldBeGreaterThan(0.01);
        TestContext.Current.SendDiagnosticMessage($"Supertonic ({engine.ExecutionProvider}): {result.Value.DurationSec:F2} sn → {output}");

        var female = await engine.SynthesizeAsync(new SpeechRequest(TurkishSentence, "tr", "F1", null, 1.2, Path.Combine(outDir, "supertonic-tr-F1-fast.wav")), ct);
        female.IsSuccess.ShouldBeTrue(female.Error.Message);
        female.Value.DurationSec.ShouldBeLessThan(result.Value.DurationSec);
    }

    /// <summary>
    /// ReleaseAsync DirectML (GPU) oturumunu kapatır, sonraki sentez oturumu yeniden açar; CPU oturumu sıcak kalır.
    /// GPU tarafı yalnız NVIDIA'lı makinede denetlenir.
    /// </summary>
    [Fact]
    public async Task Release_closes_gpu_session_and_keeps_cpu_session()
    {
        var modelDir = Path.Combine(TestEnvironment.LiveModelsDir, "supertonic-3", "onnx");
        Assert.SkipUnless(File.Exists(Path.Combine(modelDir, "vector_estimator.onnx")), "Supertonic modeli .tools-dev/speech/models altında yok.");
        var ct = TestContext.Current.CancellationToken;
        var services = TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("st-release"), TestEnvironment.LiveModelsDir));
        var outDir = TestEnvironment.NewTempDir("st-release-out");
        using (var setup = new SupertonicSpeechEngine(services))
        {
            (await setup.EnsureReadyAsync(null, ct)).IsSuccess.ShouldBeTrue();
        }

        using var cpu = new SupertonicSpeechEngine(services) { PreferGpu = false };
        (await cpu.SynthesizeAsync(new SpeechRequest("Merhaba.", "tr", "M1", null, 1.0, Path.Combine(outDir, "cpu.wav")), ct)).IsSuccess.ShouldBeTrue();
        await cpu.ReleaseAsync();
        cpu.ExecutionProvider.ShouldBe(OnnxExecutionProvider.Cpu);

        using var gpu = new SupertonicSpeechEngine(services);
        (await gpu.SynthesizeAsync(new SpeechRequest("Merhaba.", "tr", "M1", null, 1.0, Path.Combine(outDir, "gpu-1.wav")), ct)).IsSuccess.ShouldBeTrue();
        if (gpu.ExecutionProvider != OnnxExecutionProvider.DirectMl)
        {
            Assert.Skip("DirectML kullanılamadı (GPU yok).");
        }

        await gpu.ReleaseAsync();
        gpu.ExecutionProvider.ShouldBeNull();
        (await gpu.SynthesizeAsync(new SpeechRequest("Merhaba.", "tr", "M1", null, 1.0, Path.Combine(outDir, "gpu-2.wav")), ct)).IsSuccess.ShouldBeTrue();
        gpu.ExecutionProvider.ShouldBe(OnnxExecutionProvider.DirectMl);
    }

    private static double RootMeanSquare(string path)
    {
        var data = File.ReadAllBytes(path).AsSpan(44);
        double sum = 0;
        var count = data.Length / 2;
        for (var i = 0; i < count; i++)
        {
            var sample = BitConverter.ToInt16(data.Slice(i * 2, 2)) / 32768.0;
            sum += sample * sample;
        }

        return Math.Sqrt(sum / Math.Max(1, count));
    }
}
