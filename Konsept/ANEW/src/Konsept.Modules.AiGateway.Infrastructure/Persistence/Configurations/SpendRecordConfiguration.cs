using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="SpendRecord"/> eşlemesi (<c>ai_gateway.spend_records</c>, spec 0013). Maliyet <c>numeric(20,10)</c>: sentin kesirleri
/// korunur (E-3; tek token'ın maliyeti 10⁻⁶ USD'nin altında olabilir). Aylık toplam için <c>(user_id, occurred_at)</c> dizini.
/// Key tablosuna yabancı anahtar yoktur: key silinse de kayıt kalır (Req 14).
/// </summary>
public sealed class SpendRecordConfiguration : IEntityTypeConfiguration<SpendRecord>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "spend_records";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SpendRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(r => r.ProviderId).HasColumnName("provider_id").HasMaxLength(64).IsRequired();
        builder.Property(r => r.ModelId).HasColumnName("model_id").HasMaxLength(128).IsRequired();
        builder.Property(r => r.Effort).HasColumnName("effort").HasMaxLength(16);
        builder.Property(r => r.Purpose).HasColumnName("purpose").HasMaxLength(32).HasConversion(v => ToText(v), v => ToPurpose(v)).IsRequired();
        builder.Property(r => r.ProfileId).HasColumnName("profile_id");
        builder.Property(r => r.InputTokens).HasColumnName("input_tokens");
        builder.Property(r => r.OutputTokens).HasColumnName("output_tokens");
        builder.Property(r => r.ImageCount).HasColumnName("image_count");
        builder.Property(r => r.Outcome).HasColumnName("outcome").HasMaxLength(16).HasConversion(v => ToText(v), v => ToOutcome(v)).IsRequired();
        builder.Property(r => r.CostUsd).HasColumnName("cost_usd").HasPrecision(20, 10).IsRequired();
        builder.Property(r => r.IsEstimated).HasColumnName("is_estimated").IsRequired();
        builder.HasIndex(r => new { r.UserId, r.OccurredAt }).HasDatabaseName("ix_spend_records_user_time");
    }

    private static string ToText(CallPurpose purpose) => purpose switch
    {
        CallPurpose.ConceptGeneration => "concept",
        CallPurpose.Review => "review",
        CallPurpose.TopicSuggestion => "topic",
        CallPurpose.VideoCode => "video_code",
        CallPurpose.Image => "image",
        CallPurpose.Voice => "voice",
        _ => "trial",
    };

    private static CallPurpose ToPurpose(string value) => value switch
    {
        "concept" => CallPurpose.ConceptGeneration,
        "review" => CallPurpose.Review,
        "topic" => CallPurpose.TopicSuggestion,
        "video_code" => CallPurpose.VideoCode,
        "image" => CallPurpose.Image,
        "voice" => CallPurpose.Voice,
        _ => CallPurpose.Trial,
    };

    private static string ToText(CallOutcome outcome) => outcome switch
    {
        CallOutcome.Success => "success",
        CallOutcome.Truncated => "truncated",
        CallOutcome.ProviderError => "error",
        _ => "no_response",
    };

    private static CallOutcome ToOutcome(string value) => value switch
    {
        "success" => CallOutcome.Success,
        "truncated" => CallOutcome.Truncated,
        "error" => CallOutcome.ProviderError,
        _ => CallOutcome.NoResponse,
    };
}
