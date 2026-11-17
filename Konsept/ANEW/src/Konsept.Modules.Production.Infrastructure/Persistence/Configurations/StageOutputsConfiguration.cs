using System.Text.Json;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary><c>production.stage_outputs</c>: konsept başına yeniden kullanılan aşama çıktıları (spec 0017 Req 10); plan ve kodlar <c>jsonb</c>.</summary>
public sealed class StageOutputsConfiguration : IEntityTypeConfiguration<StageOutputs>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "stage_outputs";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StageOutputs> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(o => o.ConceptId);
        builder.Property(o => o.ConceptId).HasColumnName("concept_id").ValueGeneratedNever();
        builder.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(o => o.TextHash).HasColumnName("text_hash").HasMaxLength(64).IsRequired();
        builder.Property(o => o.Plan)
            .HasColumnName("plan")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, Json),
                v => v == null ? null : JsonSerializer.Deserialize<List<ScenePlan>>(v, Json),
                new ValueComparer<IReadOnlyList<ScenePlan>?>((a, b) => ReferenceEquals(a, b), v => v == null ? 0 : v.Count, v => v));
        builder.Property(o => o.Codes)
            .HasColumnName("codes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, Json),
                v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, Json),
                new ValueComparer<IReadOnlyList<string>?>((a, b) => ReferenceEquals(a, b), v => v == null ? 0 : v.Count, v => v));
        builder.Property(o => o.LocalDevice).HasColumnName("local_device");
        builder.Property(o => o.LocalDone)
            .HasColumnName("local_done")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<PipelineStage>>(v, Json) ?? new List<PipelineStage>(),
                new ValueComparer<IReadOnlyList<PipelineStage>>((a, b) => ReferenceEquals(a, b), v => v.Count, v => v))
            .IsRequired();
        builder.Property(o => o.Signatures)
            .HasColumnName("signatures")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<Dictionary<PipelineStage, string>>(v, Json) ?? new Dictionary<PipelineStage, string>(),
                new ValueComparer<IReadOnlyDictionary<PipelineStage, string>>((a, b) => ReferenceEquals(a, b), v => v.Count, v => v))
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(o => o.UserId).HasDatabaseName("ix_stage_outputs_user");
    }
}
