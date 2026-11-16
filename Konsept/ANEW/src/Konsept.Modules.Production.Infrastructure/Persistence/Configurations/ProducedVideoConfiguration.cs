using Konsept.Modules.Production.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary><c>production.videos</c>: tamamlanan videonun değişmez sürüm kaydı (spec 0017 Req 22); konsept metni kopyası <c>jsonb</c>.</summary>
public sealed class ProducedVideoConfiguration : IEntityTypeConfiguration<ProducedVideo>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "videos";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProducedVideo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(v => v.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(v => v.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(v => v.ConceptId).HasColumnName("concept_id").IsRequired();
        builder.Property(v => v.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(v => v.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(v => v.ConceptText).HasColumnName("concept_text").HasColumnType("jsonb").IsRequired();
        builder.Property(v => v.CodeModel).HasColumnName("code_model").HasMaxLength(200);
        builder.Property(v => v.ImageModel).HasColumnName("image_model").HasMaxLength(200);
        builder.Property(v => v.AudioModel).HasColumnName("audio_model").HasMaxLength(200);
        builder.Property(v => v.Engine).HasColumnName("engine").HasMaxLength(200).IsRequired();
        builder.Property(v => v.Subtitles).HasColumnName("subtitles").IsRequired();
        builder.Property(v => v.ProgressBar).HasColumnName("progress_bar").IsRequired();
        builder.Property(v => v.AiVoiceNotice).HasColumnName("ai_voice_notice").IsRequired();
        builder.Property(v => v.FileName).HasColumnName("file_name").HasMaxLength(300).IsRequired();
        builder.Property(v => v.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(v => v.EndedAt).HasColumnName("ended_at").IsRequired();
        builder.Property(v => v.DurationSeconds).HasColumnName("duration_seconds").HasDefaultValue(0).IsRequired();
        builder.Property(v => v.Format).HasColumnName("format").HasMaxLength(20).HasDefaultValue("landscape").IsRequired();
        builder.Property(v => v.CostUsd).HasColumnName("cost_usd").HasPrecision(18, 6).HasDefaultValue(0m).IsRequired();
        builder.HasIndex(v => new { v.UserId, v.ConceptId, v.EndedAt }).HasDatabaseName("ix_videos_user_concept_ended");
        builder.HasIndex(v => new { v.UserId, v.ProfileId, v.EndedAt }).HasDatabaseName("ix_videos_user_profile_ended");
    }
}
