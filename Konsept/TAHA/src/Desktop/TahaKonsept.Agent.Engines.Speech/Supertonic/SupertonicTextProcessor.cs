using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TahaKonsept.Agent.Engines.Speech.Supertonic;

/// <summary>
/// Supertonic metin işleyicisi (resmi örnek kodun C# karşılığı): NFKD normalleştirme, emoji/simge temizliği, tırnak/tire
/// sadeleştirme, noktalama boşluk düzeltmesi, sonda noktalama yoksa nokta ekleme ve <c>&lt;lang&gt;…&lt;/lang&gt;</c> sarma
/// (<see cref="Preprocess"/>). <see cref="Encode"/> her UTF-16 karakteri <c>unicode_indexer.json</c> ile kimliğe çevirir;
/// sözlükte olmayan karakterler atılır. Türkçe harfler NFKD sonrası taban harf + birleşik işaret (ş → s + U+0327) olur; model bunu bekler.
/// </summary>
internal sealed partial class SupertonicTextProcessor
{
    private static readonly (string From, string To)[] Replacements =
    [
        ("–", "-"), ("‑", "-"), ("—", "-"), ("_", " "),
        ("“", "\""), ("”", "\""), ("‘", "'"), ("’", "'"), ("´", "'"), ("`", "'"),
        ("[", " "), ("]", " "), ("|", " "), ("/", " "), ("#", " "), ("→", " "), ("←", " "),
    ];

    private readonly long[] _indexer;

    /// <summary>Kimlik tablosuyla kurar.</summary>
    /// <param name="indexer">Kod noktası → kimlik (negatif = bilinmiyor).</param>
    public SupertonicTextProcessor(long[] indexer)
    {
        _indexer = indexer;
    }

    /// <summary><c>unicode_indexer.json</c> dosyasından yükler.</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <returns>İşleyici.</returns>
    public static SupertonicTextProcessor Load(string path) =>
        new(JsonSerializer.Deserialize<long[]>(File.ReadAllText(path)) ?? throw new InvalidDataException("unicode_indexer.json okunamadı."));

    /// <summary>Metni modelin beklediği biçime getirir ve dil etiketiyle sarar.</summary>
    /// <param name="text">Ham metin.</param>
    /// <param name="language">Dil kodu (ör. <c>tr</c>).</param>
    /// <returns>Ön işlenmiş metin (ör. <c>&lt;tr&gt;Merhaba.&lt;/tr&gt;</c>).</returns>
    public static string Preprocess(string text, string language)
    {
        text = text.Normalize(NormalizationForm.FormKD);
        text = RemoveEmojis(text);
        foreach (var (from, to) in Replacements)
        {
            text = text.Replace(from, to, StringComparison.Ordinal);
        }

        text = SpecialSymbols().Replace(text, string.Empty);
        text = text.Replace("@", " at ", StringComparison.Ordinal)
                   .Replace("e.g.,", "for example, ", StringComparison.Ordinal)
                   .Replace("i.e.,", "that is, ", StringComparison.Ordinal);
        text = SpaceBeforePunctuation().Replace(text, "$1");

        while (text.Contains("\"\"", StringComparison.Ordinal))
        {
            text = text.Replace("\"\"", "\"", StringComparison.Ordinal);
        }

        while (text.Contains("''", StringComparison.Ordinal))
        {
            text = text.Replace("''", "'", StringComparison.Ordinal);
        }

        text = Whitespace().Replace(text, " ").Trim();
        if (!EndsWithPunctuation().IsMatch(text))
        {
            text += ".";
        }

        return $"<{language}>{text}</{language}>";
    }

    /// <summary>Metni ön işler ve kimlik dizisine çevirir.</summary>
    /// <param name="text">Ham metin.</param>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Kimlikler (uzunluk = maske uzunluğu).</returns>
    public long[] Encode(string text, string language)
    {
        var processed = Preprocess(text, language);
        var ids = new List<long>(processed.Length);
        foreach (var ch in processed)
        {
            var codePoint = (int)ch;
            if (codePoint < _indexer.Length && _indexer[codePoint] >= 0)
            {
                ids.Add(_indexer[codePoint]);
            }
        }

        return ids.ToArray();
    }

    private static string RemoveEmojis(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            var cp = rune.Value;
            var isEmoji = cp is >= 0x1F600 and <= 0x1F64F or >= 0x1F300 and <= 0x1F5FF or >= 0x1F680 and <= 0x1F6FF
                or >= 0x1F700 and <= 0x1F77F or >= 0x1F780 and <= 0x1F7FF or >= 0x1F800 and <= 0x1F8FF
                or >= 0x1F900 and <= 0x1F9FF or >= 0x1FA00 and <= 0x1FA6F or >= 0x1FA70 and <= 0x1FAFF
                or >= 0x2600 and <= 0x26FF or >= 0x2700 and <= 0x27BF or >= 0x1F1E6 and <= 0x1F1FF;
            if (!isEmoji)
            {
                builder.Append(rune.ToString());
            }
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"[♥☆♡©\\]")]
    private static partial Regex SpecialSymbols();

    [GeneratedRegex(@" ([,.!?;:'])")]
    private static partial Regex SpaceBeforePunctuation();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("[.!?;:,'\"“”‘’)\\]}…。」』】〉》›»]$")]
    private static partial Regex EndsWithPunctuation();
}
