using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Infrastructure.Hosting;

/// <summary>
/// Üretim orkestratörü arka plan servisi: veritabanı hazır olunca döngüye girer. Her turda ilerletilecek kullanıcıları bulur
/// (<see cref="IProductionOrchestrator.ListUsersAsync"/>) ve her kullanıcıyı kendi DI kapsamında, ayrı bir görevde ilerletir
/// (<see cref="IProductionOrchestrator.AdvanceAsync"/>); kullanıcının önceki görevi sürüyorsa yenisi başlatılmaz. Böylece bir kullanıcının
/// uzun sunucu zinciri (ör. 40 sahnelik kod üretimi) diğer kullanıcıların sırasını bekletmez. Turlar arasında <see cref="IProductionSignal"/>
/// ya da <see cref="Interval"/> kadar beklenir. Kullanıcı başına aynı anda tek iş çalışır.
/// </summary>
public sealed class ProductionOrchestratorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IProductionSignal _signal;
    private readonly IDatabaseStatus _database;
    private readonly ILogger<ProductionOrchestratorService> _logger;
    private readonly Dictionary<Guid, Task> _running = [];

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
                await StartUserWorkersAsync(stoppingToken);
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

        await DrainAsync();
        _logger.LogInformation("Üretim orkestratörü durdu");
    }

    private async Task StartUserWorkersAsync(CancellationToken stoppingToken)
    {
        foreach (var done in _running.Where(p => p.Value.IsCompleted).Select(p => p.Key).ToList())
        {
            _running.Remove(done);
        }

        IReadOnlyList<Guid> users;
        await using (var scope = _scopes.CreateAsyncScope())
        {
            users = await scope.ServiceProvider.GetRequiredService<IProductionOrchestrator>().ListUsersAsync(stoppingToken);
        }

        foreach (var userId in users.Where(u => !_running.ContainsKey(u)))
        {
            _running[userId] = Task.Run(() => AdvanceUserAsync(userId, stoppingToken), CancellationToken.None);
        }
    }

    private async Task AdvanceUserAsync(Guid userId, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IProductionOrchestrator>().AdvanceAsync(userId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama duruyor.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üretim sırası ilerletilemedi (kullanıcı {UserId})", userId);
        }
    }

    private async Task DrainAsync()
    {
        try
        {
            await Task.WhenAll(_running.Values);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Üretim görevleri kapanırken hata");
        }

        _running.Clear();
    }
}
