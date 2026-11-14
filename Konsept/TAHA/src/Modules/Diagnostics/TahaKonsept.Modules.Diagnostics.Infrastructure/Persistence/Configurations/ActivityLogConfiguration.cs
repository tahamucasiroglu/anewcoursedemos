using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ActivityLog"/> tablosu (<c>activity_logs</c>): seviye metin olarak, ayrıntı <c>jsonb</c>;
/// indeksler: oluşturulma anı (gün sorgusu / şerit), kullanıcı + an, seviye + an.
/// </summary>
public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    /// <summary>Yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("activity_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Source).HasMaxLength(ActivityLog.MaxSourceLength).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(ActivityLog.MaxMessageLength).IsRequired();
        builder.Property(x => x.Details).HasColumnType("jsonb");
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Level, x.CreatedAtUtc });
    }
}
