using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Monitoring.Infrastructure.Logging;

/// <summary>
/// Açılışta ve sonra günde bir 30 günden eski teknik log kayıtlarını siler (spec 0008 Req 6). Çerçeve adaptörü
/// (<see cref="BackgroundService"/>); silme <see cref="ITechnicalLogService"/>'tedir. Veritabanı kapalıysa
/// uyarı yazar ve ertesi turu bekler.
/// </summary>
/// <param name="services">Kök servis sağlayıcı.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
/// <param name="logger">Teknik log.</param>
public sealed partial class TechnicalLogRetentionWorker(IServiceProvider services, TimeProvider timeProvider, ILogger<TechnicalLogRetentionWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            await PurgeOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await WaitAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task PurgeOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var deleted = await scope.ServiceProvider.GetRequiredService<ITechnicalLogService>().PurgeExpiredAsync(stoppingToken).ConfigureAwait(false);
            LogPurged(logger, deleted);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Kapanıyor.
        }
#pragma warning disable CA1031 // Temizlik hatası uygulamayı düşürmemeli (DB kapalı olabilir).
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogPurgeFailed(logger, ex);
        }
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Teknik log temizliği: {Deleted} eski kayıt silindi")]
    private static partial void LogPurged(ILogger logger, int deleted);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning, Message = "Teknik log temizliği yapılamadı; ertesi gün yeniden denenecek")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception);
}
