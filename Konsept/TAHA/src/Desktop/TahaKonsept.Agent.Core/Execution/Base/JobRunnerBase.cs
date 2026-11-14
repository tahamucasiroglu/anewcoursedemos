using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Http;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Execution.Base;

/// <summary>
/// İş çalıştırıcı tabanı: sınırsız kanal + <c>maxConcurrency</c> işçi (ilk komutta başlar). Her komut için: iş denetimi
/// (<see cref="JobControl"/>) + kapanış belirteci bağlanır, girdiler <c>inputs/</c>'a indirilir (var olanlar atlanır), dağıtıcı çalışır,
/// sonuç raporlanır. İlerleme raporları 500 ms'de bir seyreltilir. Durdurma nedenleri <see cref="JobStopReason"/> ile raporlanır.
/// </summary>
public abstract class JobRunnerBase : IJobRunner
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(500);

    private readonly IStageExecutorDispatcher _dispatcher;
    private readonly IAgentReporter _reporter;
    private readonly IAgentApiClient _api;
    private readonly IAgentPaths _paths;
    private readonly TimeProvider _time;
    private readonly int _maxConcurrency;
    private readonly ConcurrentDictionary<Guid, JobControl> _jobs = new();
    private readonly ConcurrentDictionary<Guid, RunningStageInfo> _active = new();
    private readonly ConcurrentDictionary<Guid, long> _lastProgressTicks = new();
    private readonly object _gate = new();
    private Channel<QueuedStage> _queue = CreateQueue();
    private CancellationTokenSource _shutdown = new();
    private List<Task> _workers = [];
    private int _queued;

    /// <summary>Oluşturur.</summary>
    /// <param name="dispatcher">Aşama dağıtıcısı.</param>
    /// <param name="reporter">Raporlayıcı (hub).</param>
    /// <param name="api">API istemcisi (artefakt indirme).</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="time">Zaman sağlayıcı.</param>
    /// <param name="maxConcurrency">Aynı anda çalışan en fazla aşama.</param>
    protected JobRunnerBase(IStageExecutorDispatcher dispatcher, IAgentReporter reporter, IAgentApiClient api, IAgentPaths paths, TimeProvider time, int maxConcurrency)
    {
        _dispatcher = dispatcher;
        _reporter = reporter;
        _api = api;
        _paths = paths;
        _time = time;
        _maxConcurrency = Math.Max(1, maxConcurrency);
    }

    /// <inheritdoc />
    public event EventHandler<RunningStageInfo>? StageUpdated;

    /// <inheritdoc />
    public int QueuedCount => Volatile.Read(ref _queued);

    /// <inheritdoc />
    public void Submit(StageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Channel<QueuedStage> queue;
        lock (_gate)
        {
            EnsureWorkers();
            queue = _queue;
        }

        var control = _jobs.AddOrUpdate(command.JobId, _ => new JobControl(), (_, existing) => existing.IsStopped ? new JobControl() : existing);
        Interlocked.Increment(ref _queued);
        queue.Writer.TryWrite(new QueuedStage(command, control));
        OnSubmitted(command);
    }

    /// <inheritdoc />
    public void CancelJob(Guid jobId)
    {
        var control = _jobs.GetOrAdd(jobId, _ => new JobControl());
        control.Stop(JobStopReason.Cancelled);
        if (control.RunningCount == 0)
        {
            DeleteJobWorkspace(jobId);
        }
    }

    /// <inheritdoc />
    public void PauseJob(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var control))
        {
            control.Stop(JobStopReason.Paused);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RunningStageInfo> GetActiveStages() => _active.Values.OrderBy(a => a.StartedAtUtc).ToList();

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        Channel<QueuedStage> queue;
        CancellationTokenSource shutdown;
        List<Task> workers;
        lock (_gate)
        {
            queue = _queue;
            shutdown = _shutdown;
            workers = _workers;
            _queue = CreateQueue();
            _shutdown = new CancellationTokenSource();
            _workers = [];
        }

        queue.Writer.TryComplete();
        await shutdown.CancelAsync();
        try
        {
            await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(15), ct);
        }
        catch (TimeoutException)
        {
            OnStopTimedOut();
        }

        while (queue.Reader.TryRead(out var item))
        {
            Interlocked.Decrement(ref _queued);
            await ReportStoppedAsync(item.Command, JobStopReason.Shutdown, 0);
        }

        shutdown.Dispose();
    }

    /// <summary>Komut kuyruğa eklenince çağrılır.</summary>
    /// <param name="command">Komut.</param>
    protected virtual void OnSubmitted(StageCommand command)
    {
    }

    /// <summary>Aşama beklenmeyen bir istisnayla çöktüğünde çağrılır.</summary>
    /// <param name="command">Komut.</param>
    /// <param name="exception">İstisna.</param>
    protected virtual void OnStageCrashed(StageCommand command, Exception exception)
    {
    }

    /// <summary>Aşama bittiğinde çağrılır (başarı/başarısızlık/durma).</summary>
    /// <param name="command">Komut.</param>
    /// <param name="outcome">Kısa sonuç metni.</param>
    /// <param name="elapsed">Süre.</param>
    protected virtual void OnStageFinished(StageCommand command, string outcome, TimeSpan elapsed)
    {
    }

    /// <summary>Kapanışta işçiler zamanında durmazsa çağrılır.</summary>
    protected virtual void OnStopTimedOut()
    {
    }

    private void EnsureWorkers()
    {
        if (_workers.Count > 0)
        {
            return;
        }

        var token = _shutdown.Token;
        var reader = _queue.Reader;
        for (var i = 0; i < _maxConcurrency; i++)
        {
            _workers.Add(Task.Run(() => WorkerAsync(reader, token), CancellationToken.None));
        }
    }

    private async Task WorkerAsync(ChannelReader<QueuedStage> reader, CancellationToken shutdown)
    {
        try
        {
            while (await reader.WaitToReadAsync(shutdown))
            {
                while (!shutdown.IsCancellationRequested && reader.TryRead(out var item))
                {
                    Interlocked.Decrement(ref _queued);
                    await RunItemAsync(item, shutdown);
                }
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            // Kapanış.
        }
    }

    private async Task RunItemAsync(QueuedStage item, CancellationToken shutdown)
    {
        var command = item.Command;
        var control = item.Control;
        if (control.IsStopped)
        {
            await ReportStoppedAsync(command, control.Reason, 0);
            return;
        }

        control.EnterRunning();
        var stopwatch = Stopwatch.StartNew();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(control.Token, shutdown);
        var ct = linked.Token;
        UpdateActive(command, 0, "Başlıyor…", force: true);
        try
        {
            var jobDir = _paths.GetJobDir(command.JobId);
            var inputs = await DownloadInputsAsync(command, jobDir, ct);
            if (inputs.IsFailure)
            {
                await _reporter.ReportFailedAsync(ToFailure(command, inputs.Error), CancellationToken.None);
                OnStageFinished(command, inputs.Error.Code, stopwatch.Elapsed);
                return;
            }

            var context = new StageExecutionContext(command, jobDir, inputs.Value, (p, m) => UpdateActive(command, p, m, force: false));
            var result = await _dispatcher.DispatchAsync(context, ct);
            if (result.IsSuccess)
            {
                await _reporter.ReportCompletedAsync(
                    new StageResultReport
                    {
                        CommandId = command.CommandId,
                        JobId = command.JobId,
                        Stage = command.Stage,
                        SceneIndex = command.SceneIndex,
                        OutputJson = result.Value.OutputJson,
                        Artifacts = result.Value.Artifacts,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        CostUsd = 0m,
                    },
                    CancellationToken.None);
                OnStageFinished(command, "ok", stopwatch.Elapsed);
            }
            else
            {
                await _reporter.ReportFailedAsync(ToFailure(command, result.Error), CancellationToken.None);
                OnStageFinished(command, result.Error.Code, stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var reason = control.IsStopped ? control.Reason : JobStopReason.Shutdown;
            await ReportStoppedAsync(command, reason, stopwatch.ElapsedMilliseconds);
            OnStageFinished(command, reason.ToString(), stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            OnStageCrashed(command, ex);
            await _reporter.ReportFailedAsync(
                new StageFailureReport
                {
                    CommandId = command.CommandId,
                    JobId = command.JobId,
                    Stage = command.Stage,
                    SceneIndex = command.SceneIndex,
                    ErrorCode = "agent.unexpected",
                    Message = "Ajanda beklenmeyen hata: " + ex.Message,
                    Details = ex.ToString(),
                    Retryable = true,
                },
                CancellationToken.None);
        }
        finally
        {
            _active.TryRemove(command.CommandId, out _);
            _lastProgressTicks.TryRemove(command.CommandId, out _);
            var remaining = control.ExitRunning();
            if (control.Reason == JobStopReason.Cancelled && remaining == 0)
            {
                DeleteJobWorkspace(command.JobId);
            }
        }
    }

    private async Task<Result<IReadOnlyDictionary<string, string>>> DownloadInputsAsync(StageCommand command, string jobDir, CancellationToken ct)
    {
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (command.Artifacts.Count == 0)
        {
            return inputs;
        }

        var directory = Path.Combine(jobDir, WorkspaceLayout.InputsDir);
        Directory.CreateDirectory(directory);
        for (var i = 0; i < command.Artifacts.Count; i++)
        {
            var artifact = command.Artifacts[i];
            var name = FileNameSanitizer.Sanitize(artifact.Name, artifact.ArtifactId.ToString("N"));
            var path = Path.Combine(directory, name);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                var url = string.IsNullOrWhiteSpace(artifact.Url) ? AgentApiRoutes.Artifact(artifact.ArtifactId) : artifact.Url;
                var index = i;
                var progress = new InlineProgress<double>(p =>
                    UpdateActive(command, 0, "Girdiler indiriliyor (" + (index + 1) + "/" + command.Artifacts.Count + ") %" + Math.Round(p), force: false));
                var downloaded = await _api.DownloadFileAsync(url, path, progress, ct);
                if (downloaded.IsFailure)
                {
                    return Error.Failure("agent.download_failed", "Girdi indirilemedi (" + name + "): " + downloaded.Error.Message);
                }
            }

            inputs[name] = path;
        }

        return inputs;
    }

    private void UpdateActive(StageCommand command, double percent, string message, bool force)
    {
        var info = _active.AddOrUpdate(
            command.CommandId,
            _ => new RunningStageInfo(command.CommandId, command.JobId, command.Stage, command.SceneIndex, percent, message, _time.GetUtcNow()),
            (_, existing) => existing with { Percent = percent, Message = message });
        StageUpdated?.Invoke(this, info);

        var now = _time.GetTimestamp();
        var last = _lastProgressTicks.GetOrAdd(command.CommandId, 0);
        if (!force && percent < 100 && last != 0 && _time.GetElapsedTime(last, now) < ProgressInterval)
        {
            return;
        }

        _lastProgressTicks[command.CommandId] = now;
        _ = SendProgressSafeAsync(new StageProgressReport
        {
            CommandId = command.CommandId,
            JobId = command.JobId,
            Stage = command.Stage,
            SceneIndex = command.SceneIndex,
            Percent = Math.Round(percent, 1),
            Message = message,
        });
    }

    private async Task SendProgressSafeAsync(StageProgressReport report)
    {
        try
        {
            await _reporter.ReportProgressAsync(report, CancellationToken.None);
        }
        catch (Exception)
        {
            // İlerleme raporu kaybı kritik değil.
        }
    }

    private Task ReportStoppedAsync(StageCommand command, JobStopReason reason, long elapsedMs)
    {
        var (code, message, retryable) = reason switch
        {
            JobStopReason.Paused => ("agent.paused", "Duraklatıldı; devam edince kaldığı yerden sürer.", true),
            JobStopReason.Shutdown => ("agent.shutdown", "Masaüstü programı kapatıldı.", true),
            _ => ("agent.cancelled", "İptal edildi.", false),
        };
        return _reporter.ReportFailedAsync(
            new StageFailureReport
            {
                CommandId = command.CommandId,
                JobId = command.JobId,
                Stage = command.Stage,
                SceneIndex = command.SceneIndex,
                ErrorCode = code,
                Message = message,
                Details = elapsedMs > 0 ? "Süre: " + elapsedMs + " ms" : null,
                Retryable = retryable,
                Cancelled = true,
            },
            CancellationToken.None);
    }

    private static StageFailureReport ToFailure(StageCommand command, Error error) => new()
    {
        CommandId = command.CommandId,
        JobId = command.JobId,
        Stage = command.Stage,
        SceneIndex = command.SceneIndex,
        ErrorCode = error.Code,
        Message = error.Message,
        Details = error.Details is null ? null : string.Join("; ", error.Details.Select(d => d.Key + ": " + string.Join(", ", d.Value))),
        Retryable = error.Type == ErrorType.Failure,
    };

    private void DeleteJobWorkspace(Guid jobId)
    {
        var dir = Path.Combine(_paths.WorkspaceDir, jobId.ToString("N"));
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Kilitli dosya; bir sonraki iptal/silmede tekrar denenir.
        }
    }

    private static Channel<QueuedStage> CreateQueue() =>
        Channel.CreateUnbounded<QueuedStage>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
}
