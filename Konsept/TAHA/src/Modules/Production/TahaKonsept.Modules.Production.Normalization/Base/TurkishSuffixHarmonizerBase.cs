using System.Text;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Ek uyumu ortak algoritması. Adımlar: (1) kaynaştırma: ünsüzle biten gövdede yazılıştan gelen fazla "y"/"n" düşer
/// (kg'ye → kilograma), ünlüyle biten gövdede ünlüyle başlayan eke "y" (ilgi eki ise "n") eklenir; (2) ilk ünsüz benzeşmesi
/// d/t ve c/ç; (3) ünlüler sırayla iki yönlü (a/e) ve dört yönlü (ı/i/u/ü) uyuma sokulur; o/ö (-yor) korunur.
/// <see cref="Attach"/> ayrıca "dört" + ünlü → "dörd" yumuşamasını yapar. Kural istisnaları <see cref="AdjustStem"/> ile genişletilebilir.
/// </summary>
public abstract class TurkishSuffixHarmonizerBase : ITurkishSuffixHarmonizer
{
    /// <inheritdoc />
    public string HarmonizeSuffix(string stem, string suffix)
    {
        ArgumentNullException.ThrowIfNull(stem);
        ArgumentNullException.ThrowIfNull(suffix);
        if (suffix.Length == 0)
        {
            return suffix;
        }

        var s = TurkishCulture.ToLower(suffix);
        var lastLetter = TurkishCulture.LastLetter(stem);
        var stemEndsWithVowel = lastLetter is { } l && TurkishCulture.IsVowel(l);
        s = ApplyBuffer(s, stemEndsWithVowel);

        var previousVowel = TurkishCulture.LastVowel(stem) ?? 'e';
        var previousLetter = lastLetter ?? 'a';
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i == 0 && c is 'd' or 't')
            {
                c = TurkishCulture.IsVoiceless(previousLetter) ? 't' : 'd';
            }
            else if (i == 0 && c is 'c' or 'ç')
            {
                c = TurkishCulture.IsVoiceless(previousLetter) ? 'ç' : 'c';
            }
            else if (c is 'a' or 'e')
            {
                c = TurkishCulture.TwoWay(previousVowel);
            }
            else if (c is 'ı' or 'i' or 'u' or 'ü')
            {
                c = TurkishCulture.FourWay(previousVowel);
            }

            if (TurkishCulture.IsVowel(c))
            {
                previousVowel = c;
            }

            previousLetter = c;
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string Attach(string stem, string suffix)
    {
        ArgumentNullException.ThrowIfNull(stem);
        ArgumentNullException.ThrowIfNull(suffix);
        if (suffix.Length == 0)
        {
            return stem;
        }

        var harmonized = HarmonizeSuffix(stem, suffix);
        return AdjustStem(stem, harmonized) + harmonized;
    }

    /// <summary>Ek ünlüyle başlıyorsa gövdeyi yumuşatır (yalnız sayı sözcüğü "dört" → "dörd").</summary>
    /// <param name="stem">Gövde.</param>
    /// <param name="harmonizedSuffix">Uyumlu ek.</param>
    /// <returns>Gerekirse yumuşamış gövde.</returns>
    protected virtual string AdjustStem(string stem, string harmonizedSuffix)
    {
        ArgumentNullException.ThrowIfNull(stem);
        ArgumentNullException.ThrowIfNull(harmonizedSuffix);
        if (harmonizedSuffix.Length > 0
            && TurkishCulture.IsVowel(harmonizedSuffix[0])
            && TurkishCulture.ToLower(stem).EndsWith("dört", StringComparison.Ordinal)
            && (stem.Length == 4 || !char.IsLetter(stem[^5])))
        {
            return stem[..^1] + "d";
        }

        return stem;
    }

    private static string ApplyBuffer(string s, bool stemEndsWithVowel)
    {
        if (stemEndsWithVowel)
        {
            if (!TurkishCulture.IsVowel(s[0]))
            {
                return s;
            }

            // İlgi eki (-ın/-in/-un/-ün) "n", diğerleri (yönelme/belirtme) "y" kaynaştırması alır.
            var isGenitive = s.Length == 2 && s[1] == 'n';
            return (isGenitive ? "n" : "y") + s;
        }

        if (s.Length > 1 && s[0] == 'y' && TurkishCulture.IsVowel(s[1]))
        {
            return s[1..];
        }

        if (s.Length > 2 && s[0] == 'n' && TurkishCulture.IsVowel(s[1]) && s[2] == 'n')
        {
            return s[1..];
        }

        return s;
    }
}
