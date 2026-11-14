using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="StageRun"/> tablo eşlemesi (<c>production.stage_runs</c>): işin alt satırları; aşama/yer/durum metin olarak, maliyet <c>numeric(18,6)</c>, <c>command_id</c> indeksi ajan olayları için.
/// </summary>
public sealed class StageRunConfiguration : IEntityTypeConfiguration<StageRun>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<StageRun> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("stage_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Location).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.InputHash).HasMaxLength(ProductionLimits.HashMaxLength);
        builder.Property(x => x.OutputHash).HasMaxLength(ProductionLimits.HashMaxLength + 8);
        builder.Property(x => x.Message).HasMaxLength(ProductionLimits.MessageMaxLength);
        builder.Property(x => x.RepairHint).HasMaxLength(ProductionLimits.MessageMaxLength);
        builder.Property(x => x.ErrorCode).HasMaxLength(ProductionLimits.ErrorCodeMaxLength);
        builder.Property(x => x.ErrorMessage).HasMaxLength(ProductionLimits.MessageMaxLength);
        builder.Property(x => x.CostUsd).HasPrecision(18, 6);
        builder.Ignore(x => x.IsDone);
        builder.HasIndex(x => new { x.JobId, x.Sequence });
        builder.HasIndex(x => x.CommandId);
    }
}
