using Konsept.Modules.Identity.Application.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Infrastructure.Hosting;

/// <summary>
/// API açılışında rolleri <c>Identity:AdminEmail</c> ayarına eşitler (spec 0011 Req 5, plan Risk 2–3): arka planda çalışır,
/// Api açılışını engellemez; veritabanı hazır değilse <see cref="RetryDelay"/> arayla yeniden dener (Warning). Açılış
/// migration'ı kapalı olsa da çalışır. Çerçeve adaptörü; karar <see cref="IAdminRoleSync"/>'tedir.
/// </summary>
/// <param name="scopes">Kapsam fabrikası (senkron kapsamlı servisleri kullanır).</param>
/// <param name="logger">Teknik log.</param>
public sealed partial class AdminRoleSyncHostedService(IServiceScopeFactory scopes, ILogger<AdminRoleSyncHostedService> logger) : BackgroundService
{
    /// <summary>Başarısız denemeden sonra bekleme.</summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IAdminRoleSync>().SyncAsync(stoppingToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                LogSyncFailed(logger, RetryDelay.TotalSeconds, ex);
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(EventId = 2102, Level = LogLevel.Warning, Message = "Admin rol senkronu yapılamadı; {RetrySeconds} sn sonra yeniden denenecek")]
    private static partial void LogSyncFailed(ILogger logger, double retrySeconds, Exception exception);
}
