using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="User"/> tablosunun eşlemesi (<c>identity.users</c>): sütunlar atomik SQL'de adla kullanıldığı için açıkça
/// adlandırılır; <c>normalized_email</c> tekildir (eşzamanlı kayıtta ikinci hesap oluşmaz). <c>role</c> metin sütunudur
/// (<c>user</c> | <c>admin</c>, check constraint, varsayılan <c>user</c>; spec 0011).
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "users";

    /// <summary><c>role</c> sütununda kullanıcı rolünün değeri (varsayılan).</summary>
    public const string UserRoleValue = "user";

    /// <summary><c>role</c> sütununda admin rolünün değeri.</summary>
    public const string AdminRoleValue = "admin";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        builder.Property(u => u.FailedLoginCount).HasColumnName("failed_login_count").IsRequired();
        builder.Property(u => u.LockedUntil).HasColumnName("locked_until");
        builder.Property(u => u.RegisteredAt).HasColumnName("registered_at").IsRequired();
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(16)
            .HasConversion(r => r == Role.Admin ? AdminRoleValue : UserRoleValue, v => v == AdminRoleValue ? Role.Admin : Role.User)
            .HasDefaultValue(Role.User)
            .HasSentinel((Role)(-1))
            .IsRequired();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
        builder.ToTable(t => t.HasCheckConstraint("ck_users_role", $"role IN ('{UserRoleValue}', '{AdminRoleValue}')"));
    }
}
