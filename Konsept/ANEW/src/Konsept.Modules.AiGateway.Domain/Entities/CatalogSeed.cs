namespace Konsept.Modules.AiGateway.Domain.Entities;

/// <summary>
/// Sistemle gelen sürümlenmiş katalog (spec 0004 Req 5): sağlayıcılar, modeller ve key doğrulamasının en uzun bekleme süresi.
/// Değişikliği geliştirici yapar (patch sürüm); admin ekranı yoktur.
/// </summary>
public sealed class CatalogSeed
{
    /// <summary>Gets seed sürümü.</summary>
    public required string Version { get; init; }

    /// <summary>Gets key doğrulamasının en uzun beklemesi (saniye; spec 0004 Req 10: 10 sn).</summary>
    public double KeyValidationTimeoutSeconds { get; init; }

    /// <summary>Gets online sağlayıcılar.</summary>
    public IReadOnlyList<ProviderEntry> Providers { get; init; } = [];

    /// <summary>Gets modeller.</summary>
    public IReadOnlyList<ModelEntry> Models { get; init; } = [];
}
