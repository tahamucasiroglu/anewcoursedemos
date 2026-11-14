using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IStageHandler"/> ortak davranışı: varsayılan olarak sunucu/ajan yürütmesi "yanlış yer" hatası döner (somut sınıf ilgili olanı ezer),
/// ajan sonucu olduğu gibi çıktıya çevrilir. Yardımcılar: <see cref="Command"/> (StageCommand kurar: <c>JobId</c> = çalışma alanı,
/// <c>VideoId</c>, aşama, sahne, hash, payload JSON, artefaktlar), <see cref="Output"/> (başarılı sonuç), <see cref="SceneOf"/>.
/// </summary>
public abstract class StageHandlerBase : IStageHandler
{
    /// <summary>İşlenen aşama.</summary>
    public abstract PipelineStage Stage { get; }

    /// <summary>Sahne başına mı.</summary>
    public virtual bool PerScene => PipelineStages.IsPerScene(Stage);

    /// <summary>Çalışacağı yer.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public abstract StageLocation ResolveLocation(PipelineContext context);

    /// <summary>Deterministik mi (varsayılan: hayır).</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Deterministikse <c>true</c>.</returns>
    public virtual bool IsDeterministic(PipelineContext context) => false;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Girdi nesnesi.</returns>
    public abstract object BuildHashInput(PipelineContext context, int? sceneIndex);

    /// <summary>Sunucuda çalıştırır (varsayılan: desteklenmez).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<StageExecutionResult>(ProductionErrors.WrongLocation(Stage.ToString())));

    /// <summary>Ajan komutunu kurar (varsayılan: desteklenmez).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public virtual Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<StageCommand>(ProductionErrors.WrongLocation(Stage.ToString())));

    /// <summary>Ajan sonucunu çıktıya çevirir (varsayılan: <c>OutputJson</c> olduğu gibi).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public virtual Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new StageExecutionResult { OutputJson = report.OutputJson, CostUsd = report.CostUsd, Message = PipelineStages.DisplayName(Stage) + " tamamlandı" };
    }

    /// <summary>Ajan komutu kurar.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="payload">Aşama girdisi (Shared.Contracts payload kaydı).</param>
    /// <param name="artifacts">İndirilecek artefaktlar.</param>
    /// <returns>Komut.</returns>
    protected StageCommand Command(PipelineContext context, StageExecutionRequest request, object payload, IReadOnlyList<ArtifactDownload>? artifacts = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        return new StageCommand
        {
            CommandId = Guid.CreateVersion7(),
            JobId = context.WorkspaceId,
            VideoId = context.VideoId,
            Stage = Stage,
            SceneIndex = request.SceneIndex,
            InputHash = request.InputHash,
            PayloadJson = ProductionJson.Write(payload),
            Settings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["engine"] = context.Settings.VideoEngine,
                ["speechModel"] = context.Settings.SpeechModelKey ?? string.Empty,
                ["imageModel"] = context.Settings.ImageModelKey ?? string.Empty,
            },
            Artifacts = artifacts ?? [],
        };
    }

    /// <summary>Başarılı çıktı.</summary>
    /// <typeparam name="T">Çıktı türü.</typeparam>
    /// <param name="output">Çıktı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="costUsd">Maliyet.</param>
    /// <param name="artifacts">Artefaktlar.</param>
    /// <returns>Sonuç.</returns>
    protected static Result<StageExecutionResult> Output<T>(T output, string message, decimal costUsd = 0, IReadOnlyList<ArtifactDownload>? artifacts = null) =>
        new StageExecutionResult { OutputJson = ProductionJson.Write(output), Message = message, CostUsd = costUsd, Artifacts = artifacts ?? [] };

    /// <summary>Planlanan sahne (indeks yoksa ya da aralık dışındaysa <c>null</c>).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Sahne.</returns>
    protected static PlannedScene? SceneOf(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scenes = context.Outputs.ScenePlan.Scenes;
        return sceneIndex is int i && i >= 0 && i < scenes.Count ? scenes[i] : null;
    }
}
