using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Domain.Entities;

/// <summary>
/// Katalogdaki model (spec 0004 Req 4): sağlayıcı, ad, tür, efor seviyeleri, çıktı token limiti (yalnızca metin), çağrı zaman
/// aşımı ve USD birim fiyatı (metin: 1M giriş/çıkış token başına; görsel: görsel başına). Davranışa gömülü sabit yoktur; her
/// değer seed'dendir.
/// </summary>
public sealed class ModelEntry
{
    /// <summary>Gets kararlı kimlik (ör. <c>anthropic/claude-sonnet-4-5</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Gets bağlı olduğu sağlayıcının kimliği.</summary>
    public required string ProviderId { get; init; }

    /// <summary>Gets görünen ad.</summary>
    public required string Name { get; init; }

    /// <summary>Gets tür.</summary>
    public ModelKind Kind { get; init; }

    /// <summary>Gets desteklenen efor seviyeleri (low…max; boş olabilir).</summary>
    public IReadOnlyList<string> EffortLevels { get; init; } = [];

    /// <summary>Gets varsayılan efor seviyesi; desteklenmiyorsa <c>null</c>.</summary>
    public string? DefaultEffort { get; init; }

    /// <summary>Gets çıktı token limiti (yalnızca metin modelleri).</summary>
    public int? OutputTokenLimit { get; init; }

    /// <summary>Gets çağrı zaman aşımı (saniye).</summary>
    public int TimeoutSeconds { get; init; }

    /// <summary>Gets 1M giriş token'ı başına USD (metin).</summary>
    public decimal? InputPricePerMillionUsd { get; init; }

    /// <summary>Gets 1M çıkış token'ı başına USD (metin).</summary>
    public decimal? OutputPricePerMillionUsd { get; init; }

    /// <summary>Gets üretilen görsel başına USD (görsel).</summary>
    public decimal? PricePerImageUsd { get; init; }
}
