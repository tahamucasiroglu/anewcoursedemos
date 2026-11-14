using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ProductionSettings"/> tablo eşlemesi (<c>production.production_settings</c>): kullanıcı başına tek kayıt (benzersiz <c>user_id</c>), ayarlar JSON metni.
/// </summary>
public sealed class ProductionSettingsConfiguration : IEntityTypeConfiguration<ProductionSettings>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<ProductionSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("production_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.SettingsJson).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
