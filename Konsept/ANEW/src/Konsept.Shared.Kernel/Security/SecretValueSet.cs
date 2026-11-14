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
        if (value.Length < MinLength || _values.Count >= MaxValues || value == replacement
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
            text = ReplaceAllForms(text, value, replacement);
        }

        return text;
    }

    /// <summary>Değeri ham, URL kodlu ve JSON kaçışlı biçimleriyle metinden siler.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="value">Değer.</param>
    /// <param name="replacement">Yerine yazılan metin.</param>
    /// <returns>Yeni metin.</returns>
    public static string ReplaceAllForms(string text, string value, string replacement)
    {
        text = text.Replace(value, replacement, StringComparison.Ordinal);
        var url = Uri.EscapeDataString(value);
        if (url != value)
        {
            text = text.Replace(url, replacement, StringComparison.OrdinalIgnoreCase);
        }

        var json = JsonEncodedText.Encode(value).Value;
        if (json != value)
        {
            text = text.Replace(json, replacement, StringComparison.Ordinal);
        }

        return text;
    }
}
