using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Engines.Speech.Onnx;
using TahaKonsept.Agent.Engines.Speech.Onnx.Base;
using TahaKonsept.Agent.Engines.Speech.Onnx.Concrete;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Supertonic;

/// <summary>
/// ONNX sağlayıcı seçimi (GPU'suz): DirectML → CPU sırası, DirectML hatasında uyarılı CPU yedeği, GPU istenmezse doğrudan CPU.
/// Gerçek DirectML aygıtı açılmaz (GPU iş yükü yok).
/// </summary>
public sealed class OnnxProviderSelectionTests
{
    /// <summary>GPU isteniyorsa önce DirectML, sonra CPU; istenmiyorsa yalnız CPU.</summary>
    [Fact]
    public void Candidates_are_gpu_first()
    {
        OnnxSessionFactoryBase.Candidates(preferGpu: true).ShouldBe([OnnxExecutionProvider.DirectMl, OnnxExecutionProvider.Cpu]);
        OnnxSessionFactoryBase.Candidates(preferGpu: false).ShouldBe([OnnxExecutionProvider.Cpu]);
    }

    /// <summary>DirectML eklenebiliyorsa onu kullanır, düşüş yok.</summary>
    [Fact]
    public void Uses_directml_when_it_works()
    {
        var logs = new ListLoggerFactory();
        var factory = new FakeOnnxSessionFactory(logs.CreateLogger("onnx"), failingProvider: null);

        var result = factory.Load(preferGpu: true, _ => "oturumlar");

        result.Value.ShouldBe("oturumlar");
        result.Provider.ShouldBe(OnnxExecutionProvider.DirectMl);
        result.FallbackReason.ShouldBeNull();
        factory.Tried.ShouldBe([OnnxExecutionProvider.DirectMl]);
        logs.Warnings.ShouldBeEmpty();
    }

    /// <summary>DirectML sağlayıcısı eklenemezse CPU ile açar, nedeni döndürür ve uyarı loglar (sessiz düşüş yok).</summary>
    [Fact]
    public void Falls_back_to_cpu_with_warning_when_directml_fails()
    {
        var logs = new ListLoggerFactory();
        var factory = new FakeOnnxSessionFactory(logs.CreateLogger("onnx"), OnnxExecutionProvider.DirectMl);

        var result = factory.Load(preferGpu: true, _ => "oturumlar");

        result.Provider.ShouldBe(OnnxExecutionProvider.Cpu);
        result.FallbackReason.ShouldNotBeNull().ShouldContain("DirectMl");
        factory.Tried.ShouldBe([OnnxExecutionProvider.DirectMl, OnnxExecutionProvider.Cpu]);
        logs.Warnings.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    /// <summary>Oturum açılışı (open) DirectML'de hata verirse de CPU ile yeniden denenir.</summary>
    [Fact]
    public void Falls_back_to_cpu_when_session_open_fails_on_directml()
    {
        var logs = new ListLoggerFactory();
        var factory = new FakeOnnxSessionFactory(logs.CreateLogger("onnx"), failingProvider: null);
        var opens = 0;

        var result = factory.Load(preferGpu: true, _ => ++opens == 1 ? throw new NotSupportedException("op desteklenmiyor") : "cpu-oturumları");

        result.Value.ShouldBe("cpu-oturumları");
        result.Provider.ShouldBe(OnnxExecutionProvider.Cpu);
        result.FallbackReason.ShouldNotBeNull();
        logs.Warnings.ShouldNotBeEmpty();
    }

    /// <summary>CPU'da da açılamazsa hata yutulmaz.</summary>
    [Fact]
    public void Rethrows_when_cpu_also_fails()
    {
        var factory = new FakeOnnxSessionFactory(NullLogger.Instance, failingProvider: null);

        Should.Throw<InvalidOperationException>(() => factory.Load<string>(preferGpu: false, _ => throw new InvalidOperationException("bozuk model")));
    }

    /// <summary>Gerçek DirectML fabrikası GPU istenmeyince yalnız CPU oturum seçenekleri kurar (DirectML aygıtı açılmaz).</summary>
    [Fact]
    public void DirectMl_factory_cpu_path_needs_no_gpu()
    {
        var factory = new DirectMlOnnxSessionFactory(NullLogger<DirectMlOnnxSessionFactory>.Instance);

        var result = factory.Load(preferGpu: false, options => options.GraphOptimizationLevel.ToString());

        result.Provider.ShouldBe(OnnxExecutionProvider.Cpu);
        result.Value.ShouldBe("ORT_ENABLE_ALL");
        result.FallbackReason.ShouldBeNull();
    }

    /// <summary>
    /// DirectML fabrikası yüksek performanslı ekran kartının adını verir (log "RTX mi tümleşik GPU mu" göstersin); NVIDIA'lı makinede
    /// NVIDIA kartı (Optimus dizüstüde Intel değil). Sahte fabrika adı bilmez.
    /// </summary>
    [Fact]
    public void DirectMl_factory_names_high_performance_adapter()
    {
        new FakeOnnxSessionFactory(NullLogger.Instance, failingProvider: null).GpuAdapterName.ShouldBeNull();
        Assert.SkipUnless(OperatingSystem.IsWindows() && File.Exists(Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe")), "NVIDIA GPU yok.");

        var name = new DirectMlOnnxSessionFactory(NullLogger<DirectMlOnnxSessionFactory>.Instance).GpuAdapterName;

        name.ShouldNotBeNull().ShouldContain("NVIDIA");
    }
}
