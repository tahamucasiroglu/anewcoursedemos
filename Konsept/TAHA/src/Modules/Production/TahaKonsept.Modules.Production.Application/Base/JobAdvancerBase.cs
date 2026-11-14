using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IJobAdvancer"/> ortak davranışı (orkestratörün kalbi). Kullanıcı başına:
/// <list type="bullet">
/// <item>Etkin iş varsa: sıra duraklatıldıysa işi duraklatır (ajandaki aşamaya <c>PauseJob</c>), değilse bitmeyen ilk çalıştırmayı işler.</item>
/// <item>Sunucu aşaması: hash hesaplanır; kaynak işte aynı hash varsa yeniden kullanılır, yoksa yerinde çalıştırılır ve sonraki aşamaya geçilir
/// (her aşama arasında duraklatma denetlenir — kontrol noktası).</item>
/// <item>Ajan aşaması: ajan çevrimdışıysa iş "Ajan bekleniyor" olur; değilse komut gönderilir ve ajan olayı beklenir. Uzun süre yanıt yoksa yeniden gönderilir.</item>
/// <item>Etkin iş yoksa ve sıra çalışıyorsa: bütçe kontrolünden (<see cref="IBudgetGuard"/>) sonra sıradaki öğe için iş oluşturulur; bütçe aşıldıysa
/// sıra durur (uyarı günlüğü).</item>
/// </list>
/// </summary>
public abstract class JobAdvancerBase : IJobAdvancer
{
    private readonly IProductionQueueRepository _queues;
    private readonly IQueueItemRepository _items;
    private readonly IJobRepository _jobs;
    private readonly IJobFactory _factory;
    private readonly IPipelineContextFactory _contexts;
    private readonly IStageHandlerRegistry _handlers;
    private readonly IStageInputHasher _hasher;
    private readonly IAgentGateway _agent;
    private readonly IBudgetGuard _budget;
    private readonly IJobFinalizer _finalizer;
    private readonly IProductionActivity _activity;
    private readonly IJobCancellationRegistry _cancellations;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="dependencies">Bağımlılıklar.</param>
    protected JobAdvancerBase(JobAdvancerDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        _queues = dependencies.Queues;
        _items = dependencies.Items;
        _jobs = dependencies.Jobs;
        _factory = dependencies.Factory;
        _contexts = dependencies.Contexts;
        _handlers = dependencies.Handlers;
        _hasher = dependencies.Hasher;
        _agent = dependencies.Agent;
        _budget = dependencies.Budget;
        _finalizer = dependencies.Finalizer;
        _activity = dependencies.Activity;
        _cancellations = dependencies.Cancellations;
        _clock = dependencies.Clock;
        _logger = dependencies.Logger;
    }

    /// <summary>Ajan aşaması bu süre ilerleme göndermezse yeniden gönderilir.</summary>
    protected virtual TimeSpan StaleAgentRunAfter => TimeSpan.FromMinutes(10);

    /// <summary>Kullanıcının sırasını bir adım ilerletir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task AdvanceUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var queue = await _queues.GetForUserAsync(userId, cancellationToken);
        var active = await _items.GetActiveAsync(userId, cancellationToken);
        if (active is not null)
        {
            var job = active.JobId is Guid jobId ? await _jobs.GetWithRunsAsync(jobId, cancellationToken) : null;
            if (job is null)
            {
                active.MarkFailed("İş kaydı bulunamadı", _clock.UtcNow);
                await SaveAsync(cancellationToken);
                return;
            }

            if (queue?.RunState == QueueRunState.Paused)
            {
                await PauseAsync(active, job, cancellationToken);
                return;
            }

            if (job.Status == JobStatus.Paused)
            {
                job.Resume();
                active.MarkRunning(job.Id, _clock.UtcNow);
                await SaveAsync(cancellationToken);
            }

            await RunJobAsync(active, job, cancellationToken);
            return;
        }

        if (queue is null || queue.RunState != QueueRunState.Running)
        {
            return;
        }

        var next = await _items.GetNextWaitingAsync(userId, cancellationToken);
        if (next is null)
        {
            queue.Stop("Sıra tamamlandı");
            await SaveAsync(cancellationToken);
            return;
        }

