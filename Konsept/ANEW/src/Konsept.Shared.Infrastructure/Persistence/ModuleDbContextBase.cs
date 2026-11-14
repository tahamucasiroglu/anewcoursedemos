using Microsoft.EntityFrameworkCore;

namespace Konsept.Shared.Infrastructure.Persistence;

/// <summary>
/// Her modülün <c>DbContext</c>'inin temeli: modül kendi şemasını verir, entity konfigürasyonları
/// modülün derlemesinden otomatik uygulanır (docs/architecture.md, kural 7 ve Y-4).
/// </summary>
public abstract class ModuleDbContextBase : DbContext
{
    /// <summary>Initializes a new instance of the <see cref="ModuleDbContextBase"/> class.</summary>
    /// <param name="options">EF Core seçenekleri.</param>
    protected ModuleDbContextBase(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>Gets modülün veritabanı şeması (ör. <c>monitoring</c>).</summary>
    protected abstract string Schema { get; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
