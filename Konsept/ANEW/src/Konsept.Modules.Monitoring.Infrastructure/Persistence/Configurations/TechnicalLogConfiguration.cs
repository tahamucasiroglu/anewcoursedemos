using Konsept.Modules.Monitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Configurations;

/// <summary><see cref="TechnicalLog"/> tablosunun eşlemesi (<c>monitoring.technical_logs</c>).</summary>
public sealed class TechnicalLogConfiguration : IEntityTypeConfiguration<TechnicalLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TechnicalLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("technical_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Level).IsRequired();
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(TechnicalLog.MessageMaxLength).IsRequired();
        builder.Property(e => e.Exception).HasMaxLength(TechnicalLog.ExceptionMaxLength);
        builder.Property(e => e.CorrelationId).HasMaxLength(TechnicalLog.ShortMaxLength);
        builder.Property(e => e.Category).HasMaxLength(TechnicalLog.ShortMaxLength);
        builder.Property(e => e.AgentVersion).HasMaxLength(TechnicalLog.ShortMaxLength);
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.CorrelationId);
    }
}
