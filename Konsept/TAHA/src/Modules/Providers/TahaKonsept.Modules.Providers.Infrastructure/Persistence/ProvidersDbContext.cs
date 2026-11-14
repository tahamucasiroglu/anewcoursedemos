using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence;

/// <summary>
/// Providers modülünün DbContext'i (şema <c>providers</c>): API anahtarları, çağrı günlüğü, sürümlü fiyatlar, bütçe ayarları.
/// Yapılandırmalar <c>Persistence/Configurations</c> altından otomatik uygulanır.
/// </summary>
public sealed class ProvidersDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "providers";

    /// <summary>Kurucu.</summary>
    /// <param name="options">Seçenekler.</param>
    /// <param name="clock">Saat.</param>
    public ProvidersDbContext(DbContextOptions<ProvidersDbContext> options, IClock clock)
        : base(options, clock)
    {
    }

    /// <inheritdoc />
    public override string Schema => SchemaName;

    /// <summary>API anahtarları.</summary>
    public DbSet<ProviderApiKey> ApiKeys => Set<ProviderApiKey>();

    /// <summary>Çağrı günlüğü.</summary>
    public DbSet<CallLog> CallLogs => Set<CallLog>();

    /// <summary>Sürümlü fiyatlar.</summary>
    public DbSet<ModelPrice> ModelPrices => Set<ModelPrice>();

    /// <summary>Bütçe ayarları.</summary>
    public DbSet<BudgetSetting> BudgetSettings => Set<BudgetSetting>();
}
