using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>
/// Concepts modülünün DbContext'i (şema <c>concepts</c>). Tablolar: <see cref="Concepts"/> (belge jsonb),
/// <see cref="TopicMemory"/> (konu hafızası), <see cref="Settings"/> (kullanıcı başına ayar). Yapılandırmalar aynı derlemeden otomatik.
/// </summary>
/// <param name="options">EF seçenekleri.</param>
/// <param name="clock">Saat.</param>
public sealed class ConceptsDbContext(DbContextOptions<ConceptsDbContext> options, IClock clock)
    : ModuleDbContextBase(options, clock)
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "concepts";

    /// <summary>Modül şeması.</summary>
    public override string Schema => SchemaName;

    /// <summary>Konseptler.</summary>
    public DbSet<Concept> Concepts => Set<Concept>();

    /// <summary>Konu hafızası.</summary>
    public DbSet<TopicMemoryEntry> TopicMemory => Set<TopicMemoryEntry>();

    /// <summary>Kullanıcı ayarları.</summary>
    public DbSet<ConceptSettings> Settings => Set<ConceptSettings>();
}
