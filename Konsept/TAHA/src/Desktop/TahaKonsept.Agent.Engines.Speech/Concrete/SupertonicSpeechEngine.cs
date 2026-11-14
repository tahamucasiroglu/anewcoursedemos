using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Onnx;
using TahaKonsept.Agent.Engines.Speech.Onnx.Concrete;
using TahaKonsept.Agent.Engines.Speech.Supertonic;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Concrete;

/// <summary>
/// Supertonic 3 (Supertone, kod MIT / model OpenRAIL-M; HF <c>Supertone/supertonic-3</c>, depo 2026-09-09'da arşivlendi).
/// Varsayılan yerel motor: Python'suz, doğrudan C# + ONNX Runtime. 31 dil, Türkçe dahil. "Önce GPU": oturumlar DirectML ile
/// (yüksek performanslı GPU) açılır; DirectML eklenemez/açılamazsa ya da ilk çalıştırmada DirectML hatası olursa CPU ile yeniden yüklenir
/// ve düşüş <c>Warning</c> olarak loglanır (<see cref="ExecutionProvider"/>). <see cref="ReleaseAsync"/>: Speech aşaması bitince DirectML
/// oturumu kapanır (~0,5 GB VRAM sonraki işin yerel görsel aşamasına kalır; Python GPU motorlarıyla aynı kural), CPU oturumu sıcak kalır.
/// Model ~400 MB, <c>ModelsDir/supertonic-3/{onnx,voice_styles}</c> altına indirilir. Sesler: F1–F5 (kadın), M1–M5 (erkek);
/// <c>VoiceId</c> bir .json yolu da olabilir (Voice Builder stili). Hız: istek hızı × 1.05 (resmi varsayılan).
/// </summary>
public sealed class SupertonicSpeechEngine : SpeechEngineBase, IDisposable
{
    /// <summary>Hugging Face deposu.</summary>
    public const string Repository = "Supertone/supertonic-3";

    /// <summary>Varsayılan ses.</summary>
    public const string DefaultVoice = "M1";

    private const float BaseSpeed = 1.05f;
    private const int DefaultSteps = 8;
    private const float ChunkSilenceSec = 0.3f;

    private static readonly string[] OnnxFiles =
    [
        "onnx/tts.json", "onnx/unicode_indexer.json", "onnx/duration_predictor.onnx",
        "onnx/text_encoder.onnx", "onnx/vector_estimator.onnx", "onnx/vocoder.onnx",
    ];

