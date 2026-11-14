using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Engines.Image.Base;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Abstract;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Concrete;

/// <summary>
/// İsteğe bağlı ComfyUI motoru: kullanıcının kendi çalıştırdığı ComfyUI'ye (<see cref="ImageEngineOptions.ComfyUiBaseUrl"/>)
/// gömülü iş akışını <c>/prompt</c> ile gönderir, <c>/history</c> ile bitişi yoklar, görseli <c>/view</c> ile indirir.
/// <see cref="IsInstalled"/> = sunucu ulaşılabilir (sonuç 15 sn önbelleklenir). Model dosyalarını ComfyUI'nin kendi
/// <c>models</c> klasörü tutar; bu motor indirme yapmaz.
/// </summary>
public sealed class ComfyUiImageEngine : ImageEngineBase
{
    /// <summary>Motor anahtarı.</summary>
    public const string EngineKey = "comfyui";

    private static readonly TimeSpan ReachabilityCacheDuration = TimeSpan.FromSeconds(15);

    private readonly IComfyUiClient _client;
    private readonly IComfyUiWorkflowProvider _workflows;
    private readonly Lock _cacheLock = new();
    private bool _lastReachable;
    private long _lastCheckTimestamp;

    /// <summary>Motoru oluşturur.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    /// <param name="downloader">Dosya indirici (taban için; ComfyUI kullanmaz).</param>
    /// <param name="client">ComfyUI istemcisi.</param>
    /// <param name="workflows">İş akışı şablonları.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="logger">Günlükleyici.</param>
    public ComfyUiImageEngine(
        IImageModelCatalog catalog,
        IModelFileDownloader downloader,
        IComfyUiClient client,
        IComfyUiWorkflowProvider workflows,
        IAgentPaths paths,
        IOptions<ImageEngineOptions> options,
        ILogger<ComfyUiImageEngine> logger)
        : base(catalog, downloader, paths, options.Value, logger)
    {
        _client = client;
        _workflows = workflows;
    }

    /// <inheritdoc />
    public override string Key => EngineKey;

    /// <inheritdoc />
    public override string DisplayName => "ComfyUI (isteğe bağlı)";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedModels =>
        _workflows.ModelKeys.Where(k => Catalog.Find(k) is not null).ToList();

    /// <inheritdoc />
    public override bool IsInstalled()
    {
        lock (_cacheLock)
        {
            if (_lastCheckTimestamp != 0 && Stopwatch.GetElapsedTime(_lastCheckTimestamp) < ReachabilityCacheDuration)
            {
                return _lastReachable;
            }
        }

        var reachable = Task.Run(() => _client.IsReachableAsync(CancellationToken.None)).GetAwaiter().GetResult();
        Remember(reachable);
        return reachable;
    }

    /// <inheritdoc />
    public override bool IsModelInstalled(string modelKey) => SupportsModel(modelKey) && IsInstalled();

    /// <inheritdoc />
    public override async Task<Result> EnsureModelAsync(string modelKey, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var template = _workflows.GetTemplate(modelKey);
        if (template is null || !SupportsModel(modelKey))
        {
            return Result.Failure(Error.Validation("image.model.unsupported", $"ComfyUI motorunda '{modelKey}' için iş akışı yok."));
        }

        var ready = await EnsureReadyAsync(progress, ct).ConfigureAwait(false);
        if (ready.IsSuccess)
        {
            progress?.Report(new EngineSetupProgress($"ComfyUI hazır. Gerekli dosyalar: {string.Join(", ", template.RequiredFiles)}", 100));
        }

        return ready;
    }

    /// <inheritdoc />
    public override async Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var reachable = await _client.IsReachableAsync(ct).ConfigureAwait(false);
        Remember(reachable);
        return reachable
            ? Result.Success()
            : Result.Failure(Error.Failure("comfyui.unreachable", $"ComfyUI çalışmıyor ya da ulaşılamıyor ({Options.ComfyUiBaseUrl}). ComfyUI'yi başlatıp tekrar deneyin."));
    }

    /// <inheritdoc />
    protected override async Task<Result<ImageOutput>> GenerateCoreAsync(ImageModelDefinition model, ImageRequest request, CancellationToken ct)
    {
        var workflow = _workflows.Build(request, $"tahakonsept/{Guid.NewGuid():N}");
        if (workflow.IsFailure)
        {
            return workflow.Error;
        }

        var queued = await _client.QueuePromptAsync(workflow.Value, "tahakonsept-agent", ct).ConfigureAwait(false);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        Logger.LogInformation("ComfyUI kuyruğuna eklendi: {PromptId} ({Model})", queued.Value, model.Key);
        var started = Stopwatch.GetTimestamp();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (Stopwatch.GetElapsedTime(started) > Options.GenerationTimeout)
            {
                return Error.Failure("comfyui.timeout", $"ComfyUI görseli {Options.GenerationTimeout.TotalMinutes:0} dakikada üretmedi.");
            }

            var status = await _client.GetStatusAsync(queued.Value, ct).ConfigureAwait(false);
            if (status.IsFailure)
            {
                return status.Error;
            }

            if (status.Value is { Completed: true } done)
            {
                if (done.Failed)
                {
                    return Error.Failure("comfyui.execution", $"ComfyUI iş akışı hata verdi: {done.ErrorMessage ?? "bilinmeyen hata"}");
                }

                var image = done.Images.FirstOrDefault(i => i.Type == "output") ?? done.Images.FirstOrDefault();
                if (image is null)
                {
                    return Error.Failure("comfyui.noimage", "ComfyUI iş akışı bitti ama görsel üretmedi.");
                }

                var outputPath = Path.GetFullPath(request.OutputPath);
                var download = await _client.DownloadImageAsync(image, outputPath, ct).ConfigureAwait(false);
                return download.IsFailure ? download.Error : new ImageOutput(outputPath);
            }

            await Task.Delay(Options.ComfyUiPollInterval, ct).ConfigureAwait(false);
        }
    }

    private void Remember(bool reachable)
    {
        lock (_cacheLock)
        {
            _lastReachable = reachable;
            _lastCheckTimestamp = Stopwatch.GetTimestamp();
        }
    }
}
