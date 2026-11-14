using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Gizli bilgi maskelemesinin tek mekanizması (spec 0008 Req 8–10, H-2). Teknik log, olay kaydı, ajan özeti ve
/// sonraki her kayıt buradan geçer. Desen ve alan tablosu plan 0008'deki test tablosudur; yeni bir gizli alan
/// ekleyen spec bu tabloya ve testine satır ekler.
/// <para>Katmanlar: (1) desenler + anahtar/değer tarayıcısı ham metinde; (2) metin kodluysa (JSON <c>\u</c>,
/// <c>%XX</c>, HTML varlığı) çözülmüş halinde aynı tarama — bulunan değerler ham metne uygulanır; (3) gizlenen her
/// değer metnin başka yerlerinde de gizlenir; (4) uygulamanın kendi bildiği sırlar
/// (<see cref="RegisterKnownSecret"/>) nerede geçerse geçsin gizlenir.</para>
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

    /// <summary>Güvenli kesimin geri çekilebileceği en uzun mesafe.</summary>
    public const int CutBackoff = 1024;

    private const string Ellipsis = "…";

    private const int MaxDecodePasses = 3;

    private const int MaxKnownSecrets = 1024;

    private const int UrlScanWindow = 2048;

    private static readonly string[] KnownKeyPrefixes = ["sk-ant-", "sk-or-", "sk-", "AIza"];

    private static readonly string[] HiddenFieldSuffixes =
    [
        "password", "passwords", "passwd", "secret", "secrets", "token", "pairingcode", "servicekey", "privatekey",
        "credential", "credentials", "secretkey", "accesskey", "signingkey", "accountkey", "authorization", "cookie",
        "passphrase", "pwd", "refreshtokens", "accesstokens", "idtokens", "authtokens", "bearertokens", "paircode",
        "pairingpin", "encryptionkey", "masterkey", "clientkey", "apisecret",
    ];

    // Türkçe kökler (ad içinde geçerse): spec 0008/0012'deki adlar — şifre, parola, onay sırrı, oturum/yenileme
    // belirteci, API/erişim/gizli/servis anahtarı, eşleştirme kodu. "sır" tek başına değil ("sıra", "sırasında").
    private static readonly string[] HiddenFieldStems =
    [
        "sifre", "şifre", "parola", "apianahtar", "erişimanahtar", "erisimanahtar", "gizlianahtar", "servisanahtar",
        "hizmetanahtar", "erişimbelirte", "erisimbelirte", "belirteç", "belirteci", "belirtecin", "sırrı", "sirri",
        "onaysır", "onaysir", "eşleştirmekod", "eslestirmekod",
    ];

    private static readonly Lock KnownSecretsLock = new();

    private static readonly HashSet<string> KnownSecrets = new(StringComparer.Ordinal);

    // Her Mask çağrısında kilitsiz okunan anlık görüntü: bilinen sırların önceden hesaplanmış biçimleri (review r4, I-2).
    private static volatile string[][] _knownSecretForms = [];

    /// <summary>Serbest metindeki bilinen gizli bilgi biçimlerini maskeler.</summary>
    /// <param name="text">Metin (hata mesajı, kullanıcı metni, log satırı).</param>
    /// <returns>Maskelenmiş metin; <paramref name="text"/> boşsa aynen döner.</returns>
    public static string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        try
        {
            return MaskAll(text);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Kapalı hata: beklenmeyen bir durumda metin açık yazılmaz (review r4, M-1).
            return MaskFailed;
        }
    }

    /// <summary>Maskeleme beklenmedik biçimde başarısız olursa metin yerine yazılan işaret.</summary>
    public const string MaskFailed = "[maskelenemedi]";

    private static string MaskAll(string text)
    {
        // Üst sınır: maskeleme maliyeti sınırlı kalsın (review round 3, M-1); fazlası atılır, işaretlenir. Kesim yarım
        // token/URL bırakmayacak bir ayraca çekilir (hunt r2/r3); satır sonu korunur (review r4, L-1).
        if (text.Length > MaxInputLength)
        {
            var newline = text.EndsWith("\r\n", StringComparison.Ordinal) ? "\r\n" : text.EndsWith('\n') ? "\n" : string.Empty;
            text = string.Concat(text.AsSpan(0, SafeCutIndex(text, MaxInputLength)), TruncatedMarker, newline);
        }

        var secrets = new SecretValueSet();
        var result = MaskCore(text, secrets);
        var decoded = text;
        for (var pass = 0; pass < MaxDecodePasses; pass++)
        {
            var next = DecodeOnce(decoded);
            if (next == decoded)
            {
                break;
            }

            decoded = next;
            MaskCore(decoded, secrets);
        }

        result = secrets.ApplyTo(result);
        foreach (var forms in _knownSecretForms)
        {
            foreach (var form in forms)
            {
                result = result.Replace(form, Hidden, StringComparison.Ordinal);
            }
        }

        return result;
    }

    /// <summary>
    /// Uygulamanın kendi bildiği bir sırrı (DB şifresi, servis anahtarı, çözülmüş API key…) kaydeder; bundan sonra
    /// her log metninde, hangi bağlamda geçerse geçsin gizlenir. 6 karakterden kısa değerler kaydedilmez.
    /// </summary>
    /// <param name="value">Sır.</param>
    public static void RegisterKnownSecret(string? value)
    {
        value = value?.Trim();
        if (value is null || value.Length < SecretValueSet.MinLength)
        {
            return;
        }

        lock (KnownSecretsLock)
        {
            if (KnownSecrets.Count >= MaxKnownSecrets || !KnownSecrets.Add(value))
            {
                return;
            }

            _knownSecretForms = [.. _knownSecretForms, SecretValueSet.Forms(value)];
        }
    }

    /// <summary>
    /// Bir yapılandırma girdisi gizliyse değerini bilinen sır olarak kaydeder: yoldaki bir bölüm gizli alan adıysa
    /// değerin tamamı, bağlantı dizesiyse içindeki şifreler.
    /// </summary>
    /// <param name="key">Yapılandırma yolu (<c>Bölüm:Alt:Ad</c>).</param>
    /// <param name="value">Değer.</param>
    public static void RegisterConfigurationValue(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // Log ayarları (Serilog:…:Override:Microsoft.AspNetCore.Authorization = Warning) ve ad alanı biçimli bölümler sır
        // değildir; yalnızca yaprak ya da hemen üstü gizli adsa kaydedilir (review r4, L-2).
        var segments = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments[0] is "Logging" or "Serilog" || segments.Any(s => s.Contains('.', StringComparison.Ordinal)))
        {
            return;
        }

        if (segments.Any(s => IsConnectionStringField(Normalize(s)) || Normalize(s) == "connectionstrings"))
        {
            var secrets = new SecretValueSet();
            MaskCore(value, secrets);
            foreach (var secret in secrets.Values)
            {
                RegisterKnownSecret(secret);
            }

            return;
        }

        var parent = segments.Length > 1 ? segments[^2] : string.Empty;
        if (IsSecretField(segments[^1]) || (Normalize(parent).EndsWith('s') && IsSecretField(parent))) // Ai:ApiKeys:Gemini, Passwords:Admin
        {
            RegisterKnownSecret(value);
        }
    }

    /// <summary>
    /// Metni <paramref name="limit"/> karakterde güvenli keser: son ayraçta (boşluk, tırnak, <c>,;&amp;&lt;&gt;{}[]()|</c>;
    /// en fazla <see cref="CutBackoff"/> geri) — sınırdaki yarım key/URL/JWT kalmasın.
    /// </summary>
    /// <param name="text">Metin.</param>
    /// <param name="limit">Üst sınır.</param>
    /// <returns>Korunacak uzunluk.</returns>
    public static int SafeCutIndex(string text, int limit)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length <= limit)
        {
            return text.Length;
        }

        var min = Math.Max(0, limit - CutBackoff);
        for (var p = limit; p > min; p--)
        {
            if (char.IsWhiteSpace(text[p]) || text[p] is '"' or '\'' or ',' or ';' or '&' or '<' or '>' or '{' or '}' or '[' or ']' or '(' or ')' or '|')
            {
                return p;
            }
        }

        return min;
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
            return string.IsNullOrEmpty(value) ? Hidden : MaskApiKeyValue(value);
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

    /// <summary>
    /// API key alanının değerini maskeler: tek parça key <c>sk-…a3f</c>; boşluk içeren değer (key + başka metin)
    /// tamamen gizlenir — ilk parçadan sonrası sızmasın (hunt r3, H3-A).
    /// </summary>
    /// <param name="value">Değer.</param>
    /// <returns>Maskelenmiş değer.</returns>
    internal static string MaskApiKeyValue(string value)
    {
        var token = value.Trim();
        if (token == Hidden || IsMaskedApiKey(token))
        {
            return token;
        }

        return token.Length == 0 || token.Any(char.IsWhiteSpace) ? Hidden : MaskApiKey(token);
    }

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

    /// <summary>Desenler + tarayıcı; gizlenen değerleri <paramref name="secrets"/>'e yazar.</summary>
    private static string MaskCore(string text, SecretValueSet secrets)
    {
        var result = JwtPattern().Replace(text, m => Record(secrets, m.Value, JwtPlaceholder));
        // "Bearer was challenged", "Bearer error=…": sıradan sözcük token değildir (hunt r5, hasar).
        result = BearerPattern().Replace(result, m => m.Groups["token"].Value == JwtPlaceholder || IsPlainWord(m.Groups["token"].Value)
            ? m.Value
            : $"{m.Groups["scheme"].Value} {Record(secrets, m.Groups["token"].Value, Hidden)}");
        result = MaskUrlUserInfo(result, secrets);
        result = ApiKeyPattern().Replace(result, m => Record(secrets, m.Value, MaskApiKey(m.Value)));
        result = GoogleApiKeyPattern().Replace(result, m => Record(secrets, m.Value, MaskApiKey(m.Value)));
        return SecretKeyValueScanner.Mask(result, secrets);
    }

    private static string Record(SecretValueSet secrets, string value, string replacement)
    {
        secrets.Add(value, replacement);
        return replacement;
    }

    /// <summary>Tek kat çözme: JSON kaçışları (<c>\uXXXX</c>, <c>\/</c>, <c>\"</c>, <c>\n</c>…), <c>%XX</c>, HTML varlıkları.</summary>
    private static string DecodeOnce(string text)
    {
        var result = text.Contains('\\', StringComparison.Ordinal) ? JsonUnescape(text) : text;
        if (result.Contains('%', StringComparison.Ordinal))
        {
            result = Uri.UnescapeDataString(result);
        }

        if (result.Contains('&', StringComparison.Ordinal))
        {
            result = WebUtility.HtmlDecode(result);
        }

        return result;
    }

    private static string JsonUnescape(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c != '\\' || i + 1 >= text.Length)
            {
                builder.Append(c);
                continue;
            }

            var n = text[i + 1];
            if (n == 'u' && i + 5 < text.Length && IsHex(text, i + 2, 4))
            {
                builder.Append((char)Convert.ToInt32(text.Substring(i + 2, 4), 16));
                i += 5;
                continue;
            }

            char? decoded = n switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '/' => '/',
                '"' => '"',
                '\'' => '\'',
                '\\' => '\\',
                _ => null,
            };
            if (decoded is null)
            {
                builder.Append(c);
                continue;
            }

            builder.Append(decoded.Value);
            i++;
        }

        return builder.ToString();
    }

    private static bool IsHex(string text, int start, int count)
    {
        for (var i = start; i < start + count; i++)
        {
            if (!char.IsAsciiHexDigit(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsApiKeyField(string name) =>
        NameForms(name).Any(n => n.EndsWith("apikey", StringComparison.Ordinal));

    private static bool IsConnectionStringField(string name) => name.EndsWith("connectionstring", StringComparison.Ordinal);

    private static bool IsHiddenField(string name) =>
        NameForms(name).Any(n => HiddenFieldSuffixes.Any(s => n.EndsWith(s, StringComparison.Ordinal))
            || n.StartsWith("password", StringComparison.Ordinal) // password_confirmation, passwordHash…
            || PassNames.Contains(n)) // DB_PASS, SMTP_PASS — genel "pass" eki firstPass/classPass'i de yakalıyordu (hunt r5)
        || HiddenFieldStems.Any(s => name.Contains(s, StringComparison.Ordinal)); // şifreniz, parolası, yeniŞifre…

    private static readonly HashSet<string> PassNames =
        new(StringComparer.Ordinal) { "pass", "dbpass", "smtppass", "mailpass", "userpass", "adminpass", "rootpass", "postgrespass", "mysqlpass", "redispass" };

    /// <summary>Yalnızca harften (ve isteğe bağlı sondaki '=') oluşan, 15 karakterden kısa sözcük: token değil.</summary>
    private static bool IsPlainWord(string token)
    {
        var word = token.TrimEnd('=');
        return word.Length is > 0 and <= 15 && word.All(char.IsLetter);
    }

    /// <summary>Adın karşılaştırılan biçimleri: kendisi, sondaki sıra numarası atılmış, çoğul eki atılmış (<c>token_0</c>, <c>tokens</c>, <c>apiKey2</c>).</summary>
    private static IEnumerable<string> NameForms(string name)
    {
        yield return name;
        var trimmed = name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        if (trimmed.Length > 0 && trimmed != name)
        {
            yield return trimmed;
        }

        // Çoğul: tokens hariç (MaxTokens, InputTokens sayaçtır; refreshTokens gibi gerçekleri sonek tablosunda).
        if (trimmed.Length > 1 && trimmed[^1] == 's' && !trimmed.EndsWith("tokens", StringComparison.Ordinal))
        {
            yield return trimmed[..^1];
        }
    }

    private static string Normalize(string? fieldName) =>
        fieldName is null
            ? string.Empty
            : fieldName.Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace('İ', 'i') // Türkçe büyük İ: "ŞİFRE" → "şifre" (invariant küçültme İ'yi değiştirmez).
                .ToLowerInvariant()
                .Replace("̇", string.Empty, StringComparison.Ordinal);

    // '-' ve '_' sonrası başlangıç yok: "eyJ-eyJ-…" girdisinde her konumdan yeniden tarama (karesel süre) olmasın (hunt r3, H3-G).
    [GeneratedRegex(@"(?:(?<=\\u[0-9A-Fa-f]{4})|(?<!(?<!\\)[A-Za-z0-9_\-]))eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]*", RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"(?<scheme>\bBearer)[ \t]+['""]?(?<token>[^\s""',;]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();

    /// <summary>
    /// URL'deki kullanıcı bilgisinin şifresini maskeler: <c>postgres://user:pw@host</c> → <c>postgres://user:***@host</c>;
    /// yalnızca token varsa (<c>https://TOKEN@host</c>) tamamı (hunt: URI biçimli bağlantı dizeleri, git uzak adresleri).
    /// Her <c>://</c> için en fazla <see cref="UrlScanWindow"/> karakter taranır.
    /// </summary>
    /// <param name="text">Metin.</param>
    /// <param name="secrets">Gizlenen değerlerin kaydı.</param>
    /// <returns>Maskelenmiş metin.</returns>
    private static string MaskUrlUserInfo(string text, SecretValueSet secrets)
    {
        var index = text.IndexOf("://", StringComparison.Ordinal);
        if (index < 0)
        {
            return text;
        }

        var output = new StringBuilder(text.Length);
        var copied = 0;
        while (index >= 0)
        {
            var start = index + 3;
            var limit = Math.Min(text.Length, start + UrlScanWindow);
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
            // ':' sonrası ilk ayraca kadar yalnızca rakamsa port'tur (http://localhost:5000/…/ali@x), şifre değil (hunt r5).
            var firstColon = text.IndexOf(':', start, hardEnd - start);
            var colonIsPort = firstColon >= 0 && softEnd > firstColon + 1 && IsDigits(text, firstColon + 1, softEnd);
            var end = softEnd >= 0 && (colonIsPort || !(firstColon >= 0 && firstColon < softEnd)) ? softEnd : hardEnd;
            var at = end > start ? text.LastIndexOf('@', end - 1, end - start) : -1;
            if (at < 0 && hardEnd < limit && text[hardEnd] is ',' or ';')
            {
                // Şifrede ',' ya da ';' (RFC 3986 izin verir): bir sonraki URL'ye kadar boşluksuz devamda '@' ara (hunt r3).
                end = WideAuthorityEnd(text, start, limit);
                at = end > start ? text.LastIndexOf('@', end - 1, end - start) : -1;
            }

            if (at > start)
            {
                var colon = text.IndexOf(':', start, at - start);
                var secretStart = colon >= 0 ? colon + 1 : start;
                if (at > secretStart && secretStart >= copied)
                {
                    secrets.Add(text[secretStart..at], Hidden);
                    output.Append(text, copied, secretStart - copied).Append(Hidden);
                    copied = at;
                }
            }

            index = end < text.Length ? text.IndexOf("://", Math.Max(end, start), StringComparison.Ordinal) : -1;
        }

        return copied == 0 ? text : output.Append(text, copied, text.Length - copied).ToString();
    }

    private static bool IsDigits(string text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (!char.IsAsciiDigit(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Boşluk/tırnak/ayraç ya da bir sonraki <c>://</c>'ye kadar; ilk '/', '?' veya '#' de biter.</summary>
    private static int WideAuthorityEnd(string text, int start, int limit)
    {
        var p = start;
        while (p < limit && text[p] is not (' ' or '\t' or '\r' or '\n' or '"' or '\'' or '<' or '>' or '/' or '?' or '#'))
        {
            if (text[p] == ':' && p + 2 < text.Length && text[p + 1] == '/' && text[p + 2] == '/')
            {
                return text.LastIndexOfAny([',', ';'], p, p - start + 1) is var sep and >= 0 ? sep : p;
            }

            p++;
        }

        return p;
    }

    [GeneratedRegex(@"^(?:sk-(?:ant-|or-)?|AIza)?…[A-Za-z0-9_\-]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex AlreadyMaskedKey();

    [GeneratedRegex(@"(?:(?<=\\u[0-9A-Fa-f]{4})|(?<!(?<!\\)[A-Za-z0-9]))sk-[A-Za-z0-9_\-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"(?:(?<=\\u[0-9A-Fa-f]{4})|(?<!(?<!\\)[A-Za-z0-9]))AIza[0-9A-Za-z_\-]{35}", RegexOptions.CultureInvariant)]
    private static partial Regex GoogleApiKeyPattern();
}
