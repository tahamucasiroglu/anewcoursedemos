using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="UserBanRecord"/> tablosunun eşlemesi (<c>identity.user_bans</c>, spec 0024 Req 8): <c>kind</c> metin
/// (<c>ban</c> | <c>unban</c>, check), sebep en fazla 500 karakter. Kayıtlar silinmez.
/// </summary>
public sealed class UserBanRecordConfiguration : IEntityTypeConfiguration<UserBanRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserBanRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("user_bans", t => t.HasCheckConstraint("ck_user_bans_kind", "kind IN ('ban', 'unban')"));
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(b => b.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(b => b.Action)
            .HasColumnName("kind")
            .HasMaxLength(8)
            .HasConversion(a => a == BanAction.Unban ? "unban" : "ban", v => v == "unban" ? BanAction.Unban : BanAction.Ban)
            .IsRequired();
        builder.Property(b => b.AdminId).HasColumnName("admin_id").IsRequired();
        builder.Property(b => b.At).HasColumnName("at").IsRequired();
        builder.Property(b => b.Reason).HasColumnName("reason").HasMaxLength(BanReason.MaxLength);
        builder.HasIndex(b => b.UserId).HasDatabaseName("ix_user_bans_user_id");
    }
}
