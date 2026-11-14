using System.Globalization;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IProductionActivity"/> ortak davranışı: Diagnostics işlem günlüğüne Türkçe satırlar yazar (kaynak <c>Production</c>).
/// Sahne bazlı aşamalarda gürültü olmasın diye başlangıç ilk sahnede, bitiş son sahnede (aşamanın toplam süresi/maliyetiyle) yazılır.
/// Biçim yardımcıları: <see cref="FormatDuration"/>, <see cref="FormatUsd"/>.
/// </summary>
public abstract class ProductionActivityBase : IProductionActivity
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private readonly IActivityLogger _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="logger">İşlem günlüğü.</param>
    protected ProductionActivityBase(IActivityLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Aşama başladı.</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task StageStartedAsync(Job job, StageRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        if (run.SceneIndex is > 0)
        {
            return Task.CompletedTask;
        }

        return _logger.InformationAsync(
            ActivitySources.Production,
            PipelineStages.DisplayName(run.Stage) + " başladı · " + job.Title,
            new { jobId = job.Id, stage = run.Stage.ToString(), location = run.Location.ToString() },
            null,
            job.UserId,
            cancellationToken);
    }

    /// <summary>Aşama bitti.</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task StageFinishedAsync(Job job, StageRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        var stageRuns = job.Runs.Where(r => r.Stage == run.Stage).ToList();
        if (run.SceneIndex is not null && stageRuns.Any(r => !r.IsDone))
        {
            return Task.CompletedTask;
        }

        var duration = stageRuns.Sum(r => r.DurationMs);
        var cost = stageRuns.Sum(r => r.CostUsd);
        var reused = stageRuns.All(r => r.Reused);
        var text = PipelineStages.DisplayName(run.Stage)
            + (reused ? " önceki sürümden alındı" : " bitti — " + FormatDuration(duration))
            + (cost > 0 ? ", " + FormatUsd(cost) : string.Empty)
            + " · " + job.Title;
        return _logger.InformationAsync(
            ActivitySources.Production,
            text,
            new { jobId = job.Id, stage = run.Stage.ToString(), scenes = stageRuns.Count, reused, costUsd = cost },
            duration,
            job.UserId,
            cancellationToken);
    }

    /// <summary>Aşama hata verdi.</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task StageFailedAsync(Job job, StageRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        return _logger.ErrorAsync(
            ActivitySources.Production,
            PipelineStages.DisplayName(run.Stage) + " başarısız · " + job.Title + " — " + (run.ErrorMessage ?? "bilinmeyen hata"),
            new { jobId = job.Id, stage = run.Stage.ToString(), scene = run.SceneIndex, code = run.ErrorCode },
            job.UserId,
            cancellationToken);
    }

    /// <summary>Bütçe nedeniyle durdu.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task BudgetStoppedAsync(Guid userId, string message, CancellationToken cancellationToken) =>
        _logger.WarningAsync(ActivitySources.Production, "Aylık bütçe aşıldı; yeni üretim başlatılmıyor. " + message, null, userId, cancellationToken);

    /// <summary>Video hazır: "Video hazırlandı — süre 57 sn, başlangıç 10:30, bitiş 11:00, $0.92".</summary>
    /// <param name="job">İş.</param>
    /// <param name="video">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task VideoReadyAsync(Job job, Video video, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(video);
        var started = job.StartedAtUtc ?? video.CreatedAtUtc;
        var finished = job.FinishedAtUtc ?? started;
        var text = "Video hazırlandı — süre " + Math.Round(video.DurationSec).ToString(Invariant) + " sn"
            + ", başlangıç " + TurkeyTime.ToLocal(started).ToString("HH:mm", Invariant)
            + ", bitiş " + TurkeyTime.ToLocal(finished).ToString("HH:mm", Invariant)
            + ", " + FormatUsd(video.TotalCostUsd)
            + " · " + video.Title;
        return _logger.InformationAsync(
            ActivitySources.Production,
            text,
            new { videoId = video.Id, jobId = job.Id, version = video.Version, durationSec = video.DurationSec, costUsd = video.TotalCostUsd },
            (long)(finished - started).TotalMilliseconds,
            job.UserId,
            cancellationToken);
    }

    /// <summary>Genel bilgi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task InfoAsync(Guid userId, string message, CancellationToken cancellationToken) =>
        _logger.InformationAsync(ActivitySources.Production, message, null, null, userId, cancellationToken);

    /// <summary>Süreyi Türkçe yazar (ör. "3 dk 12 sn", "850 ms").</summary>
    /// <param name="milliseconds">Süre (ms).</param>
    /// <returns>Metin.</returns>
    public static string FormatDuration(long milliseconds)
    {
        if (milliseconds < 1000)
        {
            return milliseconds.ToString(Invariant) + " ms";
        }

        var span = TimeSpan.FromMilliseconds(milliseconds);
        return span.TotalMinutes >= 1
            ? ((int)span.TotalMinutes).ToString(Invariant) + " dk " + span.Seconds.ToString(Invariant) + " sn"
            : span.Seconds.ToString(Invariant) + " sn";
    }

    /// <summary>Doları sentle yazar (ör. "$0.92").</summary>
    /// <param name="usd">Tutar.</param>
    /// <returns>Metin.</returns>
    public static string FormatUsd(decimal usd) => "$" + Math.Round(usd, 2).ToString("0.00", Invariant);
}
