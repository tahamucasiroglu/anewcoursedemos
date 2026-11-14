using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ProductionQueue"/> tablo eşlemesi (<c>production.production_queues</c>): kullanıcı başına tek kayıt; çalışma durumu metin olarak.
/// </summary>
public sealed class ProductionQueueConfiguration : IEntityTypeConfiguration<ProductionQueue>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<ProductionQueue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("production_queues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.RunState).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Message).HasMaxLength(ProductionLimits.MessageMaxLength);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
