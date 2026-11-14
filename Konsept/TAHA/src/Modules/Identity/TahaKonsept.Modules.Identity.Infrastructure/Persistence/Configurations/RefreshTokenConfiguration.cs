using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="RefreshToken"/> eşlemesi: tablo <c>refresh_tokens</c>, özet benzersiz, aile ve kullanıcı indeksli,
/// tür/iptal nedeni metin, kullanıcı silinince token'lar da silinir. <c>RevokedAtUtc</c> eşzamanlılık belirtecidir: aynı token'ı iki istek
/// aynı anda döndürürse yalnız biri kaydedilir (diğeri tekrar kullanım sayılır, aile iptal edilir).
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.UserId);
        builder.Property(t => t.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.RevocationReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.RevokedAtUtc).IsConcurrencyToken();
        builder.Ignore(t => t.IsRevoked);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
