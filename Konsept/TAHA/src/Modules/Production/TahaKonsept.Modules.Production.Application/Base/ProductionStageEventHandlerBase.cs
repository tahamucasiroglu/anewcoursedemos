using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// Agents modülünün <c>IAgentStageEventHandler</c> uzatma noktasının Production uygulaması (Agents'taki no-op varsayılanın yerine kaydedilir).
/// Çalıştırma ajan komut kimliğiyle (<c>CommandId</c>) bulunur ve sahip kullanıcı doğrulanır; eski/bilinmeyen raporlar yok sayılır.
/// <list type="bullet">
/// <item>İlerleme: yüzde/mesaj kaydedilir (kilit meşgulse atlanır; ilerleme geçicidir).</item>
/// <item>Tamamlandı: işleyici sonucu çıktıya çevirir, çıktı hash'i yazılır; Mux ise video kaydedilir. Orkestratör uyandırılır.</item>
/// <item>Hata: duraklatma/iptal → aşama beklemeye döner; sahne lint hatası → sahne kodu hata ile yeniden yazdırılır; eski artefakt indirilemedi →
/// yeniden kullanım kapatılıp ilgili aşamalar yeniden çalışır; geçici hata → (deneme sınırına kadar) yeniden gönderim; aksi halde iş başarısız.</item>
/// </list>
/// </summary>
public abstract class ProductionStageEventHandlerBase : AgentStageEventHandlerBase
{
    /// <summary>Ajanın duraklatma kodu.</summary>
    public const string PausedCode = "agent.paused";

    /// <summary>Ajanın sahne doğrulama hatası kodu.</summary>
    public const string SceneInvalidCode = "agent.scene_invalid";

    private static readonly string[] MissingInputCodes = ["agent.download_failed", "agent.input_missing"];

    private readonly IJobRepository _jobs;
    private readonly IQueueItemRepository _items;
    private readonly IPipelineContextFactory _contexts;
    private readonly IStageHandlerRegistry _handlers;
    private readonly IStageInputHasher _hasher;
    private readonly IJobFinalizer _finalizer;
    private readonly IUserJobLock _locks;
    private readonly IProductionSignal _signal;
    private readonly IProductionActivity _activity;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="dependencies">Bağımlılıklar.</param>
    protected ProductionStageEventHandlerBase(StageEventHandlerDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        _jobs = dependencies.Jobs;
        _items = dependencies.Items;
        _contexts = dependencies.Contexts;
        _handlers = dependencies.Handlers;
        _hasher = dependencies.Hasher;
        _finalizer = dependencies.Finalizer;
        _locks = dependencies.Locks;
        _signal = dependencies.Signal;
        _activity = dependencies.Activity;
        _clock = dependencies.Clock;
        _logger = dependencies.Logger;
    }

    /// <summary>Aşama ilerlemesi.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override async Task OnStageProgressAsync(AgentCallContext agent, StageProgressReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(report);
        using var held = _locks.TryAcquire(agent.UserId);
        if (held is null)
        {
            return;
        }

        var (job, run) = await FindAsync(agent, report.CommandId, cancellationToken);
        if (job is null || run is null)
        {
            return;
        }

        run.ReportProgress(report.Percent, report.Message, _clock.UtcNow);
        if (!string.IsNullOrWhiteSpace(report.Message))
        {
            job.SetMessage(PipelineStages.DisplayName(run.Stage) + ": " + report.Message);
        }

        await _jobs.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Aşama tamamlandı.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override async Task OnStageCompletedAsync(AgentCallContext agent, StageResultReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(report);
        using (await _locks.AcquireAsync(agent.UserId, cancellationToken))
        {
            var (job, run) = await FindAsync(agent, report.CommandId, cancellationToken);
            if (job is null || run is null)
            {
                return;
            }

            var handler = _handlers.Find(run.Stage);
            var context = await _contexts.CreateAsync(job, cancellationToken);
            var request = new StageExecutionRequest { SceneIndex = run.SceneIndex, InputHash = run.InputHash, RepairHint = run.RepairHint, Attempt = run.Attempts };
            var applied = handler is null
                ? ProductionErrors.NoHandler(run.Stage.ToString())
                : handler.ApplyAgentResult(context, request, report);
            if (applied.IsFailure)
            {
                run.Fail(applied.Error.Code, applied.Error.Message, _clock.UtcNow);
                await FailAsync(job, run, applied.Error.Message, cancellationToken);
                return;
            }

            var output = applied.Value;
            run.Complete(
                output.OutputJson,
                _hasher.HashOutput(run.InputHash, output.OutputJson),
                output.Artifacts.Count == 0 ? null : ProductionJson.Write(output.Artifacts),
                output.CostUsd,
                output.InputTokens,
                output.OutputTokens,
                output.Message,
                _clock.UtcNow,
                report.DurationMs > 0 ? report.DurationMs : null);
            await _jobs.UnitOfWork.SaveChangesAsync(cancellationToken);
            await _activity.StageFinishedAsync(job, run, cancellationToken);

            if (run.Stage == PipelineStage.Mux && job.Runs.All(r => r.IsDone))
            {
                await _finalizer.FinalizeAsync(job, context, cancellationToken);
            }
        }

        _signal.Notify();
    }

