namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>Harf harf okunan kısaltmalar için Türkçe harf adları (A → a, B → be, C → ce, K → ke, Ğ → yumuşak ge, W → çift ve,
/// X → iks, Q → kü). Önemli üye: <see cref="Spell"/>.</summary>
public static class TurkishLetterNames
{
    private static readonly Dictionary<char, string> Names = new()
    {
        ['A'] = "a", ['B'] = "be", ['C'] = "ce", ['Ç'] = "çe", ['D'] = "de", ['E'] = "e", ['F'] = "fe", ['G'] = "ge",
        ['Ğ'] = "yumuşak ge", ['H'] = "he", ['I'] = "ı", ['İ'] = "i", ['J'] = "je", ['K'] = "ke", ['L'] = "le", ['M'] = "me",
        ['N'] = "ne", ['O'] = "o", ['Ö'] = "ö", ['P'] = "pe", ['Q'] = "kü", ['R'] = "re", ['S'] = "se", ['Ş'] = "şe",
        ['T'] = "te", ['U'] = "u", ['Ü'] = "ü", ['V'] = "ve", ['W'] = "çift ve", ['X'] = "iks", ['Y'] = "ye", ['Z'] = "ze",
    };

    /// <summary>Büyük harfli kısaltmayı harf adlarıyla okur ("TBMM" → "te be me me").</summary>
    /// <param name="acronym">Büyük harfli kısaltma.</param>
    /// <returns>Harf adları (boşlukla ayrılmış).</returns>
    public static string Spell(string acronym)
    {
        ArgumentNullException.ThrowIfNull(acronym);
        return string.Join(' ', acronym.Select(c => Names.TryGetValue(c, out var n) ? n : c.ToString()));
    }
}