        var budget = await _budget.EnsureCanStartProductionAsync(userId, cancellationToken);
        if (budget.IsFailure)
        {
            queue.Stop(budget.Error.Message, budgetBlocked: true);
            await SaveAsync(cancellationToken);
            _logger.LogWarning("Bütçe aşıldı; yeni üretim başlatılmadı (kullanıcı {UserId}): {Message}", userId, budget.Error.Message);
            await _activity.BudgetStoppedAsync(userId, budget.Error.Message, cancellationToken);
            return;
        }

        var created = await _factory.CreateAsync(next, cancellationToken);
        if (created.IsFailure)
        {
            next.MarkFailed(created.Error.Message, _clock.UtcNow);
            await SaveAsync(cancellationToken);
            await _activity.InfoAsync(userId, "Üretim başlatılamadı: " + next.Title + " — " + created.Error.Message, cancellationToken);
            return;
        }

        var newJob = created.Value;
        next.Rename(newJob.Title);
        next.MarkRunning(newJob.Id, _clock.UtcNow);
        newJob.Start(_clock.UtcNow);
        await SaveAsync(cancellationToken);
        _logger.LogInformation("Üretim başladı: {Title} (iş {JobId}, sürüm {Version})", newJob.Title, newJob.Id, newJob.Version);
        await RunJobAsync(next, newJob, cancellationToken);
    }

    /// <summary>İşi duraklatır (ajandaki aşamaya da duraklat komutu gider).</summary>
    /// <param name="item">Öğe.</param>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task PauseAsync(QueueItem item, Job job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(job);
        if (job.Status != JobStatus.Running && item.Status != QueueItemStatus.Running)
        {
            return;
        }

        job.Pause();
        item.MarkPaused();
        await SaveAsync(cancellationToken);
        if (job.Runs.Any(r => r.Status == StageStatus.Running && r.Location == StageLocation.Agent))
        {
            var sent = await _agent.PauseJobAsync(job.UserId, job.WorkspaceId, cancellationToken);
            if (sent.IsFailure)
            {
                _logger.LogWarning("Ajana duraklatma gönderilemedi (iş {JobId}): {Message}", job.Id, sent.Error.Message);
            }
        }
    }

    /// <summary>
    /// İşin bitmeyen aşamalarını sırayla işler (ajan aşamasında ya da duraklatmada durur). Zincir işe bağlı iptal kapsamında çalışır
    /// (<see cref="IJobCancellationRegistry"/>): sıradan çıkarma sürmekte olan sunucu aşamasını (LLM/TTS/görsel çağrısı dahil) anında keser.
    /// </summary>
    /// <param name="item">Öğe.</param>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task RunJobAsync(QueueItem item, Job job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(job);
        using var cancellation = _cancellations.Begin(job.UserId, item.Id, cancellationToken);
        cancellationToken = cancellation.Token;
        var context = await _contexts.CreateAsync(job, cancellationToken);
        IReadOnlyList<StageRun> baseRuns = job.BaseJobId is Guid baseId ? await _jobs.ListRunsNoTrackingAsync(baseId, cancellationToken) : [];
        for (var guard = 0; guard < 10_000 && !cancellationToken.IsCancellationRequested; guard++)
        {
            if (job.Status != JobStatus.Running)
            {
                return;
            }

            if (await _queues.GetRunStateAsync(job.UserId, cancellationToken) == QueueRunState.Paused)
            {
                await PauseAsync(item, job, cancellationToken);
                return;
            }

            var run = job.OrderedRuns.FirstOrDefault(r => !r.IsDone);
            if (run is null)
            {
                await _finalizer.FinalizeAsync(job, context, cancellationToken);
                return;
            }

            switch (run.Status)
            {
                case StageStatus.Failed:
                case StageStatus.Cancelled:
                    await FailJobAsync(item, job, run, run.ErrorMessage ?? "Aşama başarısız", cancellationToken);
                    return;
                case StageStatus.Running when run.Location == StageLocation.Agent:
                    await WatchAgentRunAsync(item, job, run, cancellationToken);
                    return;
                case StageStatus.Running:
                    run.ReleaseAttempt("Sunucu yeniden başladı; aşama tekrarlanıyor");
                    break;
            }

            var proceed = await StepAsync(item, job, run, context, baseRuns, cancellationToken);
            if (!proceed)
            {
                return;
            }
        }
    }

    /// <summary>Bekleyen tek çalıştırmayı işler.</summary>
    /// <param name="item">Öğe.</param>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="baseRuns">Kaynak işin çalıştırmaları.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonraki aşamaya hemen geçilebiliyorsa <c>true</c>.</returns>
    protected virtual async Task<bool> StepAsync(
        QueueItem item,
        Job job,
        StageRun run,
        PipelineContext context,
        IReadOnlyList<StageRun> baseRuns,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var handler = _handlers.Find(run.Stage);
        if (handler is null)
        {
            run.Fail("production.no_handler", ProductionErrors.NoHandler(run.Stage.ToString()).Message, _clock.UtcNow);
            await FailJobAsync(item, job, run, run.ErrorMessage!, cancellationToken);
            return false;
        }

        var location = handler.ResolveLocation(context);
        var request = new StageExecutionRequest
        {
            SceneIndex = run.SceneIndex,
            InputHash = _hasher.HashInput(run.Stage, run.SceneIndex, handler.BuildHashInput(context, run.SceneIndex)),
            RepairHint = run.RepairHint,
            Attempt = run.Attempts + 1,
        };
        var baseRun = run.ReuseDisabled || run.RepairHint is not null
            ? null
            : baseRuns.FirstOrDefault(r => r.Stage == run.Stage && r.SceneIndex == run.SceneIndex && r.IsDone && r.InputHash == request.InputHash && !string.IsNullOrEmpty(r.OutputHash));

        if (baseRun is not null && location == StageLocation.Server)
        {
            run.ReuseFrom(baseRun, _clock.UtcNow);
            await SaveAsync(cancellationToken);
            await _activity.StageFinishedAsync(job, run, cancellationToken);
            return true;
        }

        return location == StageLocation.Server
            ? await RunServerStageAsync(item, job, run, handler, context, request, cancellationToken)
            : await DispatchAgentStageAsync(job, run, handler, context, request, reuseCandidate: baseRun is not null && job.SharesWorkspace, cancellationToken);
    }

    /// <summary>Sunucu aşamasını yerinde çalıştırır.</summary>
    /// <param name="item">Öğe.</param>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="handler">İşleyici.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    protected virtual async Task<bool> RunServerStageAsync(
        QueueItem item,
        Job job,
        StageRun run,
        IStageHandler handler,
        PipelineContext context,
        StageExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(request);
        run.Start(request.InputHash, StageLocation.Server, _clock.UtcNow);
        job.SetMessage(PipelineStages.DisplayName(run.Stage) + (run.SceneIndex is int i ? " (sahne " + (i + 1) + ")" : string.Empty));
        await SaveAsync(cancellationToken);
        await _activity.StageStartedAsync(job, run, cancellationToken);

        Result<StageExecutionResult> result;
        try
        {
            result = await handler.ExecuteAsync(context, request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Aşama hata verdi: {Stage} (iş {JobId})", run.Stage, job.Id);
            result = Error.Failure("production.stage_exception", "Beklenmeyen hata: " + ex.Message);
        }

        if (result.IsFailure)
        {
            run.AddUsage(request.Usage.CostUsd, request.Usage.InputTokens, request.Usage.OutputTokens);
            run.Fail(result.Error.Code, result.Error.Message, _clock.UtcNow);
            await FailJobAsync(item, job, run, result.Error.Message, cancellationToken);
            return false;
        }

        var output = result.Value;
        run.Complete(
            output.OutputJson,
            _hasher.HashOutput(request.InputHash, output.OutputJson),
            output.Artifacts.Count == 0 ? null : ProductionJson.Write(output.Artifacts),
            output.CostUsd,
            output.InputTokens,
            output.OutputTokens,
            output.Message,
            _clock.UtcNow);
        await SaveAsync(cancellationToken);
        await _activity.StageFinishedAsync(job, run, cancellationToken);
        return true;
    }

    /// <summary>Ajan aşamasını gönderir (ajan çevrimdışıysa iş bekler).</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="handler">İşleyici.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="reuseCandidate">Kaynak işte aynı hash var ve alan paylaşılıyor mu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Her zaman <c>false</c> (ajan olayı beklenir).</returns>
    protected virtual async Task<bool> DispatchAgentStageAsync(
        Job job,
        StageRun run,
        IStageHandler handler,
        PipelineContext context,
        StageExecutionRequest request,
        bool reuseCandidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(handler);
        if (!_agent.IsAgentOnline(job.UserId))
        {
            if (!job.WaitingForAgent)
            {
                job.SetWaitingForAgent(true);
                await SaveAsync(cancellationToken);
                _logger.LogInformation("Ajan çevrimdışı; iş bekliyor (iş {JobId}, aşama {Stage})", job.Id, run.Stage);
            }

            return false;
        }

        var command = await handler.BuildCommandAsync(context, request, cancellationToken);
        if (command.IsFailure)
        {
            run.Fail(command.Error.Code, command.Error.Message, _clock.UtcNow);
            var item = await _items.GetByIdAsync(job.QueueItemId, cancellationToken);
            await FailJobAsync(item, job, run, command.Error.Message, cancellationToken);
            return false;
        }

        job.SetWaitingForAgent(false);
        var first = run.Attempts == 0 && (run.SceneIndex is null or 0);
        run.Dispatch(request.InputHash, command.Value.CommandId, reuseCandidate, _clock.UtcNow);
        job.SetMessage(PipelineStages.DisplayName(run.Stage) + (run.SceneIndex is int i ? " (sahne " + (i + 1) + ")" : string.Empty) + " — masaüstü programında");
        await SaveAsync(cancellationToken);
        if (first)
        {
            await _activity.StageStartedAsync(job, run, cancellationToken);
        }

        var sent = await _agent.SendStageAsync(job.UserId, command.Value, cancellationToken);
        if (sent.IsFailure)
        {
            run.ReleaseAttempt("Masaüstü programına gönderilemedi: " + sent.Error.Message);
            job.SetWaitingForAgent(true);
            await SaveAsync(cancellationToken);
        }

        return false;
    }

    /// <summary>Ajanda çalışan aşamayı izler: ajan çevrimdışıysa bekleme işareti, uzun süre yanıt yoksa yeniden gönderim.</summary>
    /// <param name="item">Öğe.</param>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task WatchAgentRunAsync(QueueItem item, Job job, StageRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        var online = _agent.IsAgentOnline(job.UserId);
        if (!online)
        {
            if (!job.WaitingForAgent)
            {
                job.SetWaitingForAgent(true);
                await SaveAsync(cancellationToken);
            }

            return;
        }

        if (job.WaitingForAgent)
        {
            job.SetWaitingForAgent(false);
            job.SetMessage(PipelineStages.DisplayName(run.Stage) + " — masaüstü programında");
            await SaveAsync(cancellationToken);
        }

        var last = run.LastProgressAtUtc ?? run.DispatchedAtUtc ?? run.StartedAtUtc ?? _clock.UtcNow;
        if (_clock.UtcNow - last < StaleAgentRunAfter)
        {
            return;
        }

        if (run.Attempts >= ProductionLimits.MaxAgentAttempts)
        {
            run.Fail("production.agent_timeout", "Masaüstü programından yanıt alınamadı.", _clock.UtcNow);
            await FailJobAsync(item, job, run, run.ErrorMessage!, cancellationToken);
            return;
        }

        // Eski komut yalnız yavaş olabilir: yenisi aynı çalışma alanında onunla yan yana çalışmasın diye önce durdurulur
        // (PauseJob kontrol noktalarını korur; yeni komut kaldığı yerden sürer). Eski komutun geç gelen sonucu CommandId uyuşmadığı için yok sayılır.
        var stopped = await _agent.PauseJobAsync(job.UserId, job.WorkspaceId, cancellationToken);
        if (stopped.IsFailure)
        {
            _logger.LogWarning("Yanıtsız ajan aşaması durdurulamadı (iş {JobId}): {Message}", job.Id, stopped.Error.Message);
        }

        run.ResetPending("Masaüstü programından yanıt yok; yeniden gönderilecek");
        await SaveAsync(cancellationToken);
    }

    /// <summary>İşi ve öğeyi hata ile bitirir, günlüğe yazar.</summary>
    /// <param name="item">Öğe (silinmişse <c>null</c>).</param>
    /// <param name="job">İş.</param>
    /// <param name="run">Hatalı çalıştırma.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task FailJobAsync(QueueItem? item, Job job, StageRun run, string message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        var text = PipelineStages.DisplayName(run.Stage) + ": " + message;
        job.Fail(text, _clock.UtcNow);
        item?.MarkFailed(text, _clock.UtcNow);
        await SaveAsync(cancellationToken);
        _logger.LogError("Üretim başarısız: {Title} (iş {JobId}) — {Message}", job.Title, job.Id, text);
        await _activity.StageFailedAsync(job, run, cancellationToken);
    }

    /// <summary>Değişiklikleri kaydeder.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected Task SaveAsync(CancellationToken cancellationToken) => _jobs.UnitOfWork.SaveChangesAsync(cancellationToken);
}