    /// <summary>Aşama başarısız.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override async Task OnStageFailedAsync(AgentCallContext agent, StageFailureReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(report);
        using (await _locks.AcquireAsync(agent.UserId, cancellationToken))
        {
            var (job, run) = await FindAsync(agent, report.CommandId, cancellationToken);
            if (job is null || run is null)
            {
                return;
            }

            await HandleFailureAsync(job, run, report, cancellationToken);
            await _jobs.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        _signal.Notify();
    }

    /// <summary>Hata raporunu yorumlar (kaydetmez).</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task HandleFailureAsync(Job job, StageRun run, StageFailureReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(report);
        var message = string.IsNullOrWhiteSpace(report.Message) ? "Masaüstü programında hata" : report.Message;
        if (report.Cancelled || report.ErrorCode == PausedCode)
        {
            run.ResetPending("Duraklatıldı; devam edince kaldığı yerden sürer");
            return;
        }

        if (run.Stage == PipelineStage.SceneRender && report.ErrorCode == SceneInvalidCode)
        {
            var codeRun = job.FindRun(PipelineStage.SceneCode, run.SceneIndex);
            if (codeRun is not null && codeRun.RepairCount < ProductionLimits.MaxSceneRepairAttempts)
            {
                codeRun.RequestRepair(string.IsNullOrWhiteSpace(report.Details) ? message : report.Details);
                run.ResetPending("Sahne kodu doğrulama hatasıyla yeniden yazılıyor");
                _logger.LogWarning("Sahne kodu ajan doğrulamasından geçmedi; yeniden yazdırılıyor (iş {JobId}, sahne {Scene})", job.Id, run.SceneIndex);
                return;
            }
        }

        if (MissingInputCodes.Contains(report.ErrorCode) && TryDisableStaleReuse(job))
        {
            _logger.LogWarning("Önceki sürümün dosyaları bulunamadı; yeniden kullanılan aşamalar yeniden çalışacak (iş {JobId})", job.Id);
            return;
        }

        if (report.Retryable && run.Attempts < ProductionLimits.MaxAgentAttempts)
        {
            run.ResetPending("Tekrar denenecek: " + message);
            return;
        }

        run.Fail(string.IsNullOrWhiteSpace(report.ErrorCode) ? "agent.failed" : report.ErrorCode, message, _clock.UtcNow);
        await FailAsync(job, run, message, cancellationToken);
    }

    /// <summary>
    /// Online artefaktlı yeniden kullanılmış aşamaları (ve sonrasındaki tüm aşamaları) yeniden çalışacak şekilde açar.
    /// Daha önce yapıldıysa (yeniden kullanım zaten kapalıysa) <c>false</c>.
    /// </summary>
    /// <param name="job">İş.</param>
    /// <returns>Bir şey değiştiyse <c>true</c>.</returns>
    protected virtual bool TryDisableStaleReuse(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var stale = job.Runs.Where(r => r.Reused && !string.IsNullOrEmpty(r.ArtifactsJson)).ToList();
        if (stale.Count == 0)
        {
            return false;
        }

        var from = stale.Min(r => r.Sequence);
        foreach (var run in job.Runs.Where(r => r.Sequence >= from && r.Status is StageStatus.Completed or StageStatus.Skipped or StageStatus.Running))
        {
            if (run.Reused)
            {
                run.DisableReuse("Önceki sürümün dosyası yok; yeniden üretilecek");
            }
            else
            {
                run.ResetPending("Yeniden çalışacak");
            }
        }

        var failing = job.Runs.Where(r => r.Status == StageStatus.Running);
        foreach (var run in failing)
        {
            run.ResetPending("Yeniden çalışacak");
        }

        return true;
    }

    /// <summary>İşi ve öğeyi hata ile bitirir (kaydeder).</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task FailAsync(Job job, StageRun run, string message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(run);
        var text = PipelineStages.DisplayName(run.Stage) + ": " + message;
        job.Fail(text, _clock.UtcNow);
        var item = await _items.GetByIdAsync(job.QueueItemId, cancellationToken);
        item?.MarkFailed(text, _clock.UtcNow);
        await _jobs.UnitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogError("Üretim başarısız: {Title} (iş {JobId}) — {Message}", job.Title, job.Id, text);
        await _activity.StageFailedAsync(job, run, cancellationToken);
    }

    private async Task<(Job? Job, StageRun? Run)> FindAsync(AgentCallContext agent, Guid commandId, CancellationToken cancellationToken)
    {
        if (commandId == Guid.Empty)
        {
            return (null, null);
        }

        var job = await _jobs.FindByCommandAsync(commandId, cancellationToken);
        if (job is null || job.UserId != agent.UserId || job.Status is not (JobStatus.Running or JobStatus.Paused))
        {
            return (null, null);
        }

        var run = job.Runs.FirstOrDefault(r => r.CommandId == commandId && r.Status == StageStatus.Running);
        return (run is null ? null : job, run);
    }
}
