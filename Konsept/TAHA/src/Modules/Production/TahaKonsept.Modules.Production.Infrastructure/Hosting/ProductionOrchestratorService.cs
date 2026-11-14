using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Infrastructure.Hosting;

/// <summary>
/// Üretim orkestratörü arka plan servisi: veritabanı hazır olunca döngüye girer; her turda yeni DI kapsamında
/// <see cref="IProductionOrchestrator.TickAsync"/> çağırır, sonra <see cref="IProductionSignal"/> ya da <see cref="Interval"/> kadar bekler
/// (ajan olayları, sıra komutları sinyalle hemen uyandırır). Kullanıcı başına aynı anda tek iş çalışır.
/// </summary>
public sealed class ProductionOrchestratorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IProductionSignal _signal;
    private readonly IDatabaseStatus _database;
    private readonly ILogger<ProductionOrchestratorService> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="scopes">Kapsam fabrikası.</param>
    /// <param name="signal">Uyandırma sinyali.</param>
    /// <param name="database">Veritabanı durumu.</param>
    /// <param name="logger">Günlük.</param>
    public ProductionOrchestratorService(IServiceScopeFactory scopes, IProductionSignal signal, IDatabaseStatus database, ILogger<ProductionOrchestratorService> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Turlar arası azami bekleme.</summary>
    public static TimeSpan Interval { get; } = TimeSpan.FromSeconds(3);

    /// <summary>Döngü.</summary>
    /// <param name="stoppingToken">Durdurma belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _database.WaitUntilReadyAsync(stoppingToken);
        _logger.LogInformation("Üretim orkestratörü başladı");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IProductionOrchestrator>().TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üretim orkestratörü turu başarısız");
            }

            try
            {
                await _signal.WaitAsync(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Üretim orkestratörü durdu");
    }
}
