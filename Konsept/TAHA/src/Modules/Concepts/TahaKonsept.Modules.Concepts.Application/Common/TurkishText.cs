using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TahaKonsept.Modules.Concepts.Application.Common;

/// <summary>
/// Türkçe metin yardımcıları (konu parmak izi ve kelime tekrar analizi ortak kullanır): <see cref="Fold"/> (tr-TR küçük harf +
/// aksan katlama: ç→c, ğ→g, ı→i, ö→o, ş→s, ü→u), <see cref="Words"/> (kesme işaretli ekleri atarak kelimelere böler),
/// <see cref="CountWords"/>.
/// </summary>
public static partial class TurkishText
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Küçük harfe çevirir ve Türkçe aksanları katlar.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Katlanmış metin.</returns>
    public static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var lower = text.ToLower(Tr);
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            sb.Append(ch switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                'â' => 'a',
                'î' => 'i',
                'û' => 'u',
                _ => ch,
            });
        }

        return sb.ToString();
    }

    /// <summary>Metni kelimelere böler (kesme işaretinden sonraki ek atılır: "Bizans'ın" → "Bizans"). Büyük/küçük harf korunur.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Kelimeler.</returns>
    public static IEnumerable<string> Words(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var stripped = ApostropheSuffixRegex().Replace(text, string.Empty);
        foreach (Match m in WordRegex().Matches(stripped))
        {
            yield return m.Value;
        }
    }

    /// <summary>Kelime sayısı.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Sayı.</returns>
    public static int CountWords(string? text) => Words(text).Count();

    [GeneratedRegex(@"['’`]\p{L}+")]
    private static partial Regex ApostropheSuffixRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRegex();
}
