using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>
/// <c>Images</c> aşaması: modeli destekleyen <see cref="IImageEngine"/>'i seçer (gerekirse kurar) ve her öğeyi
/// <c>images/&lt;ad&gt;</c> olarak üretir; var olan görseller atlanır.
/// </summary>
public sealed class ImagesStageExecutor : StageExecutorBase<ImagesStagePayload>
{
    private readonly IEnumerable<IImageEngine> _engines;

    /// <summary>Oluşturur.</summary>
    /// <param name="engines">Görsel motorları.</param>
    public ImagesStageExecutor(IEnumerable<IImageEngine> engines)
    {
        _engines = engines;
    }

    /// <inheritdoc />
    public override PipelineStage Stage => PipelineStage.Images;

    /// <inheritdoc />
    protected override string GetOutputDirectory(StageExecutionContext context, ImagesStagePayload payload) =>
        Path.Combine(context.JobDir, WorkspaceLayout.ImagesDir);

    /// <inheritdoc />
    protected override Result Validate(ImagesStagePayload payload, StageExecutionContext context) =>
        payload.Items.Count > 0 && string.IsNullOrWhiteSpace(payload.ModelKey) && string.IsNullOrWhiteSpace(payload.EngineKey)
            ? Error.Validation("agent.invalid_payload", "Görsel modeli belirtilmedi.")
            : Result.Success();

    /// <inheritdoc />
    protected override async Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, ImagesStagePayload payload, CancellationToken ct)
    {
        if (payload.Items.Count == 0)
        {
            return new StageExecutionOutput([]);
        }

        var engine = string.IsNullOrWhiteSpace(payload.EngineKey)
            ? _engines.FirstOrDefault(e => e.SupportedModels.Contains(payload.ModelKey, StringComparer.OrdinalIgnoreCase))
            : SelectByKey(_engines, payload.EngineKey, e => e.Key);
        if (engine is null)
        {
            return Error.NotFound("agent.engine_not_found", "'" + payload.ModelKey + "' modelini çalıştıracak görsel motoru bu bilgisayarda yok.");
        }

        var ready = await EnsureEngineReadyAsync(engine, context, 0, 10, ct);
        if (ready.IsFailure)
        {
            return ready.Error;
        }

        var directory = GetOutputDirectory(context, payload);
        var artifacts = new List<ProducedArtifact>(payload.Items.Count);
        for (var i = 0; i < payload.Items.Count; i++)
        {
            var item = payload.Items[i];
            var finalPath = Path.Combine(directory, SafeFileName(item.Name, "gorsel-" + i + ".png"));
            context.Report(10 + (90d * i / payload.Items.Count), "Görsel üretiliyor (" + (i + 1) + "/" + payload.Items.Count + ")");
            var produced = await RunCheckpointedAsync(finalPath, async partial =>
            {
                var result = await engine.GenerateAsync(
                    new ImageRequest(payload.ModelKey, item.Prompt, payload.NegativePrompt, payload.Width, payload.Height, item.Seed, payload.Steps, partial),
                    ct);
                return result.Map(o => o.ImagePath);
            });
            if (produced.IsFailure)
            {
                return produced.Error;
            }

            artifacts.Add(CreateArtifact(context, finalPath, "image", item.SceneIndex, null));
        }

        context.Report(100, "Görseller hazır.");
        return new StageExecutionOutput(artifacts);
    }
}
