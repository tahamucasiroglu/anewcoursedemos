using Konsept.Modules.Concepts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Concepts.Infrastructure.Persistence.Configurations;

/// <summary><c>concepts.topic_suggestions</c>: öneri başına satır (bekleyen, önerilip seçilmemiş, seçildi).</summary>
public sealed class TopicSuggestionConfiguration : IEntityTypeConfiguration<TopicSuggestion>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "topic_suggestions";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TopicSuggestion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(s => s.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(s => s.TitleKey).HasColumnName("title_key").HasMaxLength(500).IsRequired();
        builder.Property(s => s.Summary).HasColumnName("summary").HasMaxLength(2000).IsRequired();
        builder.Property(s => s.State).HasColumnName("state").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(s => new { s.UserId, s.ProfileId }).HasDatabaseName("ix_topic_suggestions_user_profile");
    }
}
