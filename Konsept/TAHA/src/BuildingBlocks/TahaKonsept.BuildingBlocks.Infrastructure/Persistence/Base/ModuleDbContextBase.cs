using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Abstract;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

/// <summary>
/// Her modül DbContext'inin tabanı: varsayılan şema (<see cref="Schema"/>), aynı derlemedeki
/// <c>IEntityTypeConfiguration</c>'ların otomatik uygulanması, <see cref="IAuditable"/> zaman damgalarının
/// <see cref="IClock"/> ile doldurulması ve <see cref="IUnitOfWork"/>. Kayıt: <c>AddModuleDbContext&lt;T&gt;(config, schema)</c>.
/// </summary>
public abstract class ModuleDbContextBase : DbContext, IUnitOfWork
{
    private readonly IClock _clock;

    /// <summary>Oluşturur.</summary>
    /// <param name="options">EF seçenekleri (türetilen sınıf <c>DbContextOptions&lt;TContext&gt;</c> geçirir).</param>
    /// <param name="clock">Saat.</param>
    protected ModuleDbContextBase(DbContextOptions options, IClock clock)
        : base(options)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Modülün veritabanı şeması (snake_case, ör. <c>concepts</c>).</summary>
    public abstract string Schema { get; }

    /// <summary>Değişiklikleri kaydeder; önce denetim alanlarını doldurur.</summary>
    /// <param name="acceptAllChangesOnSuccess">EF parametresi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Etkilenen kayıt sayısı.</returns>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Değişiklikleri eşzamanlı kaydeder; önce denetim alanlarını doldurur.</summary>
    /// <param name="acceptAllChangesOnSuccess">EF parametresi.</param>
    /// <returns>Etkilenen kayıt sayısı.</returns>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditing();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Şemayı ayarlar ve bu DbContext'in derlemesindeki yapılandırmaları uygular.</summary>
    /// <param name="modelBuilder">Model oluşturucu.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void ApplyAuditing()
    {
        var now = _clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
