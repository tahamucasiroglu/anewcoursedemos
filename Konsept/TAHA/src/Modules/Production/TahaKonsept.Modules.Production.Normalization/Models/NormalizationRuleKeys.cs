namespace TahaKonsept.Modules.Production.Normalization.Models;

/// <summary>Kural anahtarları (<see cref="NormalizationChange.Rule"/> değerleri): lexicon, date, roman, ordinal, number,
/// abbreviation, symbol.</summary>
public static class NormalizationRuleKeys
{
    /// <summary>Okunuş sözlüğü.</summary>
    public const string Lexicon = "lexicon";

    /// <summary>Tarih / dönem (MÖ, MS) / saat.</summary>
    public const string Date = "date";

    /// <summary>Romen rakamı.</summary>
    public const string Roman = "roman";

    /// <summary>Sıra sayısı (3. → üçüncü).</summary>
    public const string Ordinal = "ordinal";

    /// <summary>Sayı (yüzde, para birimi, aralık, ekler).</summary>
    public const string Number = "number";

    /// <summary>Kısaltma / birim / harf harf okunan kısaltma.</summary>
    public const string Abbreviation = "abbreviation";

    /// <summary>Sembol (&amp;, +, =, /, ×).</summary>
    public const string Symbol = "symbol";
}
