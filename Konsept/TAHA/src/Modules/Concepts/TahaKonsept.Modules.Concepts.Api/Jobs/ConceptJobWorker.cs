using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Api.Jobs;

/// <summary>
/// Konsept üretim işçisi (<see cref="BackgroundService"/>): veritabanı hazır olunca kuyruktaki işleri sırayla (tek tüketici → kullanıcı
/// başına sıralı) okur, her iş için yeni scope açıp <see cref="IConceptJobProcessor"/> çalıştırır. Kullanıcı iptali iş belirteciyle,
/// sunucu kapanışı durdurma belirteciyle bağlanır; beklenmeyen hata işi "Failed" yapar, işçi durmaz.
/// </summary>
/// <param name="scopes">Scope fabrikası.</param>
/// <param name="queue">İş kuyruğu.</param>
/// <param name="tracker">İş izleyicisi.</param>
/// <param name="database">Veritabanı durumu.</param>
/// <param name="clock">Saat.</param>
/// <param name="logger">Logger.</param>
public sealed class ConceptJobWorker(
    IServiceScopeFactory scopes,
    IConceptJobQueue queue,
    IConceptJobTracker tracker,
    IDatabaseStatus database,
    IClock clock,
    ILogger<ConceptJobWorker> logger) : BackgroundService
{
    /// <summary>İşleri tüketir.</summary>
    /// <param name="stoppingToken">Durdurma belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await database.WaitUntilReadyAsync(stoppingToken);
            await foreach (var job in queue.ReadAllAsync(stoppingToken))
            {
                if (tracker.IsCancelled(job.Id))
                {
                    continue;
                }

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, tracker.GetCancellationToken(job.Id));
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IConceptJobProcessor>();
                    await processor.ProcessAsync(job, linked.Token);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    tracker.Update(job.Id, d => d with { Status = ConceptJobStatus.Cancelled, FinishedAt = clock.UtcNow, Message = "Sunucu durduruldu." });
                    throw;
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Konsept işi beklenmeyen hatayla bitti {JobId}", job.Id);
                    tracker.Update(job.Id, d => d with { Status = ConceptJobStatus.Failed, FinishedAt = clock.UtcNow, CurrentTitle = null, Message = "Beklenmeyen hata: " + ex.Message });
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Konsept işçisi durduruldu.");
        }
    }
}
