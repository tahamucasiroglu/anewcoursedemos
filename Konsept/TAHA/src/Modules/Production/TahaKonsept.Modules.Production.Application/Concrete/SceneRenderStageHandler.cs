using System.Globalization;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>SceneRender</c> aşaması (ajan, sahne başına bir komut): sahne kodunu planlanan süreyle sessiz/altyazısız render ettirir
/// (<see cref="SceneRenderStagePayload"/>); online görseller sahneye ait artefaktlar olarak eklenir. Ajan lint'te reddederse orkestratör
/// sahne kodunu hata ile yeniden yazdırır. Hash girdisi: sahne kodu çıktı hash'i, motor, çözünürlük, fps, süre, görsel adları ve görsel çıktı hash'i
/// (ses girmez: ses değişince sahneler yeniden render edilmez).
/// </summary>
public sealed class SceneRenderStageHandler : StageHandlerBase
{
    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.SceneRender;

    /// <summary>Ajan.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Agent;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scene = SceneOf(context, sceneIndex);
        return new
        {
            code = context.Outputs.OutputHashOf(PipelineStage.SceneCode, sceneIndex),
            engine = context.Settings.VideoEngine,
            w = context.Width,
            h = context.Height,
            fps = context.Fps,
            duration = scene?.DurationSec,
            images = scene?.ImageNames ?? [],
            imagesOutput = context.Outputs.OutputHashOf(PipelineStage.Images),
        };
    }

    /// <summary>Render komutu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public override Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        var scene = SceneOf(context, request.SceneIndex);
        var code = context.Outputs.OutputOf<SceneCodeOutput>(PipelineStage.SceneCode, request.SceneIndex);
        if (scene is null || code is null)
        {
            return Task.FromResult(Result.Failure<StageCommand>(ProductionErrors.MissingOutput(nameof(PipelineStage.SceneCode))));
        }

        var images = context.Outputs.OutputOf<ImagesOutput>(PipelineStage.Images)?.Images ?? [];
        var assetNames = images.Where(i => i.SceneIndex == scene.Index).Select(i => i.Name).ToList();
        var payload = new SceneRenderStagePayload
        {
            EngineKey = string.IsNullOrWhiteSpace(code.Engine) ? context.Settings.VideoEngine : code.Engine,
            SceneIndex = scene.Index,
            SceneCode = code.Code,
            Width = context.Width,
            Height = context.Height,
            Fps = context.Fps,
            DurationSec = scene.DurationSec,
            AssetNames = assetNames,
        };
        var artifacts = context.Outputs.ArtifactsOf(PipelineStage.Images).Where(a => a.SceneIndex == scene.Index).ToList();
        return Task.FromResult(Result.Success(Command(context, request, payload, artifacts)));
    }

    /// <summary>Sahne hazır.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public override Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = (request.SceneIndex ?? 0) + 1;
        return new StageExecutionResult { Message = "Sahne " + index.ToString(CultureInfo.InvariantCulture) + " render edildi" };
    }
}
