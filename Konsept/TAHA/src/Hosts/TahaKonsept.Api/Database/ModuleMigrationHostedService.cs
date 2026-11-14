using TahaKonsept.BuildingBlocks.Api.Modules.Abstract;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Concrete;

namespace TahaKonsept.Api.Database;

/// <summary>
/// Modül migration'larını API'yi bloklamadan arka planda uygular (<c>Database:MigrateOnStartup</c>, varsayılan <c>true</c>).
/// Veritabanına ulaşılamazsa ilk hatada Critical loglar (hedef sunucu/port ile, parolasız), sonra artan aralıklarla
/// (<c>Database:RetrySeconds</c> başlangıç, <c>Database:MaxRetrySeconds</c> üst sınır) yeniden dener; API bu sürede çalışmaya devam eder.
/// Durum <see cref="IDatabaseStatus"/> üzerinden okunur (sağlık ucu, orkestratör).
/// </summary>
internal sealed class ModuleMigrationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IModuleRegistry _registry;
    private readonly DatabaseStatus _status;
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModuleMigrationHostedService> _logger;

    /// <summary>Bağımlılıklarla oluşturur.</summary>
    /// <param name="services">Kök servis sağlayıcı (modüller kendi scope'larını açar).</param>
    /// <param name="registry">Modül kayıt defteri.</param>
    /// <param name="status">Veritabanı durumu (yazılabilir tekil).</param>
    /// <param name="clock">Saat.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="logger">Logger.</param>
    public ModuleMigrationHostedService(
        IServiceProvider services,
        IModuleRegistry registry,
        DatabaseStatus status,
        IClock clock,
        IConfiguration configuration,
        ILogger<ModuleMigrationHostedService> logger)
    {
        _services = services;
        _registry = registry;
        _status = status;
        _clock = clock;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Migration döngüsü: başarılı olana ya da uygulama kapanana kadar dener.</summary>
    /// <param name="stoppingToken">Kapanış belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_registry.Modules.Count == 0 || !_configuration.GetValue("Database:MigrateOnStartup", true))
        {
            _status.MarkReady(_clock.UtcNow);
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(1, _configuration.GetValue("Database:RetrySeconds", 5)));
        var maxDelay = TimeSpan.FromSeconds(Math.Max(1, _configuration.GetValue("Database:MaxRetrySeconds", 60)));
        var target = DatabaseConnectionString.Describe(_configuration);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (await TryMigrateAsync(target, stoppingToken))
            {
                return;
            }

            await Task.Delay(delay, stoppingToken);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
        }
    }

    private async Task<bool> TryMigrateAsync(string target, CancellationToken stoppingToken)
    {
        try
        {
            await _registry.MigrateAsync(_services, stoppingToken);
            _status.MarkReady(_clock.UtcNow);
            _logger.LogInformation(
                "Veritabanı hazır: {Count} modülün migration'ları uygulandı ({Target}, deneme {Attempt}).",
                _registry.Modules.Count,
                target,
                _status.AttemptCount);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            _status.MarkAttemptFailed(_clock.UtcNow, ex.GetBaseException().Message);
            if (_status.AttemptCount == 1)
            {
                _logger.LogCritical(
                    ex,
                    "Veritabanına bağlanılamadı / migration uygulanamadı ({Target}). API veritabanısız çalışıyor; arka planda yeniden denenecek. "
                    + "Kontrol: PostgreSQL çalışıyor mu, geliştirmede 'ssh -L 5433:localhost:5432 tmpi' tüneli açık mı, TAHAKONSEPT_DB_PASSWORD tanımlı mı?",
                    target);
            }
            else
            {
                _logger.LogError(
                    "Veritabanı hâlâ kullanılamıyor ({Target}, deneme {Attempt}): {Error}",
                    target,
                    _status.AttemptCount,
                    ex.GetBaseException().Message);
            }

            return false;
        }
    }
}
