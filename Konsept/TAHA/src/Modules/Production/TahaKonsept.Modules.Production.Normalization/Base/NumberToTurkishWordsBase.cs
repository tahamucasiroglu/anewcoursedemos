using System.Text;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Sayı → Türkçe açık yazı ortak algoritması (üçlü gruplar + ölçek sözcükleri). Somut sınıf yalnız sözcük tablolarını verir
/// (<see cref="Ones"/>, <see cref="Tens"/>, <see cref="Scales"/>, <see cref="Hundred"/>, <see cref="Zero"/>, <see cref="Minus"/>).
/// Türkçe kuralları: "bir yüz" değil "yüz", "bir bin" değil "bin" (ama "bir milyon"); sıra sayısında son sözcük ünlü uyumuyla
/// -ıncı/-inci/-uncu/-üncü alır, "dört" → "dördüncü".
/// </summary>
public abstract class NumberToTurkishWordsBase : INumberToTurkishWords
{
    private const int MaxDigitsAsNumber = 18;
    private IReadOnlyCollection<string>? _vocabulary;

    /// <summary>Birler: indeks 0 boş, 1..9 → bir..dokuz.</summary>
    protected abstract IReadOnlyList<string> Ones { get; }

    /// <summary>Onlar: indeks 0 boş, 1..9 → on..doksan.</summary>
    protected abstract IReadOnlyList<string> Tens { get; }

    /// <summary>Ölçekler: indeks 0 boş, 1 → bin, 2 → milyon, 3 → milyar, 4 → trilyon …</summary>
    protected abstract IReadOnlyList<string> Scales { get; }

    /// <summary>"yüz".</summary>
    protected abstract string Hundred { get; }

    /// <summary>"sıfır".</summary>
    protected abstract string Zero { get; }

    /// <summary>"eksi".</summary>
    protected abstract string Minus { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Vocabulary => _vocabulary ??= BuildVocabulary();

    /// <inheritdoc />
    public string ToWords(long value)
    {
        if (value == 0)
        {
            return Zero;
        }

        var magnitude = value < 0 ? (ulong)(-(value + 1)) + 1UL : (ulong)value;
        var words = MagnitudeToWords(magnitude);
        return value < 0 ? Minus + " " + words : words;
    }

    /// <inheritdoc />
    public string ToWords(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
        {
            return digits;
        }

        if (digits.Length > MaxDigitsAsNumber)
        {
            return DigitByDigit(digits);
        }

        var leadingZeros = digits.Length == 1 ? 0 : digits.TakeWhile(c => c == '0').Count();
        if (leadingZeros == digits.Length)
        {
            return string.Join(' ', Enumerable.Repeat(Zero, leadingZeros));
        }

        var rest = ToWords(long.Parse(digits.AsSpan(leadingZeros), System.Globalization.CultureInfo.InvariantCulture));
        return leadingZeros == 0 ? rest : string.Join(' ', Enumerable.Repeat(Zero, leadingZeros)) + " " + rest;
    }

    /// <inheritdoc />
    public string ToDecimalWords(string integerDigits, string fractionDigits, string separatorWord)
    {
        ArgumentNullException.ThrowIfNull(integerDigits);
        ArgumentNullException.ThrowIfNull(fractionDigits);
        var integer = ToWords(integerDigits.Length == 0 ? "0" : integerDigits);
        if (fractionDigits.Length == 0)
        {
            return integer;
        }

        // Kesir: baştaki sıfırlar tek tek, kalanı sayı olarak (3,05 → üç virgül sıfır beş; 3,25 → üç virgül yirmi beş).
        return integer + " " + separatorWord + " " + ToWords(fractionDigits);
    }

    /// <inheritdoc />
    public string ToOrdinal(long value) => ToOrdinalWords(ToWords(value));

    /// <inheritdoc />
    public string ToOrdinalWords(string cardinalWords)
    {
        ArgumentNullException.ThrowIfNull(cardinalWords);
        var trimmed = cardinalWords.TrimEnd();
        if (trimmed.Length == 0)
        {
            return cardinalWords;
        }

        var split = trimmed.LastIndexOf(' ');
        var head = split < 0 ? string.Empty : trimmed[..(split + 1)];
        var last = split < 0 ? trimmed : trimmed[(split + 1)..];
        return head + OrdinalOfWord(last);
    }

    /// <summary>Tek sözcüğü sıra sayısına çevirir (üç → üçüncü, dört → dördüncü, altı → altıncı).</summary>
    /// <param name="word">Sayı sözcüğü.</param>
    /// <returns>Sıra sözcüğü.</returns>
    protected virtual string OrdinalOfWord(string word)
    {
        var lower = TurkishCulture.ToLower(word);
        var stem = lower.EndsWith("dört", StringComparison.Ordinal) ? word[..^1] + "d" : word;
        var vowel = TurkishCulture.FourWay(TurkishCulture.LastVowel(lower) ?? 'i');
        var endsWithVowel = TurkishCulture.IsVowel(lower[^1]);
        return endsWithVowel
            ? stem + "nc" + vowel
            : stem + vowel + "nc" + vowel;
    }

    /// <summary>0..999 aralığındaki grubu sözcüklere çevirir.</summary>
    /// <param name="group">Grup değeri.</param>
    /// <param name="parts">Sözcüklerin ekleneceği liste.</param>
    protected void AppendGroup(int group, List<string> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var hundreds = group / 100;
        var tens = group / 10 % 10;
        var ones = group % 10;
        if (hundreds > 0)
        {
            if (hundreds > 1)
            {
                parts.Add(Ones[hundreds]);
            }

            parts.Add(Hundred);
        }

        if (tens > 0)
        {
            parts.Add(Tens[tens]);
        }

        if (ones > 0)
        {
            parts.Add(Ones[ones]);
        }
    }

    private string MagnitudeToWords(ulong magnitude)
    {
        var groups = new List<int>();
        while (magnitude > 0)
        {
            groups.Add((int)(magnitude % 1000));
            magnitude /= 1000;
        }

        var parts = new List<string>();
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            var group = groups[i];
            if (group == 0)
            {
                continue;
            }

            // "bin" tek başına: 1000 → "bin", 21000 → "yirmi bir bin", 1.001.000 → "bir milyon bin".
            if (!(i == 1 && group == 1))
            {
                AppendGroup(group, parts);
            }

            if (i > 0)
            {
                parts.Add(Scales[i]);
            }
        }

        return string.Join(' ', parts);
    }

    private string DigitByDigit(string digits)
    {
        var sb = new StringBuilder();
        foreach (var c in digits)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            var d = c - '0';
            sb.Append(d == 0 ? Zero : Ones[d]);
        }

        return sb.ToString();
    }

    private IReadOnlyCollection<string> BuildVocabulary()
    {
        var set = new HashSet<string>(StringComparer.Ordinal) { Zero, Hundred };
        foreach (var w in Ones.Concat(Tens).Concat(Scales).Where(w => w.Length > 0))
        {
            set.Add(w);
        }

        return set;
    }
}
