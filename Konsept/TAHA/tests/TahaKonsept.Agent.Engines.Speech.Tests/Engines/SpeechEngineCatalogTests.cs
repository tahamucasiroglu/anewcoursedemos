using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Concrete;
using TahaKonsept.Agent.Engines.Speech.Concrete.Python;
using TahaKonsept.Agent.Engines.Speech.DependencyInjection;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Engines;

/// <summary>DI kaydı, katalog anahtarları, dil/VRAM bilgileri ve ortak doğrulama testleri.</summary>
public sealed class SpeechEngineCatalogTests
{
    private static readonly string[] CatalogKeys =
    [
        "supertonic-3", "chatterbox-multilingual", "ema-lightning", "ahenk", "ahenk-cep", "trendyol-tts", "xtts-v2",
        "f5-tts-tr", "piper-tr", "mms-tts-tr", "omnivoice", "voxcpm2", "fish-s2-pro", "kokoro",
    ];

    /// <summary>AddSpeechEngines katalogdaki 14 anahtarı tekil olarak kaydeder; somut tür ile arayüz aynı örnektir.</summary>
    [Fact]
    public void AddSpeechEngines_registers_all_catalog_keys()
    {
        using var provider = BuildProvider();

        var engines = provider.GetServices<ISpeechEngine>().ToList();

        engines.Select(e => e.Key).ShouldBe(CatalogKeys, ignoreOrder: true);
        engines.Select(e => e.Key).Distinct().Count().ShouldBe(engines.Count);
        engines.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.DisplayName) && !string.IsNullOrWhiteSpace(e.LanguageLabel));
        provider.GetRequiredService<SupertonicSpeechEngine>().ShouldBeSameAs(engines.Single(e => e.Key == "supertonic-3"));
    }

    /// <summary>Etiket ve VRAM bilgileri plana uygun.</summary>
    [Fact]
    public void Engine_metadata_matches_plan()
    {
        using var provider = BuildProvider();
        var engines = provider.GetServices<ISpeechEngine>().ToDictionary(e => e.Key);

        foreach (var key in new[] { "ema-lightning", "ahenk", "ahenk-cep", "trendyol-tts", "piper-tr", "mms-tts-tr" })
        {
            engines[key].LanguageLabel.ShouldStartWith("Sadece Türkçe");
            engines[key].Languages.ShouldBe(["tr"]);
        }

        engines["trendyol-tts"].LanguageLabel.ShouldContain("deneysel");
        engines["kokoro"].LanguageLabel.ShouldContain("Türkçe yok");
        engines["kokoro"].Languages.ShouldNotContain("tr");
        engines["supertonic-3"].Languages.ShouldContain("tr");
        engines["supertonic-3"].MinVramMb.ShouldBeNull();
        engines["voxcpm2"].MinVramMb.ShouldBe(8192);
        engines["fish-s2-pro"].MinVramMb.ShouldBe(12288);
        engines["chatterbox-multilingual"].SupportsCloning.ShouldBeTrue();
        engines["xtts-v2"].SupportsCloning.ShouldBeTrue();
        engines["supertonic-3"].Voices.Count.ShouldBe(10);
    }

    /// <summary>Python motorları gereksinim ve adapter tanımlar; GPU motorları CUDA dizini kullanır.</summary>
    [Fact]
    public void Python_engines_declare_adapters_and_pinned_requirements()
    {
        using var provider = BuildProvider();
        var engines = provider.GetServices<ISpeechEngine>().OfType<PythonSpeechEngineBase>().ToList();

        engines.Count.ShouldBe(13);
        var workers = Path.Combine(AppContext.BaseDirectory, "PythonWorkers");
        File.Exists(Path.Combine(workers, "tts_worker.py")).ShouldBeTrue();
        foreach (var engine in engines)
        {
            engine.Requirements.ShouldNotBeEmpty();
            File.Exists(Path.Combine(workers, "adapters", engine.AdapterName + ".py")).ShouldBeTrue(engine.Key);
            if (engine.MinVramMb is not null)
            {
                engine.TorchIndexUrl.ShouldNotBeNull(engine.Key);
                engine.Device.ShouldBe("cuda");
            }
        }
    }

    /// <summary>Desteklenmeyen dil, boş metin, hız ve klonlama doğrulanır; kurulu değilse anlaşılır hata döner.</summary>
    [Fact]
    public async Task Synthesize_validates_request_before_running()
    {
        var root = TestEnvironment.NewTempDir("validate");
        var services = TestEnvironment.CreateServices(new TestAgentPaths(root));
        await using var kokoro = new KokoroSpeechEngine(services);
        await using var piper = new PiperTurkishSpeechEngine(services);
        var output = Path.Combine(root, "o.wav");
        var ct = TestContext.Current.CancellationToken;

        (await kokoro.SynthesizeAsync(new SpeechRequest("Merhaba", "tr", null, null, 1, output), ct)).Error.Code.ShouldBe("speech.unsupported_language");
        (await piper.SynthesizeAsync(new SpeechRequest("  ", "tr", null, null, 1, output), ct)).Error.Code.ShouldBe("speech.empty_text");
        (await piper.SynthesizeAsync(new SpeechRequest("Merhaba", "tr", null, null, 3, output), ct)).Error.Code.ShouldBe("speech.invalid_speed");
        (await piper.SynthesizeAsync(new SpeechRequest("Merhaba", "tr", null, output, 1, output), ct)).Error.Code.ShouldBe("speech.cloning");
        (await piper.SynthesizeAsync(new SpeechRequest("Merhaba", "tr-TR", null, null, 1, output), ct)).Error.Code.ShouldBe("speech.not_installed");
    }

    /// <summary>uv yoksa Python motoru kurulumu anlaşılır hata döner.</summary>
    [Fact]
    public async Task EnsureReady_fails_clearly_without_uv()
    {
        var services = TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("nouv")));
        await using var engine = new EmaLightningSpeechEngine(services);

        engine.IsInstalled().ShouldBeFalse();
        var result = await engine.EnsureReadyAsync(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tool.missing");
    }

    /// <summary>Hugging Face adresi doğru üretilir (yol parçaları kaçışlı).</summary>
    [Fact]
    public void HuggingFace_url_uses_resolve_main()
    {
        SpeechEngineBase.HuggingFaceUrl("Supertone/supertonic-3", "onnx/tts.json").ToString()
            .ShouldBe("https://huggingface.co/Supertone/supertonic-3/resolve/main/onnx/tts.json");
        SpeechEngineBase.HuggingFaceUrl("a/b", "dir/file name.json").AbsoluteUri
            .ShouldBe("https://huggingface.co/a/b/resolve/main/dir/file%20name.json");
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAgentPaths>(new TestAgentPaths(TestEnvironment.NewTempDir("di")));
        services.AddSingleton(NSubstitute.Substitute.For<IToolLocator>());
        services.AddSingleton(NSubstitute.Substitute.For<IProcessRunner>());
        services.AddSpeechEngines();
        return services.BuildServiceProvider();
    }
}
