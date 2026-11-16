using Konsept.Modules.Monitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="SystemEvent"/> tablosunun eşlemesi.
/// </summary>
public sealed class SystemEventConfiguration : IEntityTypeConfiguration<SystemEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SystemEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("system_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Type).HasMaxLength(SystemEvent.TypeMaxLength).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(SystemEvent.MessageMaxLength).IsRequired();
        builder.Property(e => e.Level).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.UserId);
        builder.Property(e => e.ProfileName).HasMaxLength(SystemEvent.ProfileNameMaxLength);
        builder.HasIndex(e => new { e.UserId, e.OccurredAt }); // Günlük geçmişi: kullanıcı + gün aralığı (spec 0021).
    }
}
