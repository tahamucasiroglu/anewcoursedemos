using System.Text.RegularExpressions;

namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Gizli bilgi maskelemesinin tek mekanizması (spec 0008 Req 8–10, H-2). Teknik log, olay kaydı, ajan özeti ve
/// sonraki her kayıt buradan geçer. Desen ve alan tablosu plan 0008'deki test tablosudur; yeni bir gizli alan
/// ekleyen spec bu tabloya ve testine satır ekler.
/// </summary>
public static partial class SecretMasker
{
    /// <summary>Tamamen gizlenen değerin yerine yazılan metin.</summary>
    public const string Hidden = "***";

    /// <summary>JWT biçimli token'ın yerine yazılan metin.</summary>
    public const string JwtPlaceholder = "[jwt]";

    private const string Ellipsis = "…";

    private static readonly string[] KnownKeyPrefixes = ["sk-ant-", "sk-or-", "sk-"];

    private static readonly string[] HiddenFieldSuffixes =
        ["password", "passwd", "secret", "token", "pairingcode", "servicekey", "privatekey", "credential", "credentials"];

    /// <summary>Serbest metindeki bilinen gizli bilgi biçimlerini maskeler.</summary>
    /// <param name="text">Metin (hata mesajı, kullanıcı metni, log satırı).</param>
    /// <returns>Maskelenmiş metin; <paramref name="text"/> boşsa aynen döner.</returns>
    public static string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var result = JwtPattern().Replace(text, JwtPlaceholder);
        result = BearerPattern().Replace(result, m => m.Groups["token"].Value == JwtPlaceholder ? m.Value : $"{m.Groups["scheme"].Value} {Hidden}");
        result = ApiKeyPattern().Replace(result, m => MaskApiKey(m.Value));
        result = MaskKeyValues(result);
        return result;
    }

    /// <summary>Alan adı gizli bir bilgiyi mi taşıyor (şifre, API key, token, eşleştirme kodu/sırrı, bağlantı dizesi).</summary>
    /// <param name="fieldName">Alan ya da özellik adı (büyük/küçük harf, <c>_</c> ve <c>-</c> önemsiz).</param>
    /// <returns>Gizli alansa <c>true</c>.</returns>
    public static bool IsSecretField(string? fieldName)
    {
        var name = Normalize(fieldName);
        return name.Length > 0 && (IsApiKeyField(name) || IsConnectionStringField(name) || IsHiddenField(name));
    }

    /// <summary>Adlandırılmış bir alanın değerini alan türüne göre maskeler; gizli olmayan alanda serbest metin maskelemesi uygular.</summary>
    /// <param name="fieldName">Alan adı.</param>
    /// <param name="value">Değer.</param>
    /// <returns>Maskelenmiş değer. Şifre ve token gibi alanlar hiçbir biçimde (uzunluk, önek) sızmaz.</returns>
    public static string MaskField(string? fieldName, string? value)
    {
        var name = Normalize(fieldName);
        if (IsApiKeyField(name))
        {
            return string.IsNullOrEmpty(value) ? Hidden : MaskApiKey(value);
        }

        if (IsConnectionStringField(name))
        {
            return Mask(value);
        }

        return IsHiddenField(name) ? Hidden : Mask(value);
    }

    /// <summary>API key'i önek + son 3 karakter biçiminde maskeler (<c>sk-…a3f</c>).</summary>
    /// <param name="key">API key.</param>
    /// <returns>Maskelenmiş key; çok kısa key tamamen gizlenir.</returns>
    public static string MaskApiKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var prefix = KnownKeyPrefixes.FirstOrDefault(p => key.StartsWith(p, StringComparison.Ordinal)) ?? string.Empty;
        var body = key.Length - prefix.Length;
        return body < 8 ? Hidden : string.Concat(prefix, Ellipsis, key.AsSpan(key.Length - 3));
    }

    private static bool IsApiKeyField(string name) => name.EndsWith("apikey", StringComparison.Ordinal);

    private static bool IsConnectionStringField(string name) => name.EndsWith("connectionstring", StringComparison.Ordinal);

    private static bool IsHiddenField(string name) =>
        name == "pwd" || HiddenFieldSuffixes.Any(s => name.EndsWith(s, StringComparison.Ordinal));

    private static string Normalize(string? fieldName) =>
        fieldName is null
            ? string.Empty
            : fieldName.Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]*", RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"(?<scheme>\bBearer)\s+(?<token>[A-Za-z0-9._~+/=\-\[\]]{8,})", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();

    /// <summary>
    /// Serbest metindeki <c>anahtar=değer</c> / <c>anahtar: değer</c> çiftinde anahtar gizli bir alansa değeri maskeler
    /// (review round 1, M-1): kayıt <c>ToString</c> (<c>NewPassword = x</c>), JSON (<c>"password":"x"</c>), YAML
    /// (<c>password: x</c>), bağlantı dizesi (<c>Pwd=x</c>), sorgu dizesi (<c>access_token=x</c>), tırnaklı değer. Karar
    /// alan tablosuyla aynıdır (<see cref="IsSecretField"/>); tek kaynak. Gizli olmayan bir anahtarın eşleşmesi metni
    /// tüketmez: arama o anahtarın hemen ardından sürer (<c>Exception: Password=x</c> içindeki şifre de bulunur).
    /// </summary>
    /// <param name="text">Metin.</param>
    /// <returns>Maskelenmiş metin.</returns>
    private static string MaskKeyValues(string text)
    {
        var output = new System.Text.StringBuilder(text.Length);
        var copied = 0;
        var match = KeyValuePattern().Match(text);
        while (match.Success)
        {
            var key = match.Groups["key"];
            var value = match.Groups["val"];
            var name = Normalize(key.Value);
            if ((IsApiKeyField(name) || IsHiddenField(name)) && value.Length > 0)
            {
                // Önceki adımda maskelenmiş key (sk-…xyz) olduğu gibi kalır.
                var masked = IsApiKeyField(name)
                    ? value.Value.Contains(Ellipsis, StringComparison.Ordinal) || value.Value == Hidden ? value.Value : MaskApiKey(value.Value)
                    : Hidden;
                output.Append(text, copied, value.Index - copied).Append(masked);
                copied = value.Index + value.Length;
                match = KeyValuePattern().Match(text, copied);
            }
            else
            {
                match = KeyValuePattern().Match(text, key.Index + key.Length);
            }
        }

        return copied == 0 ? text : output.Append(text, copied, text.Length - copied).ToString();
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9])sk-[A-Za-z0-9_\-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(
        @"(?<![\w\-])(?<key>[A-Za-z][\w\-]*)[""']?\s*[:=]\s*(?:""(?<val>[^""]*)""|'(?<val>[^']*)'|(?<val>[^;,&\s""'}\)\]]+))",
        RegexOptions.CultureInvariant)]
    private static partial Regex KeyValuePattern();
}
