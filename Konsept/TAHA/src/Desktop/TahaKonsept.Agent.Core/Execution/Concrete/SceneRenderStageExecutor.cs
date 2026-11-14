using TahaKonsept.Agent.Abstractions.Video;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>
/// <c>SceneRender</c> aşaması (sahne başına): <see cref="IVideoEngine"/>'i seçer (gerekirse kurar), görsel varlıklarını çözer,
/// önce <c>ValidateSceneAsync</c> (lint) çalıştırır — geçersizse <c>agent.scene_invalid</c> (LLM düzeltme döngüsü için ayrıntılı) —
/// sonra <c>scenes/&lt;i&gt;/scene-&lt;i&gt;.mp4</c> üretir. Var olan sahne videosu atlanır.
/// </summary>
public sealed class SceneRenderStageExecutor : StageExecutorBase<SceneRenderStagePayload>
{
    private readonly IEnumerable<IVideoEngine> _engines;

    /// <summary>Oluşturur.</summary>
    /// <param name="engines">Video motorları.</param>
    public SceneRenderStageExecutor(IEnumerable<IVideoEngine> engines)
    {
        _engines = engines;
    }

    /// <inheritdoc />
    public override PipelineStage Stage => PipelineStage.SceneRender;

    /// <inheritdoc />
    protected override string GetOutputDirectory(StageExecutionContext context, SceneRenderStagePayload payload) =>
        WorkspaceLayout.SceneDir(context.JobDir, SceneIndex(context, payload));

    /// <inheritdoc />
    protected override Result Validate(SceneRenderStagePayload payload, StageExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(payload.SceneCode))
        {
            return Error.Validation("agent.invalid_payload", "Sahne kodu boş.");
        }

        return payload.DurationSec <= 0 || payload.Width <= 0 || payload.Height <= 0 || payload.Fps <= 0
            ? Error.Validation("agent.invalid_payload", "Sahne süresi/boyutu geçersiz.")
            : Result.Success();
    }

    /// <inheritdoc />
    protected override async Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, SceneRenderStagePayload payload, CancellationToken ct)
    {
        var sceneIndex = SceneIndex(context, payload);
        var finalPath = WorkspaceLayout.SceneVideoPath(context.JobDir, sceneIndex);
        if (IsComplete(finalPath))
        {
            return new StageExecutionOutput([CreateArtifact(context, finalPath, "video", sceneIndex, payload.DurationSec)]);
        }

        var engine = SelectByKey(_engines, payload.EngineKey, e => e.Key);
        if (engine is null)
        {
            return Error.NotFound("agent.engine_not_found", "'" + payload.EngineKey + "' video motoru bu bilgisayarda yok.");
        }

        var assets = ResolveAssets(context, payload);
        if (assets.IsFailure)
        {
            return assets.Error;
        }

        var ready = await EnsureEngineReadyAsync(engine, context, 0, 5, ct);
        if (ready.IsFailure)
        {
            return ready.Error;
        }

        var workDir = Path.Combine(GetOutputDirectory(context, payload), "work");
        Directory.CreateDirectory(workDir);
        var request = new SceneRenderRequest(
            workDir, sceneIndex, payload.SceneCode, payload.Width, payload.Height, payload.Fps, payload.DurationSec, assets.Value, PartialPath(finalPath));

        context.Report(5, "Sahne kodu denetleniyor…");
        var validation = await engine.ValidateSceneAsync(request, ct);
        if (validation.IsFailure)
        {
            return new Error("agent.scene_invalid", validation.Error.Message, ErrorType.Validation)
            {
                Details = new Dictionary<string, string[]> { ["engineCode"] = [validation.Error.Code] },
            };
        }

        double? duration = null;
        var progress = new InlineProgress<double>(p => context.Report(10 + (p * 0.9), "Sahne render ediliyor…"));
        var produced = await RunCheckpointedAsync(finalPath, async partial =>
        {
            var result = await engine.RenderSceneAsync(request with { OutputPath = partial }, progress, ct);
            if (result.IsSuccess)
            {
                duration = result.Value.DurationSec;
            }

            return result.Map(o => o.VideoPath);
        });
        if (produced.IsFailure)
        {
            return produced.Error;
        }

        context.Report(100, "Sahne hazır.");
        return new StageExecutionOutput([CreateArtifact(context, finalPath, "video", sceneIndex, duration ?? payload.DurationSec)]);
    }

    private static int SceneIndex(StageExecutionContext context, SceneRenderStagePayload payload) =>
        context.Command.SceneIndex ?? payload.SceneIndex;

    private static Result<IReadOnlyList<string>> ResolveAssets(StageExecutionContext context, SceneRenderStagePayload payload)
    {
        var resolved = new List<string>(payload.AssetNames.Count);
        foreach (var name in payload.AssetNames)
        {
            var path = context.FindInput(name);
            if (path is null)
            {
                var local = Path.Combine(context.JobDir, WorkspaceLayout.ImagesDir, Path.GetFileName(name));
                path = File.Exists(local) ? local : null;
            }

            if (path is null)
            {
                return Error.NotFound("agent.asset_missing", "Sahne görseli bulunamadı: " + name);
            }

            resolved.Add(path);
        }

        return resolved;
    }
}
