using Konsept.Modules.Profiles.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence;

/// <summary>Profiles modülünün veritabanı bağlamı (<c>profiles</c> şeması): profiller ve seçili profil.</summary>
public sealed class ProfilesDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "profiles";

    /// <summary>Initializes a new instance of the <see cref="ProfilesDbContext"/> class.</summary>
    /// <param name="options">EF Core seçenekleri.</param>
    public ProfilesDbContext(DbContextOptions<ProfilesDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets profiller.</summary>
    public DbSet<Profile> Profiles => Set<Profile>();

    /// <summary>Gets seçili profil satırları.</summary>
    public DbSet<ProfileSelection> Selections => Set<ProfileSelection>();

    /// <summary>Gets profil başına Konseptler sayfası ayarları (spec 0005).</summary>
    public DbSet<ConceptSettings> ConceptSettings => Set<ConceptSettings>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;
}
