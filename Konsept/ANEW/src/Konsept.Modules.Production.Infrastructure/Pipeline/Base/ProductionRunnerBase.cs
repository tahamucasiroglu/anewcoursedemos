using System.Collections.Concurrent;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Production.Application.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Production.Infrastructure.Pipeline.Base;

/// <summary>
/// Kullanıcı başına üretim döngüsü (spec 0017 Req 5, 8): istekten bağımsız görevde, her koşu kendi DI kapsamında <see cref="IProductionPipeline"/>
/// çağırır; yeni iş başladıkça sürer. Kullanıcı başına aynı anda tek döngü (yerel GPU işleri de böylece sıralıdır). Durdur, kullanıcının o
/// anki belirtecini iptal eder (yalnızca yerel aşama kesilir). Hat kapalıysa (<c>Production:Pipeline:Enabled=false</c>) hiçbir şey başlatmaz.
/// Tekil (singleton) kaydedilir.
/// </summary>
/// <param name="scopes">Kapsam fabrikası.</param>
/// <param name="lifetime">Uygulama yaşam döngüsü.</param>
/// <param name="enabled">Hat açık mı.</param>
/// <param name="logger">Teknik log.</param>
public abstract class ProductionRunnerBase(IServiceScopeFactory scopes, IHostApplicationLifetime lifetime, bool enabled, ILogger logger) : IProductionRunner
{
    private static readonly Action<ILogger, Guid, Exception?> LoopCrashed = LoggerMessage.Define<Guid>(
        LogLevel.Error, new EventId(5304, "ProductionLoopCrashed"), "Üretim döngüsü beklenmeyen hatayla durdu: {UserId}");

    private readonly ConcurrentDictionary<Guid, RunnerEntry> _entries = new();

    /// <inheritdoc />
    public void Launch(Guid userId, ModelCallerIdentity caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (!enabled)
        {
            return;
        }

        var entry = _entries.GetOrAdd(userId, _ => new RunnerEntry(caller));
        lock (entry)
        {
            entry.Caller = caller;
            if (entry.Stop.IsCancellationRequested)
            {
                entry.Stop.Dispose();
                entry.Stop = new CancellationTokenSource();
            }
        }

        Ensure(userId, entry);
    }

    /// <inheritdoc />
    public bool Nudge(Guid userId)
    {
        if (!enabled || !_entries.TryGetValue(userId, out var entry))
        {
            return false;
        }

        lock (entry)
        {
            if (entry.Stop.IsCancellationRequested)
            {
                entry.Stop.Dispose();
                entry.Stop = new CancellationTokenSource();
            }
        }

        Ensure(userId, entry);
        return true;
    }

    /// <inheritdoc />
    public void Cancel(Guid userId)
    {
        if (_entries.TryGetValue(userId, out var entry))
        {
            lock (entry)
            {
                entry.Stop.Cancel();
            }
        }
    }

    /// <inheritdoc />
    public bool IsBusy(Guid userId) => _entries.TryGetValue(userId, out var entry) && entry.Running;

    /// <inheritdoc />
    public async Task SweepAsync(bool startup, CancellationToken cancellationToken)
    {
        if (!enabled || (!startup && _entries.IsEmpty))
        {
            return; // Bu süreçte hiç iş başlatılmadıysa taranacak bir şey yok.
        }

        await using var scope = scopes.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IProductionJobRepository>();
        var running = await jobs.ListRunningAsync(cancellationToken).ConfigureAwait(false);
        foreach (var userId in running.Select(j => j.UserId).Distinct())
        {
            if (_entries.ContainsKey(userId))
            {
                Nudge(userId);
            }
            else if (startup)
            {
                await jobs.StopAsync(userId, null, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void Ensure(Guid userId, RunnerEntry entry)
    {
        entry.MarkPending();
        if (entry.TryStart())
        {
            _ = Task.Run(() => LoopAsync(userId, entry), CancellationToken.None);
        }
    }

    private async Task LoopAsync(Guid userId, RunnerEntry entry)
    {
        try
        {
            while (entry.TakePending() && !lifetime.ApplicationStopping.IsCancellationRequested)
            {
                bool more;
                do
                {
                    ModelCallerIdentity caller;
                    CancellationToken stop;
                    lock (entry)
                    {
                        caller = entry.Caller;
                        stop = entry.Stop.Token;
                    }

                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(stop, lifetime.ApplicationStopping);
                    await using var scope = scopes.CreateAsyncScope();
                    more = await scope.ServiceProvider.GetRequiredService<IProductionPipeline>().RunAsync(userId, caller, linked.Token).ConfigureAwait(false);
                }
                while (more && !lifetime.ApplicationStopping.IsCancellationRequested);
            }
        }
        catch (Exception ex)
        {
            LoopCrashed(logger, userId, ex);
        }
        finally
        {
            entry.Finish();
            if (entry.HasPending() && !lifetime.ApplicationStopping.IsCancellationRequested)
            {
                Ensure(userId, entry);
            }
        }
    }
}
