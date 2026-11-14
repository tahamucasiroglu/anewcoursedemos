using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Job"/> tablo eşlemesi (<c>production.jobs</c>): aşama çalıştırmaları alt tablo (<c>stage_runs</c>, alan üzerinden erişim); durum/biçim metin olarak.
/// </summary>
public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(ProductionLimits.TitleMaxLength);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.StatusMessage).HasMaxLength(ProductionLimits.MessageMaxLength);
        builder.Property(x => x.SettingsJson).IsRequired();
        builder.Property(x => x.DocumentJson).IsRequired();
        builder.Ignore(x => x.TotalCostUsd);
        builder.Ignore(x => x.OrderedRuns);
        builder.Ignore(x => x.SharesWorkspace);
        builder.HasMany(x => x.Runs).WithOne().HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Runs).HasField("_runs").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.UserId, x.Status });
    }
}
