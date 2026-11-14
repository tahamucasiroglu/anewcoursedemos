using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace TahaKonsept.Agent.Engines.Speech.Supertonic;

/// <summary>
/// Supertonic 3 ONNX hattı (resmi örneğin C# karşılığı, tek konuşmacı):
/// metin → <c>duration_predictor</c> (süre, hız çarpanına bölünür) → <c>text_encoder</c> (metin gömmesi) →
/// gürültülü gizil örnekleme → <c>vector_estimator</c> ile <c>totalSteps</c> adım akış eşleme (flow matching) → <c>vocoder</c> (dalga).
/// Uzun metin <see cref="SupertonicTextChunker"/> ile bölünür, parçalar arasına sessizlik eklenir (<see cref="Synthesize"/>).
/// CUDA sağlayıcısı istenirse denenir; yoksa CPU (<see cref="ExecutionProvider"/>). Oturumlar tek seferlik yüklenir, iş parçacığı güvenli değildir
/// (çağıran kilitler).
/// </summary>
internal sealed class SupertonicSynthesizer : IDisposable
{
    private readonly SupertonicConfig _config;
    private readonly SupertonicTextProcessor _textProcessor;
    private readonly InferenceSession _durationPredictor;
    private readonly InferenceSession _textEncoder;
    private readonly InferenceSession _vectorEstimator;
    private readonly InferenceSession _vocoder;

    private SupertonicSynthesizer(
        SupertonicConfig config,
        SupertonicTextProcessor textProcessor,
        InferenceSession durationPredictor,
        InferenceSession textEncoder,
        InferenceSession vectorEstimator,
        InferenceSession vocoder,
        string executionProvider)
    {
        _config = config;
        _textProcessor = textProcessor;
        _durationPredictor = durationPredictor;
        _textEncoder = textEncoder;
        _vectorEstimator = vectorEstimator;
        _vocoder = vocoder;
        ExecutionProvider = executionProvider;
    }

    /// <summary>Çıkış örnekleme hızı (Hz).</summary>
    public int SampleRate => _config.SampleRate;

    /// <summary>Kullanılan çalıştırma sağlayıcısı (<c>CUDA</c> ya da <c>CPU</c>).</summary>
    public string ExecutionProvider { get; }

    /// <summary><c>onnx</c> klasöründen modeli yükler.</summary>
    /// <param name="onnxDir">tts.json, unicode_indexer.json ve dört .onnx dosyasının klasörü.</param>
    /// <param name="preferCuda">CUDA sağlayıcısı denensin mi.</param>
    /// <returns>Hazır sentezleyici.</returns>
    public static SupertonicSynthesizer Load(string onnxDir, bool preferCuda)
    {
        var config = SupertonicConfig.Load(Path.Combine(onnxDir, "tts.json"));
        var textProcessor = SupertonicTextProcessor.Load(Path.Combine(onnxDir, "unicode_indexer.json"));
        var (options, provider) = CreateOptions(preferCuda);
        using (options)
        {
            InferenceSession Open(string name) => new(Path.Combine(onnxDir, name), options);
            return new SupertonicSynthesizer(
                config,
                textProcessor,
                Open("duration_predictor.onnx"),
                Open("text_encoder.onnx"),
                Open("vector_estimator.onnx"),
                Open("vocoder.onnx"),
                provider);
        }
    }

    /// <summary>Metni (gerekirse parçalayarak) seslendirir.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="language">Dil kodu.</param>
    /// <param name="style">Ses stili.</param>
    /// <param name="totalSteps">Akış eşleme adım sayısı (kalite/hız dengesi; varsayılan 8).</param>
    /// <param name="speed">Hız (resmi varsayılan 1.05).</param>
    /// <param name="silenceSec">Parçalar arası sessizlik (sn).</param>
    /// <param name="random">Gürültü üreteci.</param>
    /// <param name="ct">İptal belirteci (parçalar ve adımlar arasında denetlenir).</param>
    /// <returns>Mono float örnekler.</returns>
    public float[] Synthesize(string text, string language, SupertonicVoiceStyle style, int totalSteps, float speed, float silenceSec, Random random, CancellationToken ct)
    {
        var maxLength = language is "ko" or "ja" ? 120 : 300;
        var output = new List<float>();
        var silence = new float[(int)(silenceSec * SampleRate)];
        foreach (var chunk in SupertonicTextChunker.Chunk(text, maxLength))
        {
            ct.ThrowIfCancellationRequested();
            if (output.Count > 0)
            {
                output.AddRange(silence);
            }

            output.AddRange(Infer(chunk, language, style, totalSteps, speed, random, ct));
        }

        return output.ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _durationPredictor.Dispose();
        _textEncoder.Dispose();
        _vectorEstimator.Dispose();
        _vocoder.Dispose();
    }

