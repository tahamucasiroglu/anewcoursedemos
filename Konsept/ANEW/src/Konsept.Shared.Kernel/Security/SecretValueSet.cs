using System.Text;
using System.Text.Json;

namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Bir maskeleme çağrısında gizlenen değerler (hunt round 3, H3-C/H3-H). Değer metnin başka bir yerinde etiketsiz,
/// URL kodlu ya da JSON kaçışlı geçse de aynı biçimde gizlenir; kodlu metnin çözülmüş halinde bulunan değerler de
/// ham metne buradan uygulanır.
/// </summary>
internal sealed class SecretValueSet
{
    /// <summary>Kaydedilecek en kısa değer; daha kısası metnin geneline uygulanmaz (yanlış eşleşme).</summary>
    public const int MinLength = 6;

    /// <summary>Kaydedilecek en uzun değer; daha uzunu (ör. kapanmamış blok) zaten yerinde maskelidir.</summary>
    public const int MaxLength = 1024;

    private const int MaxValues = 256;

    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    /// <summary>Kayıtlı değerler.</summary>
    public IEnumerable<string> Values => _values.Keys;

    /// <summary>Gizlenen bir değeri kaydeder.</summary>
    /// <param name="value">Ham değer.</param>
    /// <param name="replacement">Yerine yazılan metin.</param>
    public void Add(string value, string replacement)
    {
        value = value.Trim();
        if (value.Length < MinLength || value.Length > MaxLength || _values.Count >= MaxValues || value == replacement
            || value.Contains(SecretMasker.Hidden, StringComparison.Ordinal)
            || value.Contains(SecretMasker.JwtPlaceholder, StringComparison.Ordinal)
            || value.Contains('…', StringComparison.Ordinal))
        {
            return;
        }

        _values.TryAdd(value, replacement);
    }

    /// <summary>Kayıtlı değerleri (ham, URL kodlu, JSON kaçışlı biçimleriyle) metne uygular; uzun olan önce.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Maskelenmiş metin.</returns>
    public string ApplyTo(string text)
    {
        foreach (var (value, replacement) in _values.OrderByDescending(v => v.Key.Length))
        {
            // Yalnızca harften oluşan değer ("Password", "Sunshine") yalnızca tam sözcük olarak yayılır: "PasswordPolicy"
            // gibi tanımlayıcılar bozulmaz, etiketsiz tekrar yine gizlenir (hunt r10/r11).
            if (value.All(char.IsLetter))
            {
                text = ReplaceWholeWord(text, value, replacement);
                continue;
            }

            foreach (var form in Forms(value))
            {
                text = text.Replace(form, replacement, StringComparison.Ordinal);
            }
        }

        return text;
    }

    /// <summary>Konumdan hemen önce (en fazla 9 karakter) yalnızca onaltılık rakam ve '%' içeren bir kaçış dizisi var mı.</summary>
    private static bool AfterPercentEscape(string text, int index)
    {
        for (var p = index - 1; p >= 0 && index - p <= 9; p--)
        {
            if (text[p] == '%')
            {
                return true;
            }

            if (!char.IsAsciiHexDigit(text[p]))
            {
                return false;
            }
        }

        return false;
    }

    private static string ReplaceWholeWord(string text, string word, string replacement)
    {
        var index = text.IndexOf(word, StringComparison.Ordinal);
        if (index < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var copied = 0;
        while (index >= 0)
        {
            var end = index + word.Length;
            var boundaryBefore = index == 0 || !char.IsLetterOrDigit(text[index - 1]) || AfterPercentEscape(text, index); // %3D, %253D…
            var boundaryAfter = end == text.Length || !char.IsLetterOrDigit(text[end]);
            if (boundaryBefore && boundaryAfter)
            {
                builder.Append(text, copied, index - copied).Append(replacement);
                copied = end;
            }

            index = text.IndexOf(word, end, StringComparison.Ordinal);
        }

        return copied == 0 ? text : builder.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// Değerin metinde aranacak biçimleri: ham, URL kodlu (büyük ve küçük harfli onaltılık), JSON kaçışlı. Geçersiz
    /// UTF-16 (yarım vekil çift) içeren değerin kodlu biçimi hesaplanmaz (review r4, M-1: istisna atmasın).
    /// </summary>
    /// <param name="value">Değer.</param>
    /// <returns>Farklı biçimler.</returns>
    public static string[] Forms(string value)
    {
        if (!IsValidUtf16(value))
        {
            return [value];
        }

        var url = Uri.EscapeDataString(value);
        return [.. new HashSet<string>(StringComparer.Ordinal) { value, url, LowerHex(url), JsonEncodedText.Encode(value).Value }];
    }

    private static bool IsValidUtf16(string value)
    {
        var span = value.AsSpan();
        while (!span.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(span, out _, out var consumed) != System.Buffers.OperationStatus.Done)
            {
                return false;
            }

            span = span[consumed..];
        }

        return true;
    }

    private static string LowerHex(string url)
    {
        var chars = url.ToCharArray();
        for (var i = 0; i + 2 < chars.Length; i++)
        {
            if (chars[i] == '%')
            {
                chars[i + 1] = char.ToLowerInvariant(chars[i + 1]);
                chars[i + 2] = char.ToLowerInvariant(chars[i + 2]);
            }
        }

        return new string(chars);
    }
}
