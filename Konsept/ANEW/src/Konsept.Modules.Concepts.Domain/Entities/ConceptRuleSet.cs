namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Konsept kurallarının yapılandırma değerleri (spec 0014 Req 5, 11, 14; kodda sabit değil, gömülü <c>concept-rules.json</c>'dan):
/// talimattaki birebir başlık sınırı, eski başlık özetindeki kelime sayısı, süre toleransı, kelime denetimi eşikleri ve dile göre durak
/// kelimeler.
/// </summary>
public sealed record ConceptRuleSet
{
    /// <summary>Gets talimata birebir giren en yeni başlık sayısı (200).</summary>
    public int TitleHistoryLimit { get; init; }

    /// <summary>Gets eski başlıkların özetindeki en sık kelime sayısı.</summary>
    public int HistorySummaryKeywords { get; init; }

    /// <summary>Gets süre toleransı (0,15 = ±%15).</summary>
    public decimal DurationTolerance { get; init; }

    /// <summary>Gets kelime denetimi: en az tekrar sayısı (5).</summary>
    public int WordRepeatMinCount { get; init; }

    /// <summary>Gets kelime denetimi: toplam kelimeye oran eşiği (0,02 = %2; aşılmalı).</summary>
    public decimal WordRepeatMinShare { get; init; }

    /// <summary>Gets dile göre durak kelimeler (küçük harf).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> StopWords { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    /// <summary>Dilin durak kelimeleri.</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Küme.</returns>
    public IReadOnlySet<string> StopWordsFor(string language) =>
        StopWords.TryGetValue(language, out var words)
            ? new HashSet<string>(words, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
}
