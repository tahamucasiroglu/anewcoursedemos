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

    /// <summary>Gets yerel girişlerin sağlayıcı adı (spec 0006 Req 18; ör. "Yerel").</summary>
    public string LocalProviderName { get; init; } = "Yerel";

    /// <summary>Gets modeller.</summary>
    public IReadOnlyList<ModelEntry> Models { get; init; } = [];

    /// <summary>
    /// Gets katalogdan kaldırılan eski kimlik → yerine geçen güncel kimlik (spec 0026 Req 2). Eski kimlik listelenmez; geçmiş
    /// kayıtlardan (sabitlenmiş iş, sürüm tabanı) gelen çağrı yerine geçen girişe gider.
    /// </summary>
    public IReadOnlyDictionary<string, string> ReplacedModels { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Kimliğin girişini bulur: doğrudan eşleşme, yoksa yerine geçen giriş (spec 0026 Req 4).</summary>
    /// <param name="id">Model kimliği.</param>
    /// <returns>Giriş; bilinmiyorsa <c>null</c>.</returns>
    public ModelEntry? Find(string? id)
    {
        if (id is null)
        {
            return null;
        }

        return Models.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.Ordinal))
            ?? (ReplacedModels.TryGetValue(id, out var current)
                ? Models.FirstOrDefault(m => string.Equals(m.Id, current, StringComparison.Ordinal))
                : null);
    }
}
