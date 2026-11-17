using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Configurations;

/// <summary><see cref="ErrorReport"/> tablosunun eşlemesi (spec 0022): numara identity, kaynak/durum sayı, metin alanları sınırlı.</summary>
public sealed class ErrorReportConfiguration : IEntityTypeConfiguration<ErrorReport>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ErrorReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("error_reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Number).UseIdentityAlwaysColumn();
        builder.HasIndex(r => r.Number).IsUnique();
        builder.Property(r => r.Source).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ProductVersion).HasMaxLength(ErrorReportRules.ShortFieldMaxLength).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(ErrorReportRules.SummaryMaxLength);
        builder.Property(r => r.Page).HasMaxLength(ErrorReportRules.ShortFieldMaxLength);
        builder.Property(r => r.CorrelationId).HasMaxLength(ErrorReportRules.ShortFieldMaxLength);
        builder.Property(r => r.AgentVersion).HasMaxLength(ErrorReportRules.ShortFieldMaxLength);
        builder.Property(r => r.OperatingSystem).HasMaxLength(ErrorReportRules.ShortFieldMaxLength);
        builder.Property(r => r.Component).HasMaxLength(ErrorReportRules.ShortFieldMaxLength);
        builder.Property(r => r.Step).HasMaxLength(ErrorReportRules.ShortFieldMaxLength);
        builder.Property(r => r.Attachments).HasMaxLength(ErrorReportRules.AttachmentMaxLength);
        builder.Property(r => r.AdminNote).HasMaxLength(ErrorReportRules.AdminNoteMaxLength); // spec 0025 Req 5.
        builder.HasIndex(r => new { r.UserId, r.CreatedAt }); // Saatlik sınır sayımı (Req 15).
        builder.HasIndex(r => r.CreatedAt);
    }
}
