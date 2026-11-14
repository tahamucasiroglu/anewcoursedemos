using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Profiles.Domain.Entities;

namespace TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Concrete;

/// <summary>
/// Profiles modülünün DbContext'i (şema <c>profiles</c>). Tablolar: <see cref="Profiles"/>.
/// Yapılandırmalar aynı derlemeden otomatik uygulanır (<c>ProfileConfiguration</c>).
/// </summary>
/// <param name="options">EF seçenekleri.</param>
/// <param name="clock">Saat.</param>
public sealed class ProfilesDbContext(DbContextOptions<ProfilesDbContext> options, IClock clock)
    : ModuleDbContextBase(options, clock)
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "profiles";

    /// <summary>Modül şeması.</summary>
    public override string Schema => SchemaName;

    /// <summary>Kanal profilleri.</summary>
    public DbSet<Profile> Profiles => Set<Profile>();
}
