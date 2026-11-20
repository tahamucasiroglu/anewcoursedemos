using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>İki adımlı doğrulama beklemeleri (two_factor_challenges): yalnızca özet (tekil); bekleyen anahtar şifreli.</summary>
public sealed class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge>
{
    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("two_factor_challenges");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(c => c.TokenHash).IsUnique();
        builder.Property(c => c.PendingSecretCipher).HasMaxLength(512);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
