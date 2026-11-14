using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Speech.Audio;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Concrete;
using TahaKonsept.Agent.Engines.Speech.Concrete.Python;
using TahaKonsept.Agent.Engines.Speech.DependencyInjection;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using TahaKonsept.BuildingBlocks.Domain.Results;
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

    /// <summary>uv yoksa ve araç kurucu varsa, Python motoru kurulumu önce uv'yi kurucuya kurdurur (kullanıcı kurulum ekranına gitmez).</summary>
    [Fact]
    public async Task EnsureReady_provisions_missing_uv()
    {
        var provisioner = Substitute.For<IToolProvisioner>();
        provisioner.EnsureAsync(ToolKind.Uv, Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("test.uv_install", "uv indirilemedi")));
        var services = TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("provision"))) with { Provisioner = provisioner };
        await using var engine = new PiperTurkishSpeechEngine(services);

        var result = await engine.EnsureReadyAsync(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("test.uv_install");
        await provisioner.Received(1).EnsureAsync(ToolKind.Uv, Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>uv geçici Windows dosya kilidine takılırsa (os error 5) kurulum yeniden denenir; kalıcı hatada denenmez.</summary>
    [Fact]
    public async Task EnsureReady_retries_uv_on_transient_file_lock()
    {
        var calls = 0;
        var processes = Substitute.For<IProcessRunner>();
        processes.RunAsync(Arg.Any<ProcessSpec>(), Arg.Any<Action<string>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++calls == 1
                ? new ProcessResult(2, string.Empty, "error: Failed to copy to: C:\\x\\cpython | cause: failed to rename file: Erişim engellendi. (os error 5)", TimeSpan.Zero, false)
                : new ProcessResult(0, string.Empty, string.Empty, TimeSpan.Zero, false)));
        var services = TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("uvlock")), "uv.exe", processes);
        await using var engine = new DummyPythonSpeechEngine(services);

        var result = await engine.EnsureReadyAsync(null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        calls.ShouldBe(2);
        PythonSpeechEngineBase.IsTransientFileLock("error: No matching distribution found for piper-tts").ShouldBeFalse();
        PythonSpeechEngineBase.IsTransientFileLock("The process cannot access the file because it is being used by another process. (os error 32)").ShouldBeTrue();
    }

    /// <summary>
    /// GPU motorunda ReleaseAsync işçiyi kapatır (VRAM boşalır, sonraki istek yeni işçi açar); CPU motorunda işçi sıcak kalır.
    /// </summary>
    /// <param name="minVramMb">GPU gereksinimi (null = CPU).</param>
    /// <param name="expectedWorkers">İkinci istekten sonra açılmış işçi sayısı.</param>
    [Theory]
    [InlineData(4096, 2)]
    [InlineData(null, 1)]
    public async Task Release_stops_worker_only_for_gpu_engines(int? minVramMb, int expectedWorkers)
    {
        var root = TestEnvironment.NewTempDir("release");
        var script = Path.Combine(root, "tts_worker.py");
        await File.WriteAllTextAsync(script, "# test", TestContext.Current.CancellationToken);
        await using var engine = new ReleaseProbeSpeechEngine(TestEnvironment.CreateServices(new TestAgentPaths(root)), minVramMb, script);

        (await engine.PingAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        await engine.ReleaseAsync();
        (await engine.PingAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        engine.Device.ShouldBe(minVramMb is null ? "cpu" : "cuda");
        engine.WorkersCreated.ShouldBe(expectedWorkers);
    }

    /// <summary>
    /// Chatterbox ortamı setuptools&lt;81 kurar: resemble-perth filigranı pkg_resources ister, yoksa model yüklemesi
    /// "'NoneType' object is not callable" ile düşer (e2e'de görüldü).
    /// </summary>
    [Fact]
    public void Chatterbox_requirements_include_pkg_resources_provider()
    {
        using var chatterbox = new ChatterboxSpeechEngine(TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("cb"))));

        chatterbox.Requirements.ShouldContain("setuptools<81");
    }

    /// <summary>XTTS-v2 ortamı six kurar: coqui-tts 0.28.0 import eder ama bildirmez (e2e'de "No module named 'six'").</summary>
    [Fact]
    public void Xtts_requirements_include_six()
    {
        using var xtts = new XttsV2SpeechEngine(TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("xtts"))));

        xtts.Requirements.ShouldContain("six");
    }

    /// <summary>
    /// F5-TTS Türkçe, referans ses verilmezse ajanla gelen Türkçe referans klibini kullanır (İngilizce örnek klip anlatımı yavaş ve
    /// aksanlı yapıyordu: e2e'de 31 yerine 44 sn). Klip çıktı klasörüne kopyalanır ve adapter onu varsayılan olarak gösterir.
    /// </summary>
    [Fact]
    public void F5_default_turkish_reference_is_shipped_with_worker()
    {
        var workers = Path.Combine(AppContext.BaseDirectory, "PythonWorkers");
        var clip = Path.Combine(workers, "assets", "f5_ref_tr.wav");

        File.Exists(clip).ShouldBeTrue();
        WavFile.GetDurationSec(clip).ShouldNotBeNull().ShouldBeInRange(3.0, 12.0);
        File.ReadAllText(Path.Combine(workers, "adapters", "f5_tts.py")).ShouldContain("f5_ref_tr.wav");
    }

    /// <summary>Kokoro Türkçe desteklemez (etiket "Türkçe yok"); Türkçe destekli motorlar tr-TR'yi kabul eder.</summary>
    [Fact]
    public void Kokoro_is_labelled_without_turkish()
    {
        var services = TestEnvironment.CreateServices(new TestAgentPaths(TestEnvironment.NewTempDir("kokoro")));
        using var kokoro = new KokoroSpeechEngine(services);
        using var chatterbox = new ChatterboxSpeechEngine(services);

        kokoro.LanguageLabel.ShouldStartWith("Türkçe yok");
        kokoro.SupportsLanguage("tr-TR").ShouldBeFalse();
        kokoro.SupportsLanguage("en").ShouldBeTrue();
        chatterbox.SupportsLanguage("tr-TR").ShouldBeTrue();
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
