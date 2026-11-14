using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Engines.Image.Base;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;

/// <summary>
/// stable-diffusion.cpp motoru: her görsel için <c>sd-cli.exe</c>'yi bir kez çalıştırır (model yükleme ≈5–10 sn dahil;
/// RTX 3060 Laptop 6 GB'ta Z-Image-Turbo Q3_K 512x768 8 adım ≈25 sn ölçüldü).
/// <see cref="EnsureReadyAsync"/>: ikiliyi kurar + varsayılan modeli (<see cref="ImageEngineOptions.DefaultModelKey"/>) indirir.
/// Üretimde eksik model dosyaları <see cref="ImageEngineOptions.AutoDownloadModels"/> açıksa indirilir.
/// </summary>
public sealed class StableDiffusionCppImageEngine : ImageEngineBase
{
    /// <summary>Motor anahtarı.</summary>
    public const string EngineKey = "stable-diffusion-cpp";

    private const int TailLineCount = 30;

    private readonly ISdCppInstaller _installer;
    private readonly ISdCppArgumentBuilder _argumentBuilder;
    private readonly IProcessRunner _processRunner;

    /// <summary>Motoru oluşturur.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    /// <param name="downloader">Dosya indirici.</param>
    /// <param name="installer">sd.cpp kurucusu.</param>
    /// <param name="argumentBuilder">Argüman üreticisi.</param>
    /// <param name="processRunner">Süreç çalıştırıcı.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="logger">Günlükleyici.</param>
    public StableDiffusionCppImageEngine(
        IImageModelCatalog catalog,
        IModelFileDownloader downloader,
        ISdCppInstaller installer,
        ISdCppArgumentBuilder argumentBuilder,
        IProcessRunner processRunner,
        IAgentPaths paths,
        IOptions<ImageEngineOptions> options,
        ILogger<StableDiffusionCppImageEngine> logger)
        : base(catalog, downloader, paths, options.Value, logger)
    {
        _installer = installer;
        _argumentBuilder = argumentBuilder;
        _processRunner = processRunner;
    }

    /// <inheritdoc />
    public override string Key => EngineKey;

    /// <inheritdoc />
    public override string DisplayName => "stable-diffusion.cpp (yerel)";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedModels => Catalog.Models.Select(m => m.Key).ToList();

    /// <inheritdoc />
    public override bool IsInstalled() => _installer.FindExecutable() is not null && IsModelInstalled(Options.DefaultModelKey);

    /// <inheritdoc />
    public override async Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var executable = await _installer.EnsureInstalledAsync(progress, ct).ConfigureAwait(false);
        if (executable.IsFailure)
        {
            return executable;
        }

        return await EnsureModelAsync(Options.DefaultModelKey, progress, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<Result<ImageOutput>> GenerateCoreAsync(ImageModelDefinition model, ImageRequest request, CancellationToken ct)
    {
        var executable = await _installer.EnsureInstalledAsync(null, ct).ConfigureAwait(false);
        if (executable.IsFailure)
        {
            return executable.Error;
        }

        if (!IsModelInstalled(model.Key))
        {
            if (!Options.AutoDownloadModels)
            {
                return Error.Failure("image.model.missing", $"{model.DisplayName} dosyaları kurulu değil. Ajanın kurulum ekranından indirin.");
            }

            var download = await EnsureModelAsync(model.Key, null, ct).ConfigureAwait(false);
            if (download.IsFailure)
            {
                return download.Error;
            }
        }

        var outputPath = Path.GetFullPath(request.OutputPath);
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var arguments = _argumentBuilder.Build(model, GetModelFilePath, request with { OutputPath = outputPath }, Options.LowVram);
        var tail = new Queue<string>(TailLineCount);
        var spec = new ProcessSpec(executable.Value, arguments, Path.GetDirectoryName(executable.Value), Timeout: Options.GenerationTimeout);

        Logger.LogInformation("sd.cpp üretimi başlıyor: {Model} {Width}x{Height} tohum {Seed}", model.Key, request.Width, request.Height, request.Seed);
        var result = await _processRunner.RunAsync(spec, line => OnOutput(line, tail), ct).ConfigureAwait(false);

        if (result.TimedOut)
        {
            return Error.Failure("image.sdcpp.timeout", $"Görsel üretimi {Options.GenerationTimeout.TotalMinutes:0} dakikada bitmedi ({model.DisplayName}).");
        }

        if (!result.Succeeded || !File.Exists(outputPath))
        {
            string details;
            lock (tail)
            {
                details = string.Join(Environment.NewLine, tail);
            }

            Logger.LogError("sd.cpp başarısız (çıkış {ExitCode}): {Details}", result.ExitCode, details);
            return Error.Failure("image.sdcpp.failed", $"stable-diffusion.cpp görsel üretemedi (çıkış kodu {result.ExitCode}). Son çıktı:{Environment.NewLine}{details}");
        }

        Logger.LogInformation("sd.cpp görseli üretildi: {Path} ({Seconds:0.0} sn)", outputPath, result.Duration.TotalSeconds);
        return new ImageOutput(outputPath);
    }

    private void OnOutput(string line, Queue<string> tail)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (tail)
        {
            if (tail.Count == TailLineCount)
            {
                tail.Dequeue();
            }

            tail.Enqueue(line);
        }

        if (line.StartsWith("[E]", StringComparison.Ordinal))
        {
            Logger.LogWarning("sd.cpp: {Line}", line);
        }
    }
}
