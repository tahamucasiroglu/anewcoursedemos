using Shouldly;
using TahaKonsept.Agent.Engines.Speech.Concrete.Python;
using TahaKonsept.Agent.Engines.Speech.Devices;
using TahaKonsept.Agent.Engines.Speech.Devices.Concrete;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Python.Protocol;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Engines;

/// <summary>
/// "Önce GPU" kararları (GPU'suz): cihaz seçici, hafif motorların CUDA tekerleği/cihazı, CPU'ya düşüşün uyarı olarak loglanması
/// ve işçi protokolündeki cihaz alanları.
/// </summary>
public sealed class GpuFirstSpeechTests
{
    /// <summary>GPU kullanabilen motor sürücü varken cuda; yokken nedenle cpu; GPU kullanamayan motor her zaman tasarım gereği cpu.</summary>
    [Fact]
    public void Selector_prefers_cuda_and_explains_fallback()
    {
        var withGpu = new NvidiaTorchDeviceSelector(new FakeGpuCapabilityProbe(true));
        var withoutGpu = new NvidiaTorchDeviceSelector(new FakeGpuCapabilityProbe(false));

        withGpu.Select(supportsGpu: true).ShouldBe(TorchDeviceDecision.Gpu);
        withGpu.Select(supportsGpu: false).ShouldBe(TorchDeviceDecision.CpuByDesign);

        var fallback = withoutGpu.Select(supportsGpu: true);
        fallback.Device.ShouldBe("cpu");
        fallback.UsesGpu.ShouldBeFalse();
        fallback.FallbackReason.ShouldNotBeNull().ShouldContain("NVIDIA");
        withoutGpu.Select(supportsGpu: false).FallbackReason.ShouldBeNull();
    }

    /// <summary>
    /// Hafif motorlar (Kokoro, MMS, EMA Lightning, Ahenk Cep) GPU varken CUDA 12.8 tekerleği + cuda; GPU yokken CPU tekerleği + cpu
    /// (parmak izi değişir → ortam yeniden kurulur). Piper her iki durumda da CPU.
    /// </summary>
    /// <param name="hasDriver">NVIDIA sürücüsü var mı.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Light_engines_follow_gpu_availability(bool hasDriver)
    {
        var services = Services(hasDriver, out _);
        using var kokoro = new KokoroSpeechEngine(services);
        using var mms = new MmsTurkishSpeechEngine(services);
        using var ema = new EmaLightningSpeechEngine(services);
        using var cep = new AhenkCepSpeechEngine(services);
        using var piper = new PiperTurkishSpeechEngine(services);

        foreach (var engine in new PythonSpeechEngineBase[] { kokoro, mms, ema, cep })
        {
            engine.Device.ShouldBe(hasDriver ? "cuda" : "cpu", engine.Key);
            engine.TorchIndexUrl.ShouldBe(hasDriver ? "https://download.pytorch.org/whl/cu128" : null, engine.Key);
        }

        piper.Device.ShouldBe("cpu");
        piper.TorchIndexUrl.ShouldBeNull();

        using var cpuKokoro = new KokoroSpeechEngine(Services(false, out _));
        using var gpuKokoro = new KokoroSpeechEngine(Services(true, out _));
        cpuKokoro.RequirementsFingerprint.ShouldNotBe(gpuKokoro.RequirementsFingerprint);
    }

    /// <summary>GPU gerektiren motorlar GPU kararından bağımsız olarak CUDA tekerleği kurar (önceki davranış korunur).</summary>
    [Fact]
    public void Heavy_engines_keep_cuda_wheels_without_gpu()
    {
        using var chatterbox = new ChatterboxSpeechEngine(Services(false, out _));

        chatterbox.TorchIndexUrl.ShouldBe("https://download.pytorch.org/whl/cu124");
        chatterbox.Device.ShouldBe("cpu");
    }

