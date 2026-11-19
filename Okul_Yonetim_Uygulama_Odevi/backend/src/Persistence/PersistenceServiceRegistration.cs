using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;
using OkulYonetim.Persistence.HealthChecks;
using OkulYonetim.Persistence.Repositories;

namespace OkulYonetim.Persistence;

/// <summary>
/// Persistence katmanının servis kaydı: PostgreSQL bağlamı (bağlantı "ConnectionStrings:OkulDb" ayarından,
/// snake_case isimlendirme; kurum/şube filtresi oturumdan), depolar, audit kaydı, işlem birimi ve veritabanı sağlık portu.
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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ILoginThrottleRepository, LoginThrottleRepository>();
        services.AddScoped<IAuditLog, EfAuditLog>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
