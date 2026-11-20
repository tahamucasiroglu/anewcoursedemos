using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Akademik takvim tablosu (calendar_entries): kuruma ve döneme bağlı, başlık zorunlu, tür metin olarak, günler
/// <c>date</c>; silme yumuşak (<c>deleted_at</c>). Dönem + başlangıç dizini dönem takvimini sıralı okur.
/// </summary>
public sealed class CalendarEntryConfiguration : IEntityTypeConfiguration<CalendarEntry>
{
    public void Configure(EntityTypeBuilder<CalendarEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("calendar_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.StartDate).HasColumnType("date");
        builder.Property(e => e.EndDate).HasColumnType("date");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Term>().WithMany().HasForeignKey(e => e.TermId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TermId, e.StartDate });
    }
}