    private float[] Infer(string text, string language, SupertonicVoiceStyle style, int totalSteps, float speed, Random random, CancellationToken ct)
    {
        var ids = _textProcessor.Encode(text, language);
        var length = ids.Length;
        var textIds = new DenseTensor<long>(ids, [1, length]);
        var textMask = new DenseTensor<float>(Enumerable.Repeat(1f, length).ToArray(), [1, 1, length]);
        var styleTtl = new DenseTensor<float>(style.Ttl, style.TtlShape);
        var styleDp = new DenseTensor<float>(style.Dp, style.DpShape);

        float durationSec;
        using (var dp = _durationPredictor.Run(
        [
            NamedOnnxValue.CreateFromTensor("text_ids", textIds),
            NamedOnnxValue.CreateFromTensor("style_dp", styleDp),
            NamedOnnxValue.CreateFromTensor("text_mask", textMask),
        ]))
        {
            durationSec = dp.First(o => o.Name == "duration").AsTensor<float>().GetValue(0) / speed;
        }

        using var encoded = _textEncoder.Run(
        [
            NamedOnnxValue.CreateFromTensor("text_ids", textIds),
            NamedOnnxValue.CreateFromTensor("style_ttl", styleTtl),
            NamedOnnxValue.CreateFromTensor("text_mask", textMask),
        ]);
        var textEmbedding = encoded.First(o => o.Name == "text_emb").AsTensor<float>();

        var wavLength = (long)(durationSec * SampleRate);
        var chunkSize = _config.BaseChunkSize * _config.ChunkCompressFactor;
        var latentLength = Math.Max(1, (int)((wavLength + chunkSize - 1) / chunkSize));
        var latentDim = _config.LatentDim * _config.ChunkCompressFactor;

        // Gürültülü gizil (Box-Muller normal dağılım); maske tüm uzunluk boyunca 1 (tek örnek, dolgu yok).
        var latent = new float[latentDim * latentLength];
        for (var i = 0; i < latent.Length; i++)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            latent[i] = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }

        var latentMask = new DenseTensor<float>(Enumerable.Repeat(1f, latentLength).ToArray(), [1, 1, latentLength]);
        var totalStepTensor = new DenseTensor<float>(new[] { (float)totalSteps }, [1]);
        for (var step = 0; step < totalSteps; step++)
        {
            ct.ThrowIfCancellationRequested();
            using var estimated = _vectorEstimator.Run(
            [
                NamedOnnxValue.CreateFromTensor("noisy_latent", new DenseTensor<float>(latent, [1, latentDim, latentLength])),
                NamedOnnxValue.CreateFromTensor("text_emb", textEmbedding),
                NamedOnnxValue.CreateFromTensor("style_ttl", styleTtl),
                NamedOnnxValue.CreateFromTensor("text_mask", textMask),
                NamedOnnxValue.CreateFromTensor("latent_mask", latentMask),
                NamedOnnxValue.CreateFromTensor("total_step", totalStepTensor),
                NamedOnnxValue.CreateFromTensor("current_step", new DenseTensor<float>(new[] { (float)step }, [1])),
            ]);
            latent = estimated.First(o => o.Name == "denoised_latent").AsTensor<float>().ToArray();
        }

        using var vocoded = _vocoder.Run(
        [
            NamedOnnxValue.CreateFromTensor("latent", new DenseTensor<float>(latent, [1, latentDim, latentLength])),
        ]);
        var wav = vocoded.First(o => o.Name == "wav_tts").AsTensor<float>().ToArray();

        // Vokoder gizil uzunluğunun katını üretir; tahmin edilen süreye kırp.
        return wavLength > 0 && wavLength < wav.Length ? wav[..(int)wavLength] : wav;
    }

    private static (SessionOptions Options, string Provider) CreateOptions(bool preferCuda)
    {
        if (preferCuda)
        {
            var cudaOptions = new SessionOptions();
            try
            {
                cudaOptions.AppendExecutionProvider_CUDA(0);
                return (cudaOptions, "CUDA");
            }
            catch (Exception ex) when (ex is OnnxRuntimeException or EntryPointNotFoundException or DllNotFoundException)
            {
                cudaOptions.Dispose();
            }
        }

        return (new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL }, "CPU");
    }
}
