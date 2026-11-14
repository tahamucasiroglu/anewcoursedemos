using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Mapping;

/// <summary>
/// Varlık → DTO dönüşümleri: sıra öğesi (aşama özetleri ve genel ilerlemeyle), aşama çalıştırması, iş ayrıntısı, video özeti/ayrıntısı, okunuş kaydı.
/// Aşama özeti: sahne bazlı aşamada sahneler toplanır (hepsi bitti → tamam/yeniden kullanıldı, biri hatalı → hata, biri çalışıyor ya da kısmen bitti → çalışıyor).
/// </summary>
public static class ProductionMappings
{
    /// <summary>Sıra öğesi DTO'su.</summary>
    /// <param name="item">Öğe.</param>
    /// <param name="job">Etkin/son iş (yoksa <c>null</c>).</param>
    /// <returns>DTO.</returns>
    public static QueueItemDto ToDto(this QueueItem item, Job? job)
    {
        ArgumentNullException.ThrowIfNull(item);
        var stages = job is null ? [] : StageSummaries(job);
        var current = stages.FirstOrDefault(s => s.Status is not (StageStatus.Completed or StageStatus.Skipped));
        var progress = item.Status == QueueItemStatus.Done ? 100 : stages.Count == 0 ? 0 : Math.Round(stages.Average(s => s.Progress), 1);
        return new QueueItemDto
        {
            Id = item.Id,
            ConceptId = item.ConceptId,
            Title = item.Title,
            Order = item.Order,
            Status = item.Status,
            JobId = item.JobId,
            VideoId = item.VideoId,
            Progress = progress,
            Message = item.IsActive ? job?.StatusMessage ?? item.Message : item.Message ?? job?.StatusMessage,
            CurrentStage = item.IsActive ? current?.Stage : null,
            WaitingForAgent = job?.WaitingForAgent ?? false,
            Version = item.Version,
            ParentVideoId = item.ParentVideoId,
            CostUsd = job?.TotalCostUsd ?? 0,
            Stages = stages,
            CreatedAt = item.CreatedAtUtc,
            StartedAt = item.StartedAtUtc,
            FinishedAt = item.FinishedAtUtc,
        };
    }

    /// <summary>İşin aşama özetleri (hat sırasıyla).</summary>
    /// <param name="job">İş.</param>
    /// <returns>Özetler.</returns>
    public static IReadOnlyList<StageSummaryDto> StageSummaries(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var result = new List<StageSummaryDto>();
        foreach (var stage in PipelineStages.Ordered)
        {
            var runs = job.Runs.Where(r => r.Stage == stage).OrderBy(r => r.Sequence).ToList();
            if (runs.Count == 0)
            {
                continue;
            }

            var done = runs.Count(r => r.IsDone);
            var running = runs.FirstOrDefault(r => r.Status == StageStatus.Running);
            var status = done == runs.Count
                ? (runs.All(r => r.Reused) ? StageStatus.Skipped : StageStatus.Completed)
                : runs.Any(r => r.Status == StageStatus.Failed) ? StageStatus.Failed
                : runs.Any(r => r.Status == StageStatus.Cancelled) ? StageStatus.Cancelled
                : running is not null || done > 0 ? StageStatus.Running
                : StageStatus.Pending;
            var progress = (done + (running?.Progress ?? 0) / 100d) / runs.Count * 100d;
            result.Add(new StageSummaryDto
            {
                Stage = stage,
                Status = status,
                Location = running?.Location ?? runs[^1].Location,
                Progress = Math.Round(Math.Clamp(progress, 0, 100), 1),
                Message = running?.Message ?? runs.LastOrDefault(r => r.Message is not null)?.Message,
                ScenesTotal = PipelineStages.IsPerScene(stage) ? runs.Count : 0,
                ScenesDone = PipelineStages.IsPerScene(stage) ? done : 0,
                Reused = runs.All(r => r.Reused),
                DurationMs = runs.Sum(r => r.DurationMs),
                CostUsd = runs.Sum(r => r.CostUsd),
            });
        }

        return result;
    }

