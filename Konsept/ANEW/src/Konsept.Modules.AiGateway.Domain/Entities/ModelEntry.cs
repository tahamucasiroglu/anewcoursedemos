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

    /// <summary>Gets sağlayıcı API'sindeki model adı (ör. <c>claude-sonnet-4-5</c>); yoksa kimliğin ilk <c>/</c>'den sonrası.</summary>
    public string? ApiModel { get; init; }

    /// <summary>Gets sağlayıcı API'sine gönderilecek model adı.</summary>
    public string ProviderModelName => ApiModel ?? (Id.IndexOf('/', StringComparison.Ordinal) is var i and >= 0 ? Id[(i + 1)..] : Id);

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

    /// <summary>Gets a value indicating whether giriş yerel mi (spec 0006 Req 18: ajanda çalışır, API key ve birim fiyat yok).</summary>
    public bool Local { get; init; }

    /// <summary>Gets yalnızca desteklenen dil (<c>tr</c> · <c>en</c>); çok dilliyse <c>null</c> (spec 0006 Req 3).</summary>
    public string? LanguageOnly { get; init; }

    /// <summary>Gets a value indicating whether "Lisans uyarısı" etiketi taşır (spec 0006 Req 2, Remotion).</summary>
    public bool LicenseWarning { get; init; }

    /// <summary>Gets girişin bağlı olduğu ajan bileşeni (ör. <c>remotion</c>); yoksa <c>null</c> (spec 0006 Req 19).</summary>
    public string? Component { get; init; }
}
