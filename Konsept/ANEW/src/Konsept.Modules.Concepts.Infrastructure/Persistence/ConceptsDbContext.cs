using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Concepts.Infrastructure.Persistence;

/// <summary>Concepts modülünün veritabanı bağlamı (<c>concepts</c> şeması): konseptler ve konu önerileri.</summary>
public sealed class ConceptsDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "concepts";

    /// <summary>Initializes a new instance of the <see cref="ConceptsDbContext"/> class.</summary>
    /// <param name="options">EF Core seçenekleri.</param>
    public ConceptsDbContext(DbContextOptions<ConceptsDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets konseptler.</summary>
    public DbSet<Concept> Concepts => Set<Concept>();

    /// <summary>Gets konu önerileri.</summary>
    public DbSet<TopicSuggestion> TopicSuggestions => Set<TopicSuggestion>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;
}
