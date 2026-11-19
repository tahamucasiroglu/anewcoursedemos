using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.HealthChecks;

/// <summary>
/// <see cref="IDatabaseHealthProbe"/>'un PostgreSQL uygulaması: bağlantı açılabiliyor mu diye bakar.
/// Bağlantı hatası false döner ve yalnızca loga (Warning) yazılır; iptal (zaman aşımı) çağırana iletilir.
/// </summary>
public sealed class NpgsqlDatabaseHealthProbe : IDatabaseHealthProbe
{
    private readonly OkulDbContext dbContext;
    private readonly ILogger<NpgsqlDatabaseHealthProbe> logger;

    public NpgsqlDatabaseHealthProbe(OkulDbContext dbContext, ILogger<NpgsqlDatabaseHealthProbe> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await this.dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // Sağlık kontrolü: her bağlantı hatası "ulaşılamıyor" demektir; hata loglanır, yutulmaz.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            this.logger.LogWarning(exception, "Veritabanı bağlantı kontrolü başarısız");
            return false;
        }
    }
}
