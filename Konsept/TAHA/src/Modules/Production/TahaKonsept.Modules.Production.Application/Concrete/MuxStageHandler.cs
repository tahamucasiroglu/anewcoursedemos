using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Mux</c> aşaması (ajan): birleşik videoyu <c>Videos</c> klasörüne taşır, küçük resmi hazırlar ve ajanın video dizinine kaydeder
/// (<see cref="MuxStagePayload"/>; video kimliği komuttaki <c>VideoId</c>). Hash girdisi video kimliğini içerir — her sürüm yeni video kaydı oluşturur.
/// Tamamlanınca orkestratör <c>IJobFinalizer</c> ile video kaydını yazar.
/// </summary>
public sealed class MuxStageHandler : StageHandlerBase
{
    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Mux;

    /// <summary>Ajan.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Agent;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new { compose = context.Outputs.OutputHashOf(PipelineStage.Compose), title = context.Title, video = context.VideoId };
    }

    /// <summary>Son dosya komutu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public override Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Result.Success(Command(context, request, new MuxStagePayload { Title = context.Title })));
    }

    /// <summary>Video kaydedildi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public override Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Output(new MuxOutput { VideoId = context.VideoId }, "Video kaydedildi");
    }
}
