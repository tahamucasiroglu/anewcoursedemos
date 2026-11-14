using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Engines.Speech.Audio;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Base;

/// <summary>
/// Tüm yerel ses motorlarının ortak tabanı. <see cref="SynthesizeAsync"/> isteği doğrular (metin, dil, hız, klonlama,
/// kurulum) ve <see cref="SynthesizeCoreAsync"/>'e verir. Yardımcılar: WAV yazma (<see cref="WriteWav"/>), süre okuma
/// (<see cref="ReadDurationSec"/>), Hugging Face dosya indirme (<see cref="DownloadHuggingFaceFilesAsync"/>, ilerleme
/// raporlu, <c>ModelsDir/&lt;Key&gt;</c> altına, önce <c>.part</c> sonra taşıma).
/// </summary>
public abstract class SpeechEngineBase : ISpeechEngine
{
    /// <summary>En düşük hız çarpanı.</summary>
    public const double MinSpeed = 0.5;

    /// <summary>En yüksek hız çarpanı.</summary>
    public const double MaxSpeed = 2.0;

    /// <summary>Ortak bağımlılıklarla tabanı kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    protected SpeechEngineBase(SpeechEngineServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
        Logger = services.LoggerFactory.CreateLogger(GetType());
    }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public abstract int? MinVramMb { get; }

    /// <inheritdoc />
    public abstract string LanguageLabel { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> Languages { get; }

    /// <inheritdoc />
    public abstract bool SupportsCloning { get; }

    /// <inheritdoc />
    public abstract IReadOnlyDictionary<string, string> Voices { get; }

    /// <summary>Ortak bağımlılıklar.</summary>
    protected SpeechEngineServices Services { get; }

    /// <summary>Motora özel logger.</summary>
    protected ILogger Logger { get; }

    /// <summary>Bu motorun model klasörü: <c>ModelsDir/&lt;Key&gt;</c>.</summary>
    protected string ModelDir => Path.Combine(Services.Paths.ModelsDir, Key);

    /// <inheritdoc />
    public abstract bool IsInstalled();

    /// <inheritdoc />
    public abstract Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct);

    /// <inheritdoc />
    public async Task<Result<SpeechOutput>> SynthesizeAsync(SpeechRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validation = Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<SpeechOutput>(validation.Error);
        }

        if (!IsInstalled())
        {
            return Result.Failure<SpeechOutput>(SpeechErrors.NotInstalled(DisplayName));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(request.OutputPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var result = await SynthesizeCoreAsync(request, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                Logger.LogInformation("{Engine}: {Chars} karakter → {Duration:F2} sn ({Path})", Key, request.Text.Length, result.Value.DurationSec, result.Value.AudioPath);
            }
            else
            {
                Logger.LogWarning("{Engine} seslendirme hatası: {Error}", Key, result.Error.Message);
            }

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Engine} seslendirme sırasında beklenmeyen hata", Key);
            return Result.Failure<SpeechOutput>(SpeechErrors.SynthesisFailed(DisplayName, ex.Message));
        }
    }

    /// <summary>Hugging Face "resolve" adresini üretir: <c>https://huggingface.co/&lt;repo&gt;/resolve/main/&lt;file&gt;</c>.</summary>
    /// <param name="repo">Depo (ör. <c>Supertone/supertonic-3</c>).</param>
    /// <param name="file">Depo içi dosya yolu (ör. <c>onnx/tts.json</c>).</param>
    /// <param name="revision">Dal/commit (varsayılan <c>main</c>).</param>
    /// <returns>İndirme adresi.</returns>
    public static Uri HuggingFaceUrl(string repo, string file, string revision = "main")
    {
        var escapedFile = string.Join('/', file.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));
        return new Uri($"https://huggingface.co/{repo.Trim('/')}/resolve/{Uri.EscapeDataString(revision)}/{escapedFile}");
    }

    /// <summary>Motora özgü sentez (istek doğrulanmış, motor kurulu, çıktı klasörü var).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ses dosyası ve süresi.</returns>
    protected abstract Task<Result<SpeechOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct);

    /// <summary>Ortak doğrulama: metin, çıktı yolu, dil, hız, klonlama/referans ses. Motor ek kural için ezebilir.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Başarı ya da doğrulama hatası.</returns>
    protected virtual Result Validate(SpeechRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result.Failure(SpeechErrors.EmptyText());
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            return Result.Failure(SpeechErrors.MissingOutput());
        }

        if (!SupportsLanguage(request.Language))
        {
            return Result.Failure(SpeechErrors.UnsupportedLanguage(DisplayName, request.Language));
        }

        if (double.IsNaN(request.Speed) || request.Speed < MinSpeed || request.Speed > MaxSpeed)
        {
            return Result.Failure(SpeechErrors.InvalidSpeed(request.Speed));
        }

        if (!string.IsNullOrWhiteSpace(request.ReferenceAudioPath))
        {
            if (!SupportsCloning)
            {
                return Result.Failure(SpeechErrors.Cloning($"{DisplayName} referans sesle klonlamayı desteklemiyor."));
            }

            if (!File.Exists(request.ReferenceAudioPath))
            {
                return Result.Failure(SpeechErrors.Cloning($"Referans ses dosyası bulunamadı: {request.ReferenceAudioPath}"));
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public virtual Task ReleaseAsync() => Task.CompletedTask;

    /// <summary>Dil kodu destekleniyor mu (<see cref="Languages"/> boşsa hepsi; <c>tr-TR</c> → <c>tr</c> olarak da denenir).</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Destekleniyorsa <c>true</c>.</returns>
    public bool SupportsLanguage(string? language)
    {
        if (Languages.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            return false;
        }

        var code = NormalizeLanguage(language);
        return Languages.Any(l => string.Equals(l, code, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(l, language, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Dil kodunu kısa biçime indirir (<c>tr-TR</c> → <c>tr</c>, küçük harf).</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Kısa kod.</returns>
    protected static string NormalizeLanguage(string language)
    {
        var trimmed = language.Trim().ToLowerInvariant().Replace('_', '-');
        var dash = trimmed.IndexOf('-', StringComparison.Ordinal);
        return dash > 0 ? trimmed[..dash] : trimmed;
    }

    /// <summary>Mono float örnekleri 16-bit WAV yazar ve sonucu döner.</summary>
    /// <param name="path">Çıktı yolu.</param>
    /// <param name="samples">Örnekler.</param>
    /// <param name="sampleRate">Örnekleme hızı.</param>
    /// <returns>Ses çıktısı.</returns>
    protected static SpeechOutput WriteWav(string path, ReadOnlySpan<float> samples, int sampleRate) =>
        new(path, WavFile.WriteMono16(path, samples, sampleRate));

    /// <summary>WAV süresini okur.</summary>
    /// <param name="path">Dosya.</param>
    /// <returns>Süre ya da <c>null</c>.</returns>
    protected static double? ReadDurationSec(string path) => WavFile.GetDurationSec(path);

    /// <summary>Model klasöründe bu dosyaların hepsi var mı.</summary>
    /// <param name="relativeFiles">Model klasörüne göre yollar.</param>
    /// <returns>Hepsi varsa <c>true</c>.</returns>
    protected bool ModelFilesExist(IEnumerable<string> relativeFiles) =>
        relativeFiles.All(f => File.Exists(Path.Combine(ModelDir, f)));

    /// <summary>
    /// Hugging Face deposundan dosyaları <c>ModelsDir/&lt;Key&gt;/&lt;dosya&gt;</c> altına indirir. Var olan dosya atlanır;
    /// yarım indirmeler <c>.part</c> olarak kalır ve sonraki denemede baştan alınır. Yüzde, toplam dosya sayısına göre
    /// (her dosya içinde bayt oranıyla) hesaplanır.
    /// </summary>
    /// <param name="repo">Depo (ör. <c>Supertone/supertonic-3</c>).</param>
    /// <param name="files">Depo içi dosya yolları.</param>
    /// <param name="progress">İlerleme (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da indirme hatası.</returns>
    protected async Task<Result> DownloadHuggingFaceFilesAsync(string repo, IReadOnlyList<string> files, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        using var client = Services.HttpClientFactory.CreateClient(SpeechEngineServices.HttpClientName);
        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var target = Path.Combine(ModelDir, file.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(target))
            {
                continue;
            }

            var fileIndex = index;
            var result = await DownloadFileAsync(
                client,
                HuggingFaceUrl(repo, file),
                target,
                fraction => progress?.Report(new EngineSetupProgress(
                    $"{DisplayName}: {file} indiriliyor ({fileIndex + 1}/{files.Count})",
                    Math.Round((fileIndex + fraction) * 100.0 / files.Count, 1))),
                ct).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return result;
            }
        }

        progress?.Report(new EngineSetupProgress($"{DisplayName}: model dosyaları hazır", 100));
        return Result.Success();
    }

    /// <summary>Tek bir dosyayı indirir (önce <c>.part</c>, sonra taşır).</summary>
    /// <param name="client">HTTP istemcisi.</param>
    /// <param name="url">Adres.</param>
    /// <param name="target">Hedef dosya.</param>
    /// <param name="onFraction">0–1 ilerleme geri çağrısı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    protected async Task<Result> DownloadFileAsync(HttpClient client, Uri url, string target, Action<double>? onFraction, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        var partial = target + ".part";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            Logger.LogInformation("İndiriliyor: {Url} → {Target}", url, target);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure(SpeechErrors.DownloadFailed(url.ToString(), $"HTTP {(int)response.StatusCode}"));
            }

            var total = response.Content.Headers.ContentLength;
            await using (var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var destination = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
            {
                var buffer = new byte[1 << 16];
                long done = 0;
                var lastReport = DateTime.MinValue;
                int read;
                while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    done += read;
                    if (total is > 0 && DateTime.UtcNow - lastReport > TimeSpan.FromMilliseconds(250))
                    {
                        lastReport = DateTime.UtcNow;
                        onFraction?.Invoke((double)done / total.Value);
                    }
                }
            }

            File.Move(partial, target, overwrite: true);
            onFraction?.Invoke(1);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or TaskCanceledException)
        {
            Logger.LogWarning(ex, "İndirme başarısız: {Url}", url);
            return Result.Failure(SpeechErrors.DownloadFailed(url.ToString(), ex.Message));
        }
    }
}
