using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="AppRole"/> eşlemesi: Identity'nin rol adı indeksini snake_case adlandırır.
/// </summary>
public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasIndex(r => r.NormalizedName).HasDatabaseName("ix_roles_normalized_name");
    }
}
