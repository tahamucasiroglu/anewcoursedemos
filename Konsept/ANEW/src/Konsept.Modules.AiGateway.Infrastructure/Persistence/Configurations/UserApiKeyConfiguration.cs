using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="UserApiKey"/> eşlemesi (<c>ai_gateway.api_keys</c>): kullanıcı + sağlayıcı tekildir (aynı anda en fazla bir key,
/// spec 0004 AC-7). Düz key sütunu yoktur; yalnızca şifreli değer ve maskeli ipucu.
/// </summary>
public sealed class UserApiKeyConfiguration : IEntityTypeConfiguration<UserApiKey>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "api_keys";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserApiKey> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName, t => t.HasCheckConstraint("ck_api_keys_validation", "validation IN ('verified', 'unverified')"));
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(k => k.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(k => k.ProviderId).HasColumnName("provider_id").HasMaxLength(64).IsRequired();
        builder.Property(k => k.Ciphertext).HasColumnName("ciphertext").IsRequired();
        builder.Property(k => k.Hint).HasColumnName("hint").HasMaxLength(16).IsRequired();
        builder.Property(k => k.Validation)
            .HasColumnName("validation")
            .HasMaxLength(16)
            .HasConversion(v => v == KeyValidation.Verified ? "verified" : "unverified", v => v == "verified" ? KeyValidation.Verified : KeyValidation.Unverified)
            .IsRequired();
        builder.Property(k => k.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(k => new { k.UserId, k.ProviderId }).IsUnique().HasDatabaseName("ux_api_keys_user_provider");
    }
}