    /// <summary>Seçici CPU'ya düştüyse işçi yüklenirken neden Warning olarak loglanır (sessiz düşüş yok).</summary>
    [Fact]
    public async Task Selector_fallback_is_logged_as_warning()
    {
        var root = TestEnvironment.NewTempDir("gpu-warn");
        var script = Path.Combine(root, "tts_worker.py");
        await File.WriteAllTextAsync(script, "# test", TestContext.Current.CancellationToken);
        var services = Services(false, out var logs, root);
        await using var engine = new ReleaseProbeSpeechEngine(services, 4096, script);

        (await engine.PingAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        engine.Device.ShouldBe("cpu");
        logs.Warnings.ShouldContain(w => w.Contains("GPU yerine CPU", StringComparison.Ordinal) && w.Contains("NVIDIA", StringComparison.Ordinal));
    }

    /// <summary>cuda istendi ama adapter CPU'ya düştüyse (load yanıtında device=cpu + device_note) neden Warning olarak loglanır.</summary>
    [Fact]
    public async Task Adapter_fallback_is_logged_as_warning()
    {
        var root = TestEnvironment.NewTempDir("adapter-warn");
        var script = Path.Combine(root, "tts_worker.py");
        await File.WriteAllTextAsync(script, "# test", TestContext.Current.CancellationToken);
        var services = Services(true, out var logs, root);
        const string load = "{\"ok\": true, \"device\": \"cpu\", \"device_note\": \"cuda istendi ama torch 2.8.0 CUDA'sız kurulmuş\"}";
        await using var engine = new ReleaseProbeSpeechEngine(services, 4096, script, load);

        (await engine.PingAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        engine.Device.ShouldBe("cuda");
        logs.Warnings.ShouldContain(w => w.Contains("CPU'ya düştü", StringComparison.Ordinal) && w.Contains("CUDA'sız", StringComparison.Ordinal));
    }

    /// <summary>Cihaz düşüşü açıklaması: yalnız cuda istenip cpu dönünce metin üretilir; eski işçi (device yok) sessizce geçer.</summary>
    [Fact]
    public void DescribeDeviceFallback_only_reports_real_fallbacks()
    {
        PythonSpeechEngineBase.DescribeDeviceFallback("cuda", new WorkerResponse { Ok = true, Device = "cpu", DeviceNote = "VRAM yetmedi" })
            .ShouldBe("VRAM yetmedi");
        PythonSpeechEngineBase.DescribeDeviceFallback("cuda", new WorkerResponse { Ok = true, Device = "cpu" }).ShouldNotBeNull();
        PythonSpeechEngineBase.DescribeDeviceFallback("cuda", new WorkerResponse { Ok = true, Device = "cuda" }).ShouldBeNull();
        PythonSpeechEngineBase.DescribeDeviceFallback("cuda", new WorkerResponse { Ok = true }).ShouldBeNull();
        PythonSpeechEngineBase.DescribeDeviceFallback("cpu", new WorkerResponse { Ok = true, Device = "cpu" }).ShouldBeNull();
    }

    /// <summary>load yanıtındaki device/device_note ayrışır; [taha:warn] stderr satırları uyarı sayılır.</summary>
    [Fact]
    public void Protocol_carries_device_and_recognizes_warnings()
    {
        WorkerProtocol.TryParseResponse("{\"ok\": true, \"sample_rate\": 16000, \"device\": \"cpu\", \"device_note\": \"x\"}", out var load).ShouldBeTrue();
        load!.Device.ShouldBe("cpu");
        load.DeviceNote.ShouldBe("x");

        WorkerProtocol.IsWarningLine("[taha:warn] GPU yerine CPU: ...").ShouldBeTrue();
        WorkerProtocol.IsWarningLine("  [taha:warn] boşluklu").ShouldBeTrue();
        WorkerProtocol.IsWarningLine("Downloading model 45%").ShouldBeFalse();
        WorkerProtocol.IsWarningLine(null).ShouldBeFalse();
    }

    private static SpeechEngineServices Services(bool hasDriver, out ListLoggerFactory logs, string? root = null)
    {
        logs = new ListLoggerFactory();
        return TestEnvironment.CreateServices(new TestAgentPaths(root ?? TestEnvironment.NewTempDir("gpu"))) with
        {
            LoggerFactory = logs,
            DeviceSelector = new NvidiaTorchDeviceSelector(new FakeGpuCapabilityProbe(hasDriver)),
        };
    }
}
