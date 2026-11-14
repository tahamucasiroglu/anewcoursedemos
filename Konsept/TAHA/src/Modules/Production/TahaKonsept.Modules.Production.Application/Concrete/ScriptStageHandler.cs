using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Script</c> aşaması (sunucu, deterministik): konsept anlık görüntüsünü senaryo olarak sabitler. Hash girdisi konseptin tamamıdır;
/// metin değişince tüm hat yeniden çalışır. Sahnesi olmayan konsept reddedilir.
/// </summary>
public sealed class ScriptStageHandler : StageHandlerBase
{
    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Script;

    /// <summary>Sunucu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Server;

    /// <summary>Deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns><c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context) => true;

    /// <summary>Hash girdisi: konsept.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Document;
    }

    /// <summary>Konsepti senaryo çıktısı olarak yazar.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(context.SceneCount == 0
            ? Result.Failure<StageExecutionResult>(ProductionErrors.ConceptHasNoScenes)
            : Output(context.Document, "Konsept incelendi (" + context.SceneCount + " sahne)"));
    }
}
