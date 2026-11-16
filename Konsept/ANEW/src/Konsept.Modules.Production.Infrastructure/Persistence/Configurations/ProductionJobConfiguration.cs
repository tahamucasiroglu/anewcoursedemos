using System.Text.Json;
using Konsept.Modules.Production.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>production.jobs</c>: iş başına satır; sabitlenen ayarlar <c>jsonb</c>. Kısıtlar (spec 0006 Req 10, 14): kullanıcı başına en çok bir
/// çalışan iş (durum Üretiliyor/Ajan bekleniyor kısmi tekil dizini) ve konsept başına en çok bir tamamlanmamış iş.
/// </summary>
public sealed class ProductionJobConfiguration : IEntityTypeConfiguration<ProductionJob>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "jobs";

    /// <summary>Tek çalışan iş dizininin adı.</summary>
    public const string OneRunningIndex = "ux_jobs_one_running_per_user";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductionJob> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(j => j.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(j => j.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(j => j.ConceptId).HasColumnName("concept_id").IsRequired();
        builder.Property(j => j.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(j => j.Language).HasColumnName("language").HasMaxLength(8).IsRequired();
        builder.Property(j => j.Format).HasColumnName("format").HasMaxLength(16).IsRequired();
        builder.Property(j => j.TargetDurationSeconds).HasColumnName("target_duration_seconds").IsRequired();
        builder.Property(j => j.Position).HasColumnName("position").IsRequired();
        builder.Property(j => j.Status).HasColumnName("status").IsRequired();
        builder.Property(j => j.FixedSettings)
            .HasColumnName("fixed_settings")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, Json),
                v => v == null ? null : JsonSerializer.Deserialize<FixedJobSettings>(v, Json));
        builder.Property(j => j.Progress)
            .HasColumnName("progress")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, Json),
                v => v == null ? null : JsonSerializer.Deserialize<JobProgress>(v, Json));
        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(j => new { j.UserId, j.ProfileId, j.Position }).HasDatabaseName("ix_jobs_user_profile_position");
        builder.HasIndex(j => j.UserId).IsUnique().HasFilter("status IN (1, 3)").HasDatabaseName(OneRunningIndex);
        builder.HasIndex(j => j.ConceptId).IsUnique().HasFilter("status <> 5").HasDatabaseName("ux_jobs_one_active_per_concept");
    }
}