    private static readonly IReadOnlyDictionary<string, string> VoiceNames = new Dictionary<string, string>
    {
        ["F1"] = "Kadın 1", ["F2"] = "Kadın 2", ["F3"] = "Kadın 3", ["F4"] = "Kadın 4", ["F5"] = "Kadın 5",
        ["M1"] = "Erkek 1", ["M2"] = "Erkek 2", ["M3"] = "Erkek 3", ["M4"] = "Erkek 4", ["M5"] = "Erkek 5",
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, SupertonicVoiceStyle> _styles = new(StringComparer.OrdinalIgnoreCase);
    private SupertonicSynthesizer? _synthesizer;
    private bool _disposed;

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public SupertonicSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "supertonic-3";

    /// <inheritdoc />
    public override string DisplayName => "Supertonic 3";

    /// <inheritdoc />
    public override int? MinVramMb => null;

    /// <inheritdoc />
    public override string LanguageLabel => "Çok dilli (31 dil, Türkçe dahil)";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages => SupertonicLanguages.All;

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices => VoiceNames;

    /// <summary>GPU (DirectML) sağlayıcısı denensin mi (varsayılan <c>true</c>; olmazsa uyarıyla CPU).</summary>
    public bool PreferGpu { get; init; } = true;

    /// <summary>Akış eşleme adım sayısı (varsayılan 8; daha yüksek = daha temiz, daha yavaş).</summary>
    public int TotalSteps { get; init; } = DefaultSteps;

    /// <summary>Son yüklemede kullanılan çalıştırma sağlayıcısı (yüklenmediyse <c>null</c>).</summary>
    public OnnxExecutionProvider? ExecutionProvider => _synthesizer?.ExecutionProvider;

    private static IEnumerable<string> RequiredFiles => OnnxFiles.Concat(VoiceNames.Keys.Select(v => $"voice_styles/{v}.json"));

    /// <inheritdoc />
    public override bool IsInstalled() => ModelFilesExist(RequiredFiles);

    /// <inheritdoc />
    public override async Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (IsInstalled())
        {
            return Result.Success();
        }

        return await DownloadHuggingFaceFilesAsync(Repository, RequiredFiles.ToArray(), progress, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// DirectML (GPU) oturumu açıksa kapatır: model VRAM'de kalıp sonraki işin Images aşamasındaki sd.cpp'nin belleğini daraltmasın
    /// (e2e: Supertonic açıkken FLUX.2 4B'de boş VRAM 5,3 GB'tan 4,6 GB'a indi, toplam tepe 5,7/6 GB). CPU oturumu sıcak kalır.
    /// Sonraki istek oturumu yeniden açar (~1–3 sn).
    /// </summary>
    /// <returns>Görev.</returns>
    public override async Task ReleaseAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_synthesizer?.ExecutionProvider == OnnxExecutionProvider.DirectMl)
            {
                _synthesizer.Dispose();
                _synthesizer = null;
                Logger.LogInformation("Supertonic 3: DirectML oturumu kapatıldı (VRAM serbest)");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _synthesizer?.Dispose();
        _synthesizer = null;
        _gate.Dispose();
    }

    /// <inheritdoc />
    protected override Result Validate(SpeechRequest request)
    {
        var baseResult = base.Validate(request);
        if (baseResult.IsFailure)
        {
            return baseResult;
        }

        var voice = request.VoiceId;
        return string.IsNullOrWhiteSpace(voice) || VoiceNames.ContainsKey(voice) || File.Exists(voice)
            ? Result.Success()
            : Result.Failure(SpeechErrors.VoiceNotFound(voice));
    }

    /// <inheritdoc />
    protected override async Task<Result<SpeechOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(
                () =>
                {
                    var synthesizer = _synthesizer ??= LoadSynthesizer(PreferGpu);
                    var style = GetStyle(string.IsNullOrWhiteSpace(request.VoiceId) ? DefaultVoice : request.VoiceId);
                    var language = NormalizeLanguage(request.Language);
                    float[] samples;
                    try
                    {
                        samples = Synthesize(synthesizer, request, style, language, ct);
                    }
                    catch (OnnxRuntimeException ex) when (synthesizer.ExecutionProvider == OnnxExecutionProvider.DirectMl)
                    {
                        // DirectML oturumu açıldı ama çalıştırmada düştü (sürücü/işlem desteği): CPU ile yeniden yükle, bir kez daha dene.
                        Logger.LogWarning(ex, "Supertonic 3: DirectML çalıştırması başarısız, CPU ile yeniden yükleniyor");
                        synthesizer.Dispose();
                        _synthesizer = synthesizer = LoadSynthesizer(preferGpu: false);
                        samples = Synthesize(synthesizer, request, style, language, ct);
                    }

                    return Result.Success(WriteWav(request.OutputPath, samples, synthesizer.SampleRate));
                },
                ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private SupertonicSynthesizer LoadSynthesizer(bool preferGpu)
    {
        var sessions = Services.OnnxSessions ?? new DirectMlOnnxSessionFactory(Services.LoggerFactory.CreateLogger<DirectMlOnnxSessionFactory>());
        var loaded = SupertonicSynthesizer.Load(Path.Combine(ModelDir, "onnx"), preferGpu, sessions);
        if (loaded.FallbackReason is { } reason)
        {
            Logger.LogWarning("Supertonic 3: GPU yerine CPU kullanılıyor — {Reason}", reason);
        }

        if (loaded.Provider == OnnxExecutionProvider.DirectMl)
        {
            Logger.LogInformation("Supertonic 3 yüklendi ({Provider}, GPU: {Adapter})", loaded.Provider, sessions.GpuAdapterName ?? "adaptör adı okunamadı");
        }
        else
        {
            Logger.LogInformation("Supertonic 3 yüklendi ({Provider})", loaded.Provider);
        }
        return loaded.Value;
    }

    private float[] Synthesize(SupertonicSynthesizer synthesizer, SpeechRequest request, SupertonicVoiceStyle style, string language, CancellationToken ct) =>
        synthesizer.Synthesize(request.Text, language, style, TotalSteps, (float)(request.Speed * BaseSpeed), ChunkSilenceSec, Random.Shared, ct);

    private SupertonicVoiceStyle GetStyle(string voice)
    {
        if (!_styles.TryGetValue(voice, out var style))
        {
            var path = VoiceNames.ContainsKey(voice) ? Path.Combine(ModelDir, "voice_styles", voice.ToUpperInvariant() + ".json") : voice;
            style = SupertonicVoiceStyle.Load(path);
            _styles[voice] = style;
        }

        return style;
    }
}
