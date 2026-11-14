using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Concept"/> tablo eşlemesi: belge <c>jsonb</c> (ortak JSON ayarlarıyla; değişiklik JSON karşılaştırmasıyla izlenir),
/// durum/kaynak metin, maliyet numeric(12,6), (<c>user_id</c>, <c>status</c>) indeksi.
/// </summary>
public sealed class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("concepts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Origin).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(ConceptLimits.TitleMaxLength);
        builder.Property(c => c.ModelKey).IsRequired().HasMaxLength(ConceptLimits.ModelKeyMaxLength);
        builder.Property(c => c.CostUsd).HasPrecision(12, 6);
        builder.Ignore(c => c.CanPurge);

        var comparer = new ValueComparer<ConceptDocument>(
            (a, b) => Serialize(a) == Serialize(b),
            d => Serialize(d).GetHashCode(StringComparison.Ordinal),
            d => Deserialize(Serialize(d)));
        builder.Property(c => c.Document)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(d => Serialize(d), s => Deserialize(s), comparer);

        builder.HasIndex(c => new { c.UserId, c.Status });
    }

    private static string Serialize(ConceptDocument? document) => JsonSerializer.Serialize(document ?? new ConceptDocument(), ContractJson.Options);

    private static ConceptDocument Deserialize(string json) =>
        string.IsNullOrWhiteSpace(json) ? new ConceptDocument() : JsonSerializer.Deserialize<ConceptDocument>(json, ContractJson.Options) ?? new ConceptDocument();
}
