using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="QueueItem"/> tablo eşlemesi (<c>production.queue_items</c>): durum metin olarak; (<c>user_id</c>, <c>status</c>, <c>order</c>) indeksi sıradaki öğe sorgusu için.
/// </summary>
public sealed class QueueItemConfiguration : IEntityTypeConfiguration<QueueItem>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<QueueItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("queue_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(ProductionLimits.TitleMaxLength);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Message).HasMaxLength(ProductionLimits.MessageMaxLength);
        builder.Ignore(x => x.IsActive);
        builder.HasIndex(x => new { x.UserId, x.Status, x.Order });
    }
}
