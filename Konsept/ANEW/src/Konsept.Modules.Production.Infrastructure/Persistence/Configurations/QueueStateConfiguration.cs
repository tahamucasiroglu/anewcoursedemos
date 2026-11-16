using Konsept.Modules.Production.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary><c>production.queue_states</c>: kullanıcı başına sıra durumu (art arda hata, durdu; spec 0017 Req 11).</summary>
public sealed class QueueStateConfiguration : IEntityTypeConfiguration<QueueState>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "queue_states";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<QueueState> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(q => q.UserId);
        builder.Property(q => q.UserId).HasColumnName("user_id").ValueGeneratedNever();
        builder.Property(q => q.ConsecutiveFailures).HasColumnName("consecutive_failures").IsRequired();
        builder.Property(q => q.Halted).HasColumnName("halted").IsRequired();
        builder.Property(q => q.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
