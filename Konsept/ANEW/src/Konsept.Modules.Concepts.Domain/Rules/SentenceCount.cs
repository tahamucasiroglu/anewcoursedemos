using System.Text.RegularExpressions;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Cümle sayımı (spec 0014 Req 3, G-19): nokta, soru ve ünlem işaretleriyle. Bir işaret grubu (<c>?!</c>, <c>...</c>) ardından boşluk ya
/// da metin sonu geliyorsa cümle biter; rakamdan sonraki nokta ("1. Dünya Savaşı") sıra sayısıdır, cümle bitirmez. İşaretten sonra kalan
/// harfli metin de bir cümledir. Boş metin 0 cümledir.
/// </summary>
public static partial class SentenceCount
{
    /// <summary>Cümle sayısı.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Sayı.</returns>
    public static int Of(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return 0;
        }

        var count = 0;
        var last = 0;
        foreach (Match match in Terminator().Matches(value))
        {
            var isOrdinal = match.Value == "." && match.Index > 0 && char.IsDigit(value[match.Index - 1]);
            if (isOrdinal || !HasLetterOrDigit(value, last, match.Index))
            {
                continue;
            }

            count++;
            last = match.Index + match.Length;
        }

        return HasLetterOrDigit(value, last, value.Length) ? count + 1 : count;
    }

    private static bool HasLetterOrDigit(string value, int from, int to)
    {
        for (var i = from; i < to; i++)
        {
            if (char.IsLetterOrDigit(value[i]))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"[.!?…]+(?=\s|$)")]
    private static partial Regex Terminator();
}
