using System.Text.RegularExpressions;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Birebir tekrar anahtarı (BR-4, spec 0014 Req 4, 9): baş/son boşluk, tırnak ve bitiş noktalaması atılır, iç boşluklar teke indirilir,
/// dile göre küçük harfe çevrilir. "Vikingler İstanbul'da." ile "vikingler  istanbul'da" aynı anahtardır.
/// </summary>
public static partial class TitleKey
{
    /// <summary>Anahtar üretir.</summary>
    /// <param name="title">Başlık.</param>
    /// <param name="language">Profil dili.</param>
    /// <returns>Anahtar.</returns>
    public static string Of(string? title, string language)
    {
        var collapsed = Spaces().Replace(title ?? string.Empty, " ").Trim().Trim('"', '\'', '“', '”', '«', '»').TrimEnd('.', '!', '?', '…', ' ');
        return TextCase.Lower(collapsed, language);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
