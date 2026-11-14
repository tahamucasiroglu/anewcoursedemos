using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="CallLog"/> tablo eşlemesi (<c>providers.call_logs</c>; kullanıcı + zaman indeksi aylık toplam için).
/// </summary>
public sealed class CallLogConfiguration : IEntityTypeConfiguration<CallLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CallLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("call_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Platform).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ModelKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProviderModelId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Units).HasPrecision(18, 6);
        builder.Property(x => x.UnitKind).HasMaxLength(20);
        builder.Property(x => x.CostUsd).HasPrecision(18, 6);
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(CallLog.MaxErrorLength);
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}
