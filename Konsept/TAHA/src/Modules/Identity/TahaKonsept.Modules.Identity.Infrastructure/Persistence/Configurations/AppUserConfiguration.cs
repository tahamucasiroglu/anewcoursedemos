using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="AppUser"/> ek alanlarının eşlemesi: tema metin olarak, yasak/zaman alanları; Identity indeks adları snake_case.
/// </summary>
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Property(u => u.Theme).HasConversion<string>().HasMaxLength(16);
        builder.Property(u => u.IsBanned).HasDefaultValue(false);
        builder.Property(u => u.CreatedAtUtc);
        builder.Property(u => u.BannedAtUtc);
        builder.Property(u => u.LastLoginAtUtc);
        builder.HasIndex(u => u.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");
        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");
    }
}
