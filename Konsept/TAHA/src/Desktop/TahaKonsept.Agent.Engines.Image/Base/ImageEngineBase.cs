using System.Globalization;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Engines.Image.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.Downloads;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.Base;

/// <summary>
/// Yerel görsel motorlarının ortak tabanı.
/// <list type="bullet">
/// <item><see cref="GenerateAsync"/>: isteği doğrular (model destekleniyor mu, boyut 8'in katı mı, istem boş mu), çıktı klasörünü açar,
/// <see cref="GenerateCoreAsync"/>'i çağırır ve beklenmeyen istisnaları <see cref="Result"/>'a çevirir.</item>
/// <item><see cref="EnsureModelAsync"/>: katalogdaki dosya setini <c>ModelsDir/{ModelsSubdirectory}</c> altına indirir; toplam ilerleme raporlar,
/// kesintide kaldığı yerden devam eder.</item>
/// <item><see cref="IsModelInstalled"/>: tüm dosyalar beklenen boyutta var mı.</item>
/// </list>
/// </summary>
public abstract class ImageEngineBase : ILocalImageEngine
{
    /// <summary>İzin verilen en küçük kenar (piksel).</summary>
    public const int MinDimension = 64;

    /// <summary>İzin verilen en büyük kenar (piksel).</summary>
    public const int MaxDimension = 4096;

    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    /// <summary>Taban oluşturur.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    /// <param name="downloader">Dosya indirici.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="options">Görsel motor ayarları.</param>
    /// <param name="logger">Günlükleyici.</param>
    protected ImageEngineBase(IImageModelCatalog catalog, IModelFileDownloader downloader, IAgentPaths paths, ImageEngineOptions options, ILogger logger)
    {
        Catalog = catalog;
        Downloader = downloader;
        Paths = paths;
        Options = options;
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedModels { get; }

    /// <summary>Desteklenen modellerin en küçük VRAM gereksinimi.</summary>
    public virtual int? MinVramMb =>
        SupportedModels.Select(k => Catalog.Find(k)?.MinVramMb).Where(v => v is not null).Min();

    /// <summary>Görsel modellerinin kök klasörü.</summary>
    public string ModelsRoot => System.IO.Path.Combine(Paths.ModelsDir, Options.ModelsSubdirectory);

    /// <summary>Model kataloğu.</summary>
    protected IImageModelCatalog Catalog { get; }

    /// <summary>Dosya indirici.</summary>
    protected IModelFileDownloader Downloader { get; }

    /// <summary>Ajan klasörleri.</summary>
    protected IAgentPaths Paths { get; }

    /// <summary>Ayarlar.</summary>
    protected ImageEngineOptions Options { get; }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public abstract bool IsInstalled();

    /// <inheritdoc />
    public abstract Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct);

    /// <summary>Katalog dosyasının yerel tam yolu.</summary>
    /// <param name="file">Dosya.</param>
    /// <returns>Tam yol.</returns>
    public string GetModelFilePath(ImageModelFile file) =>
        System.IO.Path.Combine(ModelsRoot, file.LocalPath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    /// <inheritdoc />
    public virtual bool IsModelInstalled(string modelKey)
    {
        var model = Catalog.Find(modelKey);
        return model is not null && model.Files.All(IsFilePresent);
    }

    /// <inheritdoc />
    public virtual async Task<Result> EnsureModelAsync(string modelKey, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var model = Catalog.Find(modelKey);
        if (model is null || !SupportsModel(modelKey))
        {
            return Result.Failure(Error.Validation("image.model.unsupported", $"Bu motor '{modelKey}' modelini desteklemiyor."));
        }

        var total = model.TotalSizeBytes;
        var done = model.Files.Where(IsFilePresent).Sum(f => f.SizeBytes);
        foreach (var file in model.Files.Where(f => !IsFilePresent(f)).DistinctBy(f => f.LocalPath))
        {
            var fileName = System.IO.Path.GetFileName(file.LocalPath);
            var baseDone = done;
            var fileProgress = progress is null
                ? null
                : new Progress<ModelDownloadProgress>(p => progress.Report(new EngineSetupProgress(
                    string.Create(CultureInfo.InvariantCulture, $"{model.DisplayName}: {fileName} indiriliyor ({(baseDone + p.BytesReceived) / 1048576} / {total / 1048576} MB)"),
                    total > 0 ? Math.Min(100d, (baseDone + p.BytesReceived) * 100d / total) : null)));

            Logger.LogInformation("Model dosyası indiriliyor: {Model} {File} ({Bytes} bayt)", model.Key, file.LocalPath, file.SizeBytes);
            var result = await Downloader.DownloadAsync(BuildDownloadUri(file), GetModelFilePath(file), file.SizeBytes, file.Sha256, fileProgress, ct).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Logger.LogError("Model dosyası indirilemedi: {Model} {File}: {Error}", model.Key, file.LocalPath, result.Error.Message);
                return result;
            }

            done += file.SizeBytes;
        }

        progress?.Report(new EngineSetupProgress($"{model.DisplayName} hazır.", 100));
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ImageOutput>> GenerateAsync(ImageRequest request, CancellationToken ct)
    {
        var validation = Validate(request);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var model = validation.Value;
        try
        {
            var outputDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(request.OutputPath));
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return await GenerateCoreAsync(model, request, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Engine} görsel üretiminde beklenmeyen hata ({Model})", Key, request.ModelKey);
            return Error.Failure("image.generate.unexpected", $"Görsel üretilemedi: {ex.Message}");
        }
    }

