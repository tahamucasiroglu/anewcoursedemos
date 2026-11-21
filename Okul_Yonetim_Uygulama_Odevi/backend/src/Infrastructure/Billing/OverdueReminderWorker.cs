using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Features.Billing.Reminders;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Billing;

/// <summary>
/// Gecikme hatırlatması işçisi (spec 0015 gereksinim 9; plan Risks 2, 3, 8): açılıştan 1 dk sonra, sonra 15 dakikada bir çalışır.
/// Etkin okulların yalnızca kimliklerini izinli okumadan (<see cref="IPlatformDirectory.ListActiveTenantIdsAsync"/>) alır; her okul
/// için yeni DI kapsamında <see cref="OverdueReminderRunner"/>'ı çağırır (okul başına sistem oturumu — filtre okulla sınırlı). Türkiye
/// saatiyle 10:00'dan önce komut hiçbir şey göndermez; aynı hatırlatma iki kez gitmez (komutun sırası + tekil dizin). Bir okulun
/// hatası yalnızca okul kimliğiyle Error loglanır, diğer okullar sürer. Ayarla kapatılabilir (<c>Billing:RemindersEnabled</c>).
/// </summary>
public sealed class OverdueReminderWorker : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory scopes;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OverdueReminderWorker> logger;

    public OverdueReminderWorker(IServiceScopeFactory scopes, TimeProvider timeProvider, ILogger<OverdueReminderWorker> logger)
    {
        this.scopes = scopes;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <summary>Bütün etkin okullar için bir tur; gönderilen toplam hatırlatma sayısı.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> tenantIds;
        AsyncServiceScope listScope = this.scopes.CreateAsyncScope();
        await using (listScope.ConfigureAwait(false))
        {
            tenantIds = await listScope.ServiceProvider.GetRequiredService<IPlatformDirectory>().ListActiveTenantIdsAsync(cancellationToken).ConfigureAwait(false);
        }

        int total = 0;
        foreach (Guid tenantId in tenantIds)
        {
            AsyncServiceScope scope = this.scopes.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                try
                {
                    total += await scope.ServiceProvider.GetRequiredService<OverdueReminderRunner>().RunAsync(tenantId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
                {
                    this.logger.LogError(exception, "Gecikme hatırlatması çalışması hata verdi: okul {TenantId}", tenantId);
                }
            }
        }

        return total;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, this.timeProvider, stoppingToken).ConfigureAwait(false);
            using PeriodicTimer timer = new(Interval, this.timeProvider);
            do
            {
                try
                {
                    await this.RunOnceAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
                {
                    this.logger.LogError(exception, "Gecikme hatırlatması turu başlatılamadı");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapanıyor: tur yarıda bırakılır (işlem geri döner, bir sonraki açılışta yeniden denenir).
            this.logger.LogInformation("Gecikme hatırlatması işçisi durdu");
        }
    }
}
