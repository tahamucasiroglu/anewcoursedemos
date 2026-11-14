using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Tek hat aşamasının işleyicisi (aşama başına bir somut sınıf). Görevleri: çalışacağı yeri seçmek (<see cref="ResolveLocation"/>),
/// hash girdisini üretmek (<see cref="BuildHashInput"/>: aşama girdileri + etkileyen ayarlar + üst aşama çıktı hash'leri), sunucuda çalıştırmak
/// (<see cref="ExecuteAsync"/>) ya da ajan komutunu kurmak (<see cref="BuildCommandAsync"/>) ve ajan sonucunu çıktıya çevirmek (<see cref="ApplyAgentResult"/>).
/// </summary>
public interface IStageHandler
{
    /// <summary>İşlenen aşama.</summary>
    PipelineStage Stage { get; }

    /// <summary>Sahne başına mı çalışır.</summary>
    bool PerScene { get; }

    /// <summary>Ayarlara göre çalışacağı yer.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Sonuç.</returns>
    StageLocation ResolveLocation(PipelineContext context);

    /// <summary>Çıktı yalnızca girdilere bağlı mı (yeniden kullanım planında gerçek çıktı hash'i hesaplanabilir).</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Sonuç.</returns>
    bool IsDeterministic(PipelineContext context);

    /// <summary>Girdi hash'inin kaynağı (kanonik JSON'a çevrilip SHA-256 alınır).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Sonuç.</returns>
    object BuildHashInput(PipelineContext context, int? sceneIndex);

    /// <summary>Sunucuda çalıştırır.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken);

    /// <summary>Ajan komutunu kurar.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken);

    /// <summary>Ajan sonucunu aşama çıktısına çevirir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Ajan raporu.</param>
    /// <returns>Sonuç.</returns>
    Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report);
}
