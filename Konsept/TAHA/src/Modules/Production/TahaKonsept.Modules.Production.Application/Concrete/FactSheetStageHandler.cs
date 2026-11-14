using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>FactSheet</c> aşaması (sunucu, deterministik geçiş): konsept zaten kaynaklı/gerçek bilgiyle üretildiği için ek LLM çağrısı yapmaz;
/// kaynak listesini (varsa) bilgi özeti olarak taşır. Kaynak yoksa "kullanılmadı" olarak geçer.
/// </summary>
public sealed class FactSheetStageHandler : StageHandlerBase
{
    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.FactSheet;

    /// <summary>Sunucu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Server;

    /// <summary>Deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns><c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context) => true;

    /// <summary>Hash girdisi: senaryo çıktısı + kaynaklar.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new { script = context.Outputs.OutputHashOf(PipelineStage.Script), sources = context.Document.Sources };
    }

    /// <summary>Bilgi özetini üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sources = context.Document.Sources.Where(s => !string.IsNullOrWhiteSpace(s.Title) || !string.IsNullOrWhiteSpace(s.Url)).ToList();
        var output = new FactSheetOutput { Used = sources.Count > 0, Sources = sources };
        return Task.FromResult(Output(output, sources.Count > 0 ? sources.Count + " kaynak" : "Kaynak yok; atlandı"));
    }
}
