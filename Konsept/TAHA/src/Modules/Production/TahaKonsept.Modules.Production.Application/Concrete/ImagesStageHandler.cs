using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Images</c> aşaması. Kapalı (ya da planda görsel yok) → sunucuda boş geçiş. Online → <see cref="IMediaGateway.GenerateImageAsync"/> ile
/// her görsel üretilir, <see cref="IArtifactStore"/>'a kaydedilir (ajan sahne render'ında indirir). Yerel → ajana <see cref="ImagesStagePayload"/>.
/// Hash girdisi: kip, model, çözünürlük ve görsel listesi (sahne, ad, istem). Görsele yazı yazdırılmaz (istem sonuna eklenir).
/// </summary>
public sealed class ImagesStageHandler : StageHandlerBase
{
    /// <summary>İstemlere eklenen "yazı yok" eki.</summary>
    public const string NoTextSuffix = ", no text, no letters, no watermark";

    private readonly IMediaGateway _media;
    private readonly IArtifactStore _artifacts;

    /// <summary>Oluşturur.</summary>
    /// <param name="media">Online görsel geçidi.</param>
    /// <param name="artifacts">Artefakt deposu.</param>
    public ImagesStageHandler(IMediaGateway media, IArtifactStore artifacts)
    {
        _media = media ?? throw new ArgumentNullException(nameof(media));
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
    }

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Images;

    /// <summary>Kipe göre yer (yerel ve görsel varsa ajan).</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.ImageMode == MediaMode.Local && Items(context).Count > 0 ? StageLocation.Agent : StageLocation.Server;
    }

    /// <summary>Kapalıyken deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Deterministikse <c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.ImageMode == MediaMode.Off || Items(context).Count == 0;
    }

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mode = context.Settings.ImageMode;
        return mode == MediaMode.Off
            ? new { mode }
            : new { mode, model = context.Settings.ImageModelKey, w = context.Width, h = context.Height, items = Items(context) };
    }

    /// <summary>Kapalı/boş geçiş ya da online üretim.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override async Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mode = context.Settings.ImageMode;
        var items = Items(context);
        if (mode == MediaMode.Off || items.Count == 0)
        {
            return Output(new ImagesOutput { Mode = mode, Images = [] }, mode == MediaMode.Off ? "Kapalı (koddan çizim)" : "Görsel istemi yok");
        }

        if (mode != MediaMode.Online)
        {
            return ProductionErrors.WrongLocation(Stage.ToString());
        }

        if (string.IsNullOrWhiteSpace(context.Settings.ImageModelKey))
        {
            return ProductionErrors.ModelMissing("Görsel");
        }

        decimal cost = 0;
        var images = new List<GeneratedImage>(items.Count);
        var artifacts = new List<ArtifactDownload>(items.Count);
        foreach (var item in items)
        {
            var generated = await _media.GenerateImageAsync(
                context.Settings.ImageModelKey,
                new ImageRequest
                {
                    Context = new CallContext(context.UserId, "image"),
                    Prompt = item.Prompt + NoTextSuffix,
                    Width = context.Width,
                    Height = context.Height,
                    Seed = item.Seed,
                    Count = 1,
                },
                cancellationToken);
            if (generated.IsFailure)
            {
                return generated.Error;
            }

            cost += generated.Value.CostUsd;
            var bytes = generated.Value.Images.FirstOrDefault();
            if (bytes is null || bytes.Length == 0)
            {
                return Error.Failure("production.image_empty", "Görsel modeli boş sonuç döndürdü (" + item.Name + ").");
            }

            using var stream = new MemoryStream(bytes, writable: false);
            var saved = await _artifacts.SaveAsync(context.UserId, context.JobId, item.Name, generated.Value.MimeType, stream, item.SceneIndex, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            artifacts.Add(saved.Value);
            images.Add(new GeneratedImage { SceneIndex = item.SceneIndex, Name = item.Name, Prompt = item.Prompt, ArtifactId = saved.Value.ArtifactId });
        }

        return Output(new ImagesOutput { Mode = mode, Images = images }, images.Count + " görsel üretildi", cost, artifacts);
    }

    /// <summary>Yerel görsel komutu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public override Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.Settings.ImageModelKey))
        {
            return Task.FromResult(Result.Failure<StageCommand>(ProductionErrors.ModelMissing("Görsel")));
        }

        var payload = new ImagesStagePayload
        {
            ModelKey = context.Settings.ImageModelKey,
            Width = context.Width,
            Height = context.Height,
            Items = Items(context).Select(i => new ImagePromptItem { SceneIndex = i.SceneIndex, Name = i.Name, Prompt = i.Prompt + NoTextSuffix, Seed = i.Seed }).ToList(),
        };
        return Task.FromResult(Result.Success(Command(context, request, payload)));
    }

    /// <summary>Yerel üretim sonucu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public override Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        var images = Items(context).Select(i => new GeneratedImage { SceneIndex = i.SceneIndex, Name = i.Name, Prompt = i.Prompt }).ToList();
        return Output(new ImagesOutput { Mode = MediaMode.Local, Images = images }, images.Count + " görsel üretildi");
    }

    private static List<ImagePromptItem> Items(PipelineContext context) =>
        context.Outputs.ScenePlan.Scenes
            .SelectMany(s => s.ImageNames.Select(n => new ImagePromptItem { SceneIndex = s.Index, Name = n, Prompt = s.VisualPrompt, Seed = StableSeed(n + "|" + s.VisualPrompt) }))
            .ToList();

    private static long StableSeed(string text)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in text)
            {
                hash = (hash ^ c) * 16777619u;
            }

            return hash & 0x7FFFFFFF;
        }
    }
}
