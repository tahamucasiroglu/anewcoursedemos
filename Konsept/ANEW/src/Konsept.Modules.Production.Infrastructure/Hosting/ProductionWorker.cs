using Konsept.Modules.Production.Application.Abstract;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Production.Infrastructure.Hosting;

/// <summary>
/// Üretim hattının zamanlayıcısı (spec 0017 Req 8, 15): açılışta bir kez, sonra <see cref="Interval"/> arayla
/// <see cref="IProductionRunner.SweepAsync"/>'i çağırır (ör. Ajan bekleniyor işi ajan bağlanınca kendiliğinden sürer). Veritabanı hazır
/// değilse taramayı atlar (arızanın ilk taramasında bir kez Warning). Çerçeve adaptörü; karar koşucudadır.
/// </summary>
/// <param name="runner">Koşucu.</param>
/// <param name="logger">Teknik log.</param>
public sealed partial class ProductionWorker(IProductionRunner runner, ILogger<ProductionWorker> logger) : BackgroundService
{
    /// <summary>Tarama aralığı (sayfa durumu ≤5 sn içinde güncellenir, Req 6).</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startup = true;
        var failing = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await runner.SweepAsync(startup, stoppingToken).ConfigureAwait(false);
                startup = false;
                failing = false;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (!failing)
                {
                    SweepFailed(logger, ex); // Aynı arıza her taramada yeniden loglanmaz.
                }

                failing = true;
            }

            try
            {
                await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(EventId = 5305, Level = LogLevel.Warning, Message = "Üretim hattı taraması yapılamadı; sonraki taramada yeniden denenecek")]
    private static partial void SweepFailed(ILogger logger, Exception exception);
}
