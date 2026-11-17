using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Application.Rules;

/// <summary>İş durumu ve aşama anahtarları (0006 Req 13 tek listesi; 0017 aşamaları) ve sürüm işinin özeti (spec 0020).</summary>
public static class JobNames
{
    /// <summary>Durum anahtarı.</summary>
    /// <param name="status">Durum.</param>
    /// <returns>Anahtar.</returns>
    public static string Status(ProductionJobStatus status) => status switch
    {
        ProductionJobStatus.Queued => "queued",
        ProductionJobStatus.Producing => "producing",
        ProductionJobStatus.Stopped => "stopped",
        ProductionJobStatus.WaitingForAgent => "waiting_for_agent",
        ProductionJobStatus.Failed => "failed",
        _ => "completed",
    };

    /// <summary>Aşama anahtarı.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Anahtar.</returns>
    public static string Stage(PipelineStage stage) => stage switch
    {
        PipelineStage.Analysis => "analysis",
        PipelineStage.Code => "code",
        PipelineStage.Images => "images",
        PipelineStage.Voice => "voice",
        PipelineStage.Render => "render",
        _ => "merge",
    };

    /// <summary>Aşama durumu.</summary>
    /// <param name="stage">Aşama ilerlemesi.</param>
    /// <returns>DTO.</returns>
    public static StageDto ToDto(StageProgress stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return new(
            Stage(stage.Stage),
            stage.State switch
            {
                StageState.Running => "running",
                StageState.Done => "done",
                StageState.Skipped => "skipped",
                StageState.Failed => "failed",
                _ => "pending",
            },
            stage.Device);
    }

    /// <summary>Sürüm işinin özeti (spec 0020 Req 9–10).</summary>
    /// <param name="job">İş.</param>
    /// <returns>DTO.</returns>
    public static ActiveVersionDto Active(ProductionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var running = job.Progress?.Stages.FirstOrDefault(s => s.State == StageState.Running)?.Stage;
        return new ActiveVersionDto(
            job.Id,
            job.Version?.Kind == VersionKind.Regenerate ? "regenerate" : "new",
            Status(job.Status),
            running is { } stage ? Stage(stage) : null,
            job.Status == ProductionJobStatus.Failed ? job.Progress?.Error : null,
            [.. (job.Progress?.Stages ?? []).Select(ToDto)],
            job.Progress?.CostUsd ?? 0m);
    }
}