    /// <summary>İsteği doğrular ve model tanımını döner.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Model tanımı ya da doğrulama hatası.</returns>
    public Result<ImageModelDefinition> Validate(ImageRequest request)
    {
        var model = Catalog.Find(request.ModelKey);
        if (model is null || !SupportsModel(request.ModelKey))
        {
            return Error.Validation("image.model.unsupported", $"'{request.ModelKey}' modeli {DisplayName} tarafından desteklenmiyor.");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Error.Validation("image.prompt.empty", "Görsel istemi boş olamaz.");
        }

        if (!IsValidDimension(request.Width) || !IsValidDimension(request.Height))
        {
            return Error.Validation("image.size.invalid", $"Genişlik ve yükseklik {MinDimension}–{MaxDimension} aralığında ve 8'in katı olmalı (istenen {request.Width}x{request.Height}).");
        }

        if (request.Steps is < 1 or > 150)
        {
            return Error.Validation("image.steps.invalid", "Adım sayısı 1–150 aralığında olmalı.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath)
            || !AllowedExtensions.Contains(System.IO.Path.GetExtension(request.OutputPath), StringComparer.OrdinalIgnoreCase))
        {
            return Error.Validation("image.output.invalid", "Çıktı yolu .png, .jpg veya .webp uzantılı olmalı.");
        }

        return model;
    }

    /// <summary>Asıl üretim (istek doğrulanmış, çıktı klasörü hazır).</summary>
    /// <param name="model">Model tanımı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görsel dosyası ya da hata.</returns>
    protected abstract Task<Result<ImageOutput>> GenerateCoreAsync(ImageModelDefinition model, ImageRequest request, CancellationToken ct);

    /// <summary>Bu motor modeli destekliyor mu.</summary>
    /// <param name="modelKey">Model anahtarı.</param>
    /// <returns>Destekliyorsa <c>true</c>.</returns>
    protected bool SupportsModel(string modelKey) => SupportedModels.Contains(modelKey, StringComparer.OrdinalIgnoreCase);

    /// <summary>Dosyanın Hugging Face indirme adresi.</summary>
    /// <param name="file">Dosya.</param>
    /// <returns>Adres.</returns>
    protected Uri BuildDownloadUri(ImageModelFile file) =>
        new($"{Options.HuggingFaceBaseUrl.TrimEnd('/')}/{file.Repo}/resolve/main/{file.RepoPath}");

    /// <summary>Dosya beklenen boyutta var mı.</summary>
    /// <param name="file">Dosya.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    protected bool IsFilePresent(ImageModelFile file)
    {
        var info = new FileInfo(GetModelFilePath(file));
        return info.Exists && info.Length == file.SizeBytes;
    }

    private static bool IsValidDimension(int value) => value is >= MinDimension and <= MaxDimension && value % 8 == 0;
}
