using Konsept.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Configurations;

/// <summary><see cref="ProfileSelection"/> eşlemesi (<c>profiles.selections</c>): kullanıcı başına tek satır (spec 0003 Req 9).</summary>
public sealed class ProfileSelectionConfiguration : IEntityTypeConfiguration<ProfileSelection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProfileSelection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("selections");
        builder.HasKey(s => s.UserId);
        builder.Property(s => s.UserId).HasColumnName("user_id").ValueGeneratedNever();
        builder.Property(s => s.ProfileId).HasColumnName("profile_id").IsRequired();
    }
}
