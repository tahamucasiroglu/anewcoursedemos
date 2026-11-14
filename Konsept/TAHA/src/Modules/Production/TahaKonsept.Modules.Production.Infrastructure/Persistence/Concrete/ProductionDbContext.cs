using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// Production modülünün DbContext'i (şema <c>production</c>). Tablolar: ayarlar, sıra durumu, sıra öğeleri, işler + aşama çalıştırmaları,
/// videolar, okunuş sözlüğü. Yapılandırmalar aynı derlemeden otomatik uygulanır.
/// </summary>
/// <param name="options">EF seçenekleri.</param>
/// <param name="clock">Saat.</param>
public sealed class ProductionDbContext(DbContextOptions<ProductionDbContext> options, IClock clock)
    : ModuleDbContextBase(options, clock)
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "production";

    /// <summary>Modül şeması.</summary>
    public override string Schema => SchemaName;

    /// <summary>Üretim ayarları.</summary>
    public DbSet<ProductionSettings> Settings => Set<ProductionSettings>();

    /// <summary>Sıra durumları.</summary>
    public DbSet<ProductionQueue> Queues => Set<ProductionQueue>();

    /// <summary>Sıra öğeleri.</summary>
    public DbSet<QueueItem> QueueItems => Set<QueueItem>();

    /// <summary>İşler.</summary>
    public DbSet<Job> Jobs => Set<Job>();

    /// <summary>Aşama çalıştırmaları.</summary>
    public DbSet<StageRun> StageRuns => Set<StageRun>();

    /// <summary>Videolar.</summary>
    public DbSet<Video> Videos => Set<Video>();

    /// <summary>Okunuş sözlüğü.</summary>
    public DbSet<Pronunciation> Pronunciations => Set<Pronunciation>();
}
