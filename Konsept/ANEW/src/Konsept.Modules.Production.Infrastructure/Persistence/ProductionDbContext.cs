using Konsept.Modules.Production.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence;

/// <summary>Production modülünün <c>production</c> şeması (spec 0006): profil başına üretim ayarları ve üretim işleri.</summary>
public sealed class ProductionDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "production";

    /// <summary>Initializes a new instance of the <see cref="ProductionDbContext"/> class.</summary>
    /// <param name="options">Seçenekler.</param>
    public ProductionDbContext(DbContextOptions<ProductionDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets işler.</summary>
    public DbSet<ProductionJob> Jobs => Set<ProductionJob>();

    /// <summary>Gets ayarlar.</summary>
    public DbSet<ProductionSettings> Settings => Set<ProductionSettings>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;
}
