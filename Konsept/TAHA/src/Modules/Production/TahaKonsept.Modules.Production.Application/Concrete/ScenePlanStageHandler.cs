using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>ScenePlan</c> aşaması (sunucu, deterministik): <see cref="IScenePlanner"/> ile sahne listesi, seslendirme uzunluğundan süreler ve görsel adları.
/// Hash girdisi: senaryo + bilgi özeti çıktı hash'leri ve görselin açık olup olmadığı (görsel modeli/ses ayarları plana girmez).
/// </summary>
public sealed class ScenePlanStageHandler : StageHandlerBase
{
    private readonly IScenePlanner _planner;

    /// <summary>Oluşturur.</summary>
    /// <param name="planner">Planlayıcı.</param>
    public ScenePlanStageHandler(IScenePlanner planner)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.ScenePlan;

    /// <summary>Sunucu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Server;

    /// <summary>Deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns><c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context) => true;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new
        {
            script = context.Outputs.OutputHashOf(PipelineStage.Script),
            facts = context.Outputs.OutputHashOf(PipelineStage.FactSheet),
            images = context.Settings.ImageMode != MediaMode.Off,
        };
    }

    /// <summary>Planı üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var plan = _planner.Plan(context.Document, context.Settings.ImageMode != MediaMode.Off);
        return Task.FromResult(Output(plan, plan.Scenes.Count + " sahne, ~" + Math.Round(plan.TotalDurationSec) + " sn"));
    }
}