    /// <summary>Aşama çalıştırması DTO'su.</summary>
    /// <param name="run">Çalıştırma.</param>
    /// <returns>DTO.</returns>
    public static StageRunDto ToDto(this StageRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return new StageRunDto
        {
            Id = run.Id,
            Stage = run.Stage,
            SceneIndex = run.SceneIndex,
            Status = run.Status,
            Location = run.Location,
            InputHash = run.InputHash,
            OutputHash = run.OutputHash,
            Reused = run.Reused,
            Progress = run.Progress,
            Message = run.Message,
            Attempts = run.Attempts,
            InputTokens = run.InputTokens,
            OutputTokens = run.OutputTokens,
            CostUsd = run.CostUsd,
            StartedAt = run.StartedAtUtc,
            FinishedAt = run.FinishedAtUtc,
            DurationMs = run.DurationMs,
            ErrorCode = run.ErrorCode,
            ErrorMessage = run.ErrorMessage,
        };
    }

    /// <summary>İş ayrıntısı DTO'su.</summary>
    /// <param name="job">İş.</param>
    /// <returns>DTO.</returns>
    public static JobDetailDto ToDetailDto(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new JobDetailDto
        {
            Id = job.Id,
            QueueItemId = job.QueueItemId,
            ConceptId = job.ConceptId,
            Title = job.Title,
            Status = job.Status,
            StatusMessage = job.StatusMessage,
            WaitingForAgent = job.WaitingForAgent,
            VideoId = job.VideoId,
            BaseJobId = job.BaseJobId,
            Version = job.Version,
            Width = job.Width,
            Height = job.Height,
            Fps = job.Fps,
            Settings = ProductionJson.Read<ProductionSettingsDto>(job.SettingsJson) ?? new ProductionSettingsDto(),
            Runs = job.OrderedRuns.Select(r => r.ToDto()).ToList(),
            TotalCostUsd = job.TotalCostUsd,
            CreatedAt = job.CreatedAtUtc,
            StartedAt = job.StartedAtUtc,
            FinishedAt = job.FinishedAtUtc,
        };
    }

    /// <summary>Video özeti DTO'su.</summary>
    /// <param name="video">Video.</param>
    /// <returns>DTO.</returns>
    public static VideoSummaryDto ToSummaryDto(this Video video)
    {
        ArgumentNullException.ThrowIfNull(video);
        return new VideoSummaryDto
        {
            Id = video.Id,
            ConceptId = video.ConceptId,
            JobId = video.JobId,
            Title = video.Title,
            Format = video.Format,
            DurationSec = video.DurationSec,
            CostUsd = video.TotalCostUsd,
            Version = video.Version,
            ParentVideoId = video.ParentVideoId,
            CreatedAt = video.CreatedAtUtc,
        };
    }

    /// <summary>Video ayrıntısı DTO'su (oynatma adresleri hariç).</summary>
    /// <param name="video">Video.</param>
    /// <returns>DTO.</returns>
    public static VideoDetailDto ToDetailDto(this Video video)
    {
        ArgumentNullException.ThrowIfNull(video);
        return new VideoDetailDto
        {
            Id = video.Id,
            ConceptId = video.ConceptId,
            JobId = video.JobId,
            Title = video.Title,
            Format = video.Format,
            Width = video.Width,
            Height = video.Height,
            Fps = video.Fps,
            DurationSec = video.DurationSec,
            CostUsd = video.TotalCostUsd,
            Version = video.Version,
            ParentVideoId = video.ParentVideoId,
            CreatedAt = video.CreatedAtUtc,
            StartedAt = video.StartedAtUtc,
            FinishedAt = video.FinishedAtUtc,
            Settings = ProductionJson.Read<ProductionSettingsDto>(video.SettingsJson) ?? new ProductionSettingsDto(),
            Document = ProductionJson.Read<ConceptDocument>(video.DocumentJson) ?? new ConceptDocument(),
            Scenes = ProductionJson.Read<List<VideoSceneDto>>(video.ScenesJson) ?? [],
            CostBreakdown = ProductionJson.Read<List<StageCostDto>>(video.CostBreakdownJson) ?? [],
        };
    }

    /// <summary>Okunuş kaydı DTO'su.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <returns>DTO.</returns>
    public static PronunciationDto ToDto(this Pronunciation entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new PronunciationDto
        {
            Id = entry.Id,
            Written = entry.Written,
            Spoken = entry.Spoken,
            Status = entry.Status,
            ConceptId = entry.ConceptId,
            Source = entry.Source,
            CreatedAt = entry.CreatedAtUtc,
            UpdatedAt = entry.UpdatedAtUtc,
        };
    }
}
