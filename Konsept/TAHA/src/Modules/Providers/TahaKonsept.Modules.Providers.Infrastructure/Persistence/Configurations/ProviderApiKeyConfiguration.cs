using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ProviderApiKey"/> tablo eşlemesi (<c>providers.api_keys</c>; kullanıcı + platform benzersiz).
/// </summary>
public sealed class ProviderApiKeyConfiguration : IEntityTypeConfiguration<ProviderApiKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProviderApiKey> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Platform).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ProtectedKey).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.Last4).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(60);
        builder.HasIndex(x => new { x.UserId, x.Platform }).IsUnique();
    }
}
