using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;
using OkulYonetim.Persistence.HealthChecks;

namespace OkulYonetim.Persistence;

/// <summary>
/// Persistence katmanının servis kaydı: PostgreSQL bağlamı (bağlantı "ConnectionStrings:OkulDb" ayarından,
/// snake_case isimlendirme) ve veritabanı sağlık portu.
/// </summary>
public static class PersistenceServiceRegistration
{
    public const string ConnectionStringName = "OkulDb";

    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName) ?? string.Empty;
        services.AddDbContext<OkulDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IDatabaseHealthProbe, NpgsqlDatabaseHealthProbe>();
        return services;
    }
}
