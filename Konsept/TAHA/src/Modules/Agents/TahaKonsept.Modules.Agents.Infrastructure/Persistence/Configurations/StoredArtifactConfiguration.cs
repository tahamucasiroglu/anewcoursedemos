using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="StoredArtifact"/> tablo eşlemesi (<c>agents.stored_artifacts</c>): kullanıcı + iş indeksi.
/// </summary>
public sealed class StoredArtifactConfiguration : IEntityTypeConfiguration<StoredArtifact>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Oluşturucu.</param>
    public void Configure(EntityTypeBuilder<StoredArtifact> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("stored_artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RelativePath).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.JobId });
    }
}
