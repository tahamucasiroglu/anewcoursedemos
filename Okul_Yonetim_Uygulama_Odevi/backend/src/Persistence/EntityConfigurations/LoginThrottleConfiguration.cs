using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Hatalı giriş sayacı tablosu (login_throttles): anahtar e-posta. PostgreSQL'in satır sürümü (<c>xmin</c>) iyimser
/// eşzamanlılık belirteci: aynı satırı iki istek birden güncellerse ikincisi çakışma alır ve yeniden dener.
/// </summary>
public sealed class LoginThrottleConfiguration : IEntityTypeConfiguration<LoginThrottle>
{
    public void Configure(EntityTypeBuilder<LoginThrottle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("login_throttles");
        builder.HasKey(t => t.Email);
        builder.Property(t => t.Email).HasMaxLength(320);
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
