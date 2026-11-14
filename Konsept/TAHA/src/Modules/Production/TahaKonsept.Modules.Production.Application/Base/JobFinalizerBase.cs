using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Concrete;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IJobFinalizer"/> ortak davranışı: sahneleri (plan + Normalize yazılış/okunuş + gerçek süreler), aşama maliyet/zaman dökümünü kurar,
/// <see cref="Video"/> kaydını ekler, işi ve sıra öğesini tamamlar, online artefaktları siler ve işlem günlüğüne "Video hazırlandı" yazar.
/// Sesli videoda gerçek konuşma hızını (yazılış kelimesi / seslendirme süresi) ses modeli + ses için kaydeder (<see cref="ISpeechRateService"/>;
/// Speech aşaması tamamen yeniden kullanıldıysa aynı ses iki kez sayılmasın diye atlanır; hata videoyu bozmaz, yalnız uyarı yazılır).
/// </summary>
public abstract class JobFinalizerBase : IJobFinalizer
{
    private readonly IVideoRepository _videos;
    private readonly IQueueItemRepository _items;
    private readonly IArtifactStore _artifacts;
    private readonly IProductionActivity _activity;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly ISpeechRateService _rates;

    /// <summary>Oluşturur.</summary>
    /// <param name="videos">Video deposu.</param>
    /// <param name="items">Sıra öğeleri deposu.</param>
    /// <param name="artifacts">Artefakt deposu.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlük.</param>
    /// <param name="rates">Konuşma hızı servisi.</param>
    protected JobFinalizerBase(IVideoRepository videos, IQueueItemRepository items, IArtifactStore artifacts, IProductionActivity activity, IClock clock, ILogger logger, ISpeechRateService rates)
    {
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        _videos = videos ?? throw new ArgumentNullException(nameof(videos));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Videoyu oluşturur ve kaydeder.</summary>
    /// <param name="job">İş.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Video.</returns>
    public async Task<Video> FinalizeAsync(Job job, PipelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(context);
        var now = _clock.UtcNow;
        var durations = ComposeStageHandler.Durations(context);
        var compose = context.Outputs.OutputOf<ComposeOutput>(PipelineStage.Compose);
        var normalized = context.Outputs.OutputOf<NormalizeOutput>(PipelineStage.Normalize);
        var scenes = new List<VideoSceneDto>();
        var start = 0d;
        foreach (var scene in context.Outputs.ScenePlan.Scenes)
        {
            var segment = normalized?.Segments.FirstOrDefault(s => s.SceneIndex == scene.Index);
            var duration = scene.Index < durations.Count ? durations[scene.Index] : scene.DurationSec;
            scenes.Add(new VideoSceneDto
            {
                Index = scene.Index,
                Title = scene.Title,
                OnScreen = scene.OnScreen,
                DisplayText = segment?.DisplayText ?? scene.Narration,
                SpokenText = segment?.SpokenText ?? scene.Narration,
                StartSec = Math.Round(start, 3),
                DurationSec = duration,
            });
            start += duration;
        }

        var breakdown = PipelineStages.Ordered
            .Select(stage => job.Runs.Where(r => r.Stage == stage).ToList())
            .Where(runs => runs.Count > 0)
            .Select(runs => new StageCostDto
            {
                Stage = runs[0].Stage,
                CostUsd = runs.Sum(r => r.CostUsd),
                DurationMs = runs.Sum(r => r.DurationMs),
                InputTokens = runs.Sum(r => r.InputTokens),
                OutputTokens = runs.Sum(r => r.OutputTokens),
                Reused = runs.All(r => r.Reused),
                StartedAt = runs.Min(r => r.StartedAtUtc),
                FinishedAt = runs.Max(r => r.FinishedAtUtc),
            })
            .ToList();

        var video = Video.FromJob(job, compose?.DurationSec ?? start, ProductionJson.Write(breakdown), ProductionJson.Write(scenes), now);
        await _videos.AddAsync(video, cancellationToken);
        job.Complete(now);
        var item = await _items.GetByIdAsync(job.QueueItemId, cancellationToken);
        item?.MarkDone(video.Id, now);
        await _videos.UnitOfWork.SaveChangesAsync(cancellationToken);

        var cleanup = await _artifacts.DeleteJobArtifactsAsync(job.UserId, job.Id, cancellationToken);
        if (cleanup.IsFailure)
        {
            _logger.LogWarning("Online artefaktlar silinemedi (iş {JobId}): {Message}", job.Id, cleanup.Error.Message);
        }

        _logger.LogInformation("Video hazırlandı: {Title} (video {VideoId}, {Duration} sn, ${Cost})", job.Title, video.Id, video.DurationSec, video.TotalCostUsd);
        await _activity.VideoReadyAsync(job, video, cancellationToken);
        await RecordSpeechRateAsync(job, context, cancellationToken);
        return video;
    }

    /// <summary>Gerçek konuşma hızını kaydeder (kendini ayarlama).</summary>
    /// <param name="job">İş.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task RecordSpeechRateAsync(Job job, PipelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(context);
        var speech = context.Outputs.OutputOf<SpeechOutput>(PipelineStage.Speech);
        var speechRuns = job.Runs.Where(r => r.Stage == PipelineStage.Speech).ToList();
        if (speech is null || speech.Silent || context.Settings.SpeechMode == MediaMode.Off || (speechRuns.Count > 0 && speechRuns.All(r => r.Reused)))
        {
            return;
        }

        var seconds = speech.TotalDurationSec > 0 ? speech.TotalDurationSec : speech.Scenes.Sum(s => s.DurationSec);
        var words = context.Outputs.ScenePlan.Scenes.Sum(s => NarrationWords.Count(s.Narration));
        try
        {
            var average = await _rates.RecordAsync(job.UserId, context.Settings, context.Language, words, seconds, cancellationToken);
            if (average is { } avg)
            {
                _logger.LogInformation(
                    "Konuşma hızı ölçüldü: {Model}/{Voice} {Words} kelime / {Seconds} sn → ortalama {Average} kelime/sn (1.0×)",
                    context.Settings.SpeechModelKey,
                    context.Settings.VoiceId ?? "varsayılan",
                    words,
                    Math.Round(seconds, 2),
                    avg);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Konuşma hızı ölçümü kaydedilemedi (iş {JobId})", job.Id);
        }
    }
}
