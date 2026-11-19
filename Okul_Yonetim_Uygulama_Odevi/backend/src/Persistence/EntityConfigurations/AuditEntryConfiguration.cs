using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Audit tablosu (audit_entries): olay metin olarak saklanır; yabancı anahtar yok (kayıt, ilgili satırdan bağımsız
/// kalıcıdır). Değiştirme ve silme veritabanı tetikleyicisiyle reddedilir (migration IdentityAndOrganization).
/// </summary>
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("audit_entries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(64);
        builder.Property(a => a.AttemptedEmail).HasMaxLength(320);
        builder.Property(a => a.TargetType).HasMaxLength(64);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.TenantId);
    }
}
