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

    /// <summary>Maskelenecek en uzun metin; daha uzunu önce kırpılır.</summary>
    public const int MaxInputLength = 64 * 1024;

    /// <summary>Kırpılan metnin sonuna eklenen işaret.</summary>
    public const string TruncatedMarker = "…[kırpıldı]";

    private const string Ellipsis = "…";

    private static readonly string[] KnownKeyPrefixes = ["sk-ant-", "sk-or-", "sk-", "AIza"];

    private static readonly string[] HiddenFieldSuffixes =
    [
        "password", "passwords", "passwd", "secret", "secrets", "token", "pairingcode", "servicekey", "privatekey",
        "credential", "credentials", "secretkey", "accesskey", "signingkey", "accountkey", "authorization", "cookie",
        "passphrase", "pwd",
    ];

    private static readonly string[] HiddenFieldStems = ["sifre", "şifre", "parola"];

    /// <summary>Serbest metindeki bilinen gizli bilgi biçimlerini maskeler.</summary>
    /// <param name="text">Metin (hata mesajı, kullanıcı metni, log satırı).</param>
    /// <returns>Maskelenmiş metin; <paramref name="text"/> boşsa aynen döner.</returns>
    public static string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        // Üst sınır: maskeleme maliyeti sınırlı kalsın (review round 3, M-1); fazlası atılır, işaretlenir.
        // Kesim noktası son boşluğa çekilir (en fazla 256 karakter): yarım token/URL sızmasın (hunt r2).
        if (text.Length > MaxInputLength)
        {
            var cut = text.LastIndexOfAny([' ', '\t', '\r', '\n'], MaxInputLength - 1, 256);
            text = string.Concat(text.AsSpan(0, cut > 0 ? cut : MaxInputLength - 256), TruncatedMarker);
        }

        var result = JwtPattern().Replace(text, JwtPlaceholder);
        result = BearerPattern().Replace(result, m => m.Groups["token"].Value == JwtPlaceholder ? m.Value : $"{m.Groups["scheme"].Value} {Hidden}");
        result = MaskUrlUserInfo(result);
        result = ApiKeyPattern().Replace(result, m => MaskApiKey(m.Value));
        result = GoogleApiKeyPattern().Replace(result, m => MaskApiKey(m.Value));
        result = SecretKeyValueScanner.Mask(result);
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

    /// <summary>Değer zaten maskelenmiş bir API key mi (<c>sk-…xyz</c>, <c>…xyz</c>).</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Maskeliyse <c>true</c>.</returns>
    internal static bool IsMaskedApiKey(string value) => AlreadyMaskedKey().IsMatch(value);

    /// <summary>Alan adını karşılaştırma için sadeleştirir (<c>_</c>/<c>-</c> atılır, küçük harf).</summary>
    /// <param name="fieldName">Alan adı.</param>
    /// <returns>Sade ad.</returns>
    internal static string NormalizeFieldName(string? fieldName) => Normalize(fieldName);

    /// <summary>Sade ad bir API key alanı mı.</summary>
    /// <param name="name">Sade ad.</param>
    /// <returns>API key alanıysa <c>true</c>.</returns>
    internal static bool IsApiKeyFieldName(string name) => IsApiKeyField(name);

    /// <summary>Sade ad tamamen gizlenen bir alan mı (şifre, token, sır…).</summary>
    /// <param name="name">Sade ad.</param>
    /// <returns>Gizliyse <c>true</c>.</returns>
    internal static bool IsHiddenFieldName(string name) => IsHiddenField(name);

    /// <summary>Sade ad bir bağlantı dizesi alanı mı.</summary>
    /// <param name="name">Sade ad.</param>
    /// <returns>Bağlantı dizesiyse <c>true</c>.</returns>
    internal static bool IsConnectionStringFieldName(string name) => IsConnectionStringField(name);

    private static bool IsApiKeyField(string name) =>
        name.EndsWith("apikey", StringComparison.Ordinal) || name.EndsWith("apikeys", StringComparison.Ordinal);

    private static bool IsConnectionStringField(string name) => name.EndsWith("connectionstring", StringComparison.Ordinal);

    private static bool IsHiddenField(string name) =>
        HiddenFieldSuffixes.Any(s => name.EndsWith(s, StringComparison.Ordinal))
        || name.StartsWith("password", StringComparison.Ordinal) // password_confirmation, passwordHash…
        || HiddenFieldStems.Any(s => name.Contains(s, StringComparison.Ordinal)); // şifreniz, parolası, yeniŞifre…

    private static string Normalize(string? fieldName) =>
        fieldName is null
            ? string.Empty
            : fieldName.Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace('İ', 'i') // Türkçe büyük İ: "ŞİFRE" → "şifre" (invariant küçültme İ'yi değiştirmez).
                .ToLowerInvariant()
                .Replace("̇", string.Empty, StringComparison.Ordinal);

    [GeneratedRegex(@"(?:(?<=\\u[0-9A-Fa-f]{4})|(?<!(?<!\\)[A-Za-z0-9_]))eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]*", RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"(?<scheme>\bBearer)[ \t]+(?<token>[^\s""',;]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();

    /// <summary>
    /// URL'deki kullanıcı bilgisinin şifresini maskeler: <c>postgres://user:pw@host</c> → <c>postgres://user:***@host</c>;
    /// yalnızca token varsa (<c>https://TOKEN@host</c>) tamamı (hunt: URI biçimli bağlantı dizeleri, git uzak adresleri).
    /// Her <c>://</c> için en fazla 512 karakter taranır.
    /// </summary>
    /// <param name="text">Metin.</param>
    /// <returns>Maskelenmiş metin.</returns>
    private static string MaskUrlUserInfo(string text)
    {
        var index = text.IndexOf("://", StringComparison.Ordinal);
        if (index < 0)
        {
            return text;
        }

        var output = new System.Text.StringBuilder(text.Length);
        var copied = 0;
        while (index >= 0)
        {
            var start = index + 3;
            var limit = Math.Min(text.Length, start + 512);
            var hardEnd = start;
            var softEnd = -1;
            while (hardEnd < limit && text[hardEnd] is not (' ' or '\t' or '\r' or '\n' or '"' or '\'' or '<' or '>' or ',' or ';'))
            {
                if (softEnd < 0 && text[hardEnd] is ('/' or '?' or '#'))
                {
                    softEnd = hardEnd;
                }

                hardEnd++;
            }

            // Şifrede '/', '?', '#' olabilir (amqp://guest:gu#S3CRET@rabbit): ilk ayraçtan önce ':' varsa yetki
            // bölümü sert sınıra kadar uzatılır; fazla maskeleme güvenli taraftır (hunt r2).
            var firstColon = text.IndexOf(':', start, hardEnd - start);
            var end = softEnd >= 0 && !(firstColon >= 0 && firstColon < softEnd) ? softEnd : hardEnd;
            var at = text.LastIndexOf('@', end - 1, end - start);
            if (at > start)
            {
                var colon = text.IndexOf(':', start, at - start);
                var secretStart = colon >= 0 ? colon + 1 : start;
                if (at > secretStart)
                {
                    output.Append(text, copied, secretStart - copied).Append(Hidden);
                    copied = at;
                }
            }

            index = end < text.Length ? text.IndexOf("://", end, StringComparison.Ordinal) : -1;
        }

        return copied == 0 ? text : output.Append(text, copied, text.Length - copied).ToString();
    }

    [GeneratedRegex(@"^(?:sk-(?:ant-|or-)?|AIza)?…[A-Za-z0-9_\-]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex AlreadyMaskedKey();

    [GeneratedRegex(@"(?:(?<=\\u[0-9A-Fa-f]{4})|(?<!(?<!\\)[A-Za-z0-9]))sk-[A-Za-z0-9_\-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"(?:(?<=\\u[0-9A-Fa-f]{4})|(?<!(?<!\\)[A-Za-z0-9_\-]))AIza[0-9A-Za-z_\-]{35}", RegexOptions.CultureInvariant)]
    private static partial Regex GoogleApiKeyPattern();
}
