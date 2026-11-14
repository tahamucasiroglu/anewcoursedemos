using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ErrorReport"/> tablosu (<c>error_reports</c>): kaynak ve durum metin olarak; indeksler: durum + an, an.
/// </summary>
public sealed class ErrorReportConfiguration : IEntityTypeConfiguration<ErrorReport>
{
    /// <summary>Yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<ErrorReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("error_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Message).HasMaxLength(ErrorReport.MaxMessageLength).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(ErrorReport.MaxDetailsLength);
        builder.Property(x => x.PageUrl).HasMaxLength(ErrorReport.MaxPageUrlLength);
        builder.Property(x => x.UserAgent).HasMaxLength(ErrorReport.MaxUserAgentLength);
        builder.Property(x => x.AppVersion).HasMaxLength(ErrorReport.MaxAppVersionLength);
        builder.Property(x => x.Machine).HasMaxLength(ErrorReport.MaxMachineLength);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
