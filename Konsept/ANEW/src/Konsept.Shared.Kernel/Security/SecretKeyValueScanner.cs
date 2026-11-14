namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Serbest metindeki gizli anahtar/değer çiftlerini tek geçişte bulup değerini maskeleyen el yazımı tarayıcı (review
/// round 3: regex uç durumları ve karesel süre yerine). Her karakter sınırlı sayıda okunur; süre girdi boyuyla
/// doğrusaldır. Anahtarın gizli olup olmadığına alan tablosu karar verir (<see cref="SecretMasker.IsSecretField"/>).
/// <para>Tanınan biçimler (her biri <c>SecretMaskerTests</c> tablosunda):</para>
/// <list type="bullet">
/// <item>Anahtar: harf/rakam/<c>_</c>/<c>-</c> dizisi, öncesinde harf ya da rakam yok (ya da bir kaçış: <c>%22</c>,
/// <c>"</c>, <c>\n</c>). <c>Bölüm:Alt:Ad=</c> yolunda bölümlerden biri gizliyse de gizli sayılır. Sorgu dizesinde
/// <c>?key=</c>/<c>&amp;key=</c> API key'dir.</item>
/// <item>Ayırıcı: isteğe bağlı kaçış + tırnak (<c>\"</c>, <c>"</c>, <c>%22</c>), boşluk (NBSP dahil; sıfır
/// genişlikli atlanır), <c>:=</c>, <c>=</c>, <c>:</c>, <c>＝</c>, <c>：</c>, <c>%3D</c>, <c>%3A</c>.</item>
/// <item>Değer: kaçışlı tırnak (her derinlik), <c>"…"</c>, <c>%22…%22</c>, <c>"…"</c>/<c>'…'</c>
/// (<c>\x</c>, <c>""</c>; kapanmazsa satır sonu), <c>{</c>/<c>[</c> ile başlıyorsa satır sonu + daha içeride girintili
/// satırlar; boşsa (satırın kalanı boş ya da yorum) ya da YAML blok göstergesiyse (<c>|</c>, <c>&gt;-</c>, <c>|2-</c>…)
/// daha içeride girintili sonraki satırlar; boş değerden sonra satırda başka şey varsa tarama hemen sürer.</item>
/// <item>Tırnaksız değer: bağlantı dizesi bağlamında yalnızca <c>;</c>/satır sonu; serbest metinde ayrıca <c>, Ad =</c>,
/// <c>; Ad=</c>, <c>&amp;ad=</c>, kapanan <c>}</c>; URL kodlu ayırıcıda boşluk, <c>&amp;</c>, <c>%26</c>.</item>
/// <item>Ayrıca: CLI bayrağı (<c>--password değer</c>, tırnaklı değer), SQL tarzı <c>PASSWORD 'x'</c>, XML öğesi
/// (<c>&lt;ns:Password a="…"&gt;değer&lt;/…&gt;</c>, CDATA), <c>KeyValuePair</c> metni <c>[Password, değer]</c>.</item>
/// <item>API key alanı: tek parça değer <c>sk-…xyz</c>, boşluk içeren değer tümüyle gizlenir; tırnaksız değer ilk boşlukta biter. Cookie değeri satır sonuna kadar. Anahtar süsleri (<c>["Password"]=</c>, <c>**Password**:</c>, <c>password[0]</c>), <c>A__B__C</c>/<c>A.B.C</c> yolları, iki sözcüklü adlar (<c>API key</c>), <c>Token'ınız</c>, tuple <c>(Password, x)</c>, SQL <c>E'…'</c>/<c>$$…$$</c>.</item>
/// </list>
/// </summary>
internal static class SecretKeyValueScanner
{
    /// <summary>Anahtar olarak okunacak en uzun ad; daha uzunu anahtar sayılmaz.</summary>
    public const int MaxKeyLength = 256;

    private const int LookaheadNameLength = 64;

    /// <summary>Metindeki gizli anahtar/değer çiftlerinin değerini maskeler.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="secrets">Gizlenen değerlerin kaydı.</param>
    /// <returns>Maskelenmiş metin (değişiklik yoksa aynı örnek).</returns>
    public static string Mask(string text, SecretValueSet? secrets = null)
    {
        var output = new SecretMaskOutput(text, secrets);
        var i = 0;
        while (i < text.Length)
        {
            if (!IsKeyStart(text, i))
            {
                i++;
                continue;
            }

            var keyEnd = i;
            while (keyEnd < text.Length && IsKeyChar(text[keyEnd]))
            {
                keyEnd++;
            }

            if (keyEnd - i > MaxKeyLength)
            {
                i = keyEnd;
                continue;
            }

            if (!IsSecretKey(text, i, keyEnd, out var apiKey))
            {
                // İki sözcüklü ad: "API key:", "Pairing code:", "Erişim anahtarı:" (hunt r3).
                if (!TryTwoWordKey(text, i, keyEnd, out var pairEnd, out apiKey))
                {
                    i = keyEnd;
                    continue;
                }

                keyEnd = pairEnd;
            }

            // Not: "Invalid password: user 42, …" için niteleyici istisnası (fix-10/11) kaldırıldı — her biçimi yeni bir
            // sızıntı açtı (review r9/r10). Bu satırlar tümüyle gizlenir; tanı kaybı güvenli taraftır (B-45).
            i = Math.Max(keyEnd, MaskPair(text, i, keyEnd, apiKey, output));
        }

        return output.Result();
    }

    /// <summary>Gizli anahtardan sonraki değeri bulup maskeler; taramanın süreceği konumu döner.</summary>
    private static int MaskPair(string text, int keyStart, int keyEnd, bool apiKey, SecretMaskOutput output)
    {
        if (TryMaskXmlElement(text, keyStart, keyEnd, apiKey, output, out _))
        {
            // Tarama etiketin özniteliklerinden sürer (ClientSecret="…" gibi gizli öznitelik adları da okunur).
            return keyEnd;
        }

        // KeyValuePair metni [Password, değer] ve tuple metni (Password, değer).
        if (keyStart > 0 && text[keyStart - 1] is '[' or '(' && keyEnd + 1 < text.Length && text[keyEnd] == ',' && text[keyEnd + 1] == ' ')
        {
            var close = text.IndexOf(text[keyStart - 1] == '[' ? ']' : ')', keyEnd);
            var lineEnd = LineEnd(text, keyEnd);
            return Emit(text, keyEnd + 2, close >= 0 && close < lineEnd ? close : lineEnd, apiKey, output);
        }

        if (!TryReadSeparator(text, keyEnd, out var valueStart, out var urlEncoded))
        {
            // Gizli ad + tek sözcük + ayırıcı: "Secret key: x", "Token value: x", "Şifre değeri: x" (hunt r4).
            var word = SkipWord(text, keyEnd);
            if (word < 0 || !TryReadSeparator(text, word, out valueStart, out urlEncoded))
            {
                return MaskWithoutSeparator(text, keyStart, keyEnd, apiKey, output);
            }
        }

        var lineEndOfValue = LineEnd(text, valueStart);
        var unquoted = QuoteLength(text, valueStart) == 0 && valueStart < text.Length && text[valueStart] is not ('{' or '[');
        if (unquoted && (IsPathContinuation(text, keyStart, valueStart, apiKey) || IsSqlParameter(text, keyStart, keyEnd, valueStart)))
        {
            // Ai:ApiKeys:OpenRouter yolu (sağlayıcı adı değer değildir; sonraki bölüm yol kuralıyla okunur) ve
            // EF SQL'deki "Token" = @__token_0 parametre adı (hunt r5, hasar).
            return keyEnd;
        }

        int start, end, after;
        if (unquoted && (apiKey || (keyStart > 0 && text[keyStart - 1] is '?' or '&' && text[valueStart - 1] == '=')))
        {
            // API key ve URL sorgu değeri boşluk içermez: tırnaksız değer ilk boşlukta (ya da ayraçta) biter, tarama
            // sonrasında sürer (H3-A; hunt r11: "?access_token=x - 10 ms" satırın kalanı korunur); satır sonuna kadar
            // okunmaz (hunt r4, perf).
            // Sorgu değeri yalnızca boşluk/&/#'te biter (",;')" bir şifrede olabilir, hunt r12); API key ayrıca ayraçlarda.
            var query = !apiKey;
            start = end = valueStart;
            while (end < text.Length && !char.IsWhiteSpace(text[end])
                && (query ? text[end] is not ('&' or '#') : text[end] is not ('&' or ';' or ',' or '"' or '\'' or '<' or '>' or ')')))
            {
                end++;
            }

            after = end;
        }
        else
        {
            (start, end, after) = ReadValue(text, valueStart, ConnectionStringContext(text, keyStart), urlEncoded);
        }

        if (unquoted && end > start && IsCookieKey(text, keyStart, keyEnd))
        {
            // Cookie/Set-Cookie: başlık değeri satır sonuna kadar — "a=1; session=…" ikinci çerez açıkta kalmasın (H3-B).
            end = after = Trim(text, start, lineEndOfValue);
        }

        if (end > start && IsBlockIndicator(text, start, lineEndOfValue))
        {
            MaskIndentedLines(text, keyStart, lineEndOfValue, output);
            return after;
        }

        if (end <= start)
        {
            // Boş değer: satırın kalanı boşsa (ya da yorum) değer sonraki girintili satırlardadır; tarama her durumda
            // değerden hemen sonra sürer (yorumdaki "token=…" de okunur, H3-F).
            if (RestOfLineBlank(text, after, lineEndOfValue))
            {
                MaskIndentedLines(text, keyStart, lineEndOfValue, output);
            }

            return after;
        }

        var next = Emit(text, start, end, apiKey, output);

        // Yalnızca satırda kapanmayan ayraç sonraki girintili satırlara uzanır; "Credentials: [none]" yığın izini silmesin (hunt r6).
        if (text[start] is '{' or '[' && end == lineEndOfValue && MatchingBracket(text, start, lineEndOfValue) < 0)
        {
            next = Math.Max(next, MaskIndentedLines(text, keyStart, lineEndOfValue, output));
        }

        return Math.Max(next, after);
    }

    /// <summary>Ad tek boşlukla ayrılmış ikinci bir sözcükle birlikte gizli bir ad oluşturuyor mu (<c>API key</c> → <c>apikey</c>).</summary>
    private static bool TryTwoWordKey(string text, int keyStart, int keyEnd, out int pairEnd, out bool apiKey)
    {
        pairEnd = keyEnd;
        apiKey = false;
        if (keyEnd + 1 >= text.Length || text[keyEnd] != ' ' || !IsKeyChar(text[keyEnd + 1]))
        {
            return false;
        }

        var end = keyEnd + 1;
        while (end < text.Length && IsKeyChar(text[end]) && end - keyStart <= LookaheadNameLength)
        {
            end++;
        }

        // İkinci sözcük tek başına gizliyse (ör. "-Password" bayrağı) kendi başına okunur.
        if ((end < text.Length && IsKeyChar(text[end])) || IsSecretKey(text, keyEnd + 1, end, out _))
        {
            return false;
        }

        var name = SecretMasker.NormalizeFieldName(string.Concat(text.AsSpan(keyStart, keyEnd - keyStart), text.AsSpan(keyEnd + 1, end - keyEnd - 1)));
        apiKey = SecretMasker.IsApiKeyFieldName(name);
        if (!apiKey && !SecretMasker.IsHiddenFieldName(name))
        {
            return false;
        }

        pairEnd = end;
        return true;
    }

    /// <summary>
    /// Konumda tek boşluk + değer bildiren bir sözcük (<c>key</c>, <c>value</c>, <c>değeri</c>, <c>string</c>…) varsa
    /// sözcüğün sonu; yoksa -1. Her sözcük kabul edilseydi "Token expired: …", "Password reset: …" satırları silinirdi (hunt r5).
    /// </summary>
    private static int SkipWord(string text, int pos)
    {
        var end = SkipAnyWord(text, pos);
        return end > 0 && ValueWords.Contains(text[(pos + 1)..end]) ? end : -1;
    }

    /// <summary>Gizli addan sonra boşluksuz <c>:</c> ve yalnızca harften oluşan bir yapılandırma yolu bölümü mü (<c>ApiKeys:OpenRouter</c>).</summary>
    private static bool IsPathContinuation(string text, int keyStart, int valueStart, bool apiKey)
    {
        if (valueStart == 0 || text[valueStart - 1] != ':' || !char.IsLetter(text[valueStart]))
        {
            return false;
        }

        var p = valueStart;
        while (p < text.Length && char.IsLetter(text[p]))
        {
            p++;
        }

        // Sağlayıcı adları kısadır; 20+ harflik değer yol bölümü sayılmaz (review r5, I-1).
        if (p - valueStart >= 20 || (p < text.Length && IsKeyChar(text[p])))
        {
            return false;
        }

        // API key adı: harf-yalnız bölüm yol sayılır (Ai:ApiKeys:OpenRouter). Diğer gizli adlar: yalnızca anahtarın kendisi
        // de bir yol bölümüyse ve büyük harfli bölümü yeni bir ayırıcı/tırnak izliyorsa (Identity:Password:RequiredLength',
        // hunt r8); "password:Welcome\"" maskelenir (review r8, L-1).
        return apiKey || (keyStart > 0 && text[keyStart - 1] == ':' && char.IsUpper(text[valueStart]) && p < text.Length && text[p] is ':' or '=' or '\'' or '"');
    }

    /// <summary>
    /// Değer bir EF/ADO SQL parametre adı mı: anahtar çift tırnaklı SQL tanımlayıcısı (<c>"Token" = @__token_0</c>) ve
    /// ad <c>@__…</c> ya da <c>@p&lt;rakam&gt;</c>. Yalnızca biçime bakmak <c>Password=@dmin2024</c>'ü açık bırakıyordu (review r5, M-1).
    /// </summary>
    private static bool IsSqlParameter(string text, int keyStart, int keyEnd, int valueStart)
    {
        if (keyStart == 0 || text[keyStart - 1] != '"' || keyEnd >= text.Length || text[keyEnd] != '"'
            || text[valueStart] != '@' || valueStart + 2 >= text.Length)
        {
            return false;
        }

        // Ad: EF ≤9 @__token_0, EF 10 @token, ADO @p0 — koruma tırnaklı SQL tanımlayıcısı + "=" karşılaştırmasıdır
        // (review r6, L-3; r7, L-1: {"Password": @dmin2024} JSON'u ':' ile gelir ve maskelenir).
        var sep = SkipInlineSpace(text, keyEnd + 1);
        if (sep >= text.Length || text[sep] != '=' || !(char.IsLetter(text[valueStart + 1]) || text[valueStart + 1] == '_'))
        {
            return false;
        }

        var p = valueStart + 1;
        while (p < text.Length && (char.IsLetterOrDigit(text[p]) || text[p] == '_'))
        {
            p++;
        }

        return p == text.Length || char.IsWhiteSpace(text[p]) || text[p] is ')' or ',';
    }

    /// <summary>
    /// Anahtar bir tip ya da hata kodu adı mı: büyük harfle başlar ve tip benzeri bir sonekle biter
    /// (<c>PasswordPolicyException</c>, <c>PasswordResetService</c>, <c>PasswordHasherOptions</c>) ya da bir ASP.NET Core
    /// Identity hata kodudur (<c>PasswordRequiresDigit</c>). Bu adlar değer taşımaz.
    /// </summary>
    private static bool IsDescriptiveName(string text, int keyStart, int keyEnd)
    {
        // Yalnızca "Ad: metin" biçiminde (istisna başlığı, kategori, Identity hatası); "PasswordResult=x" ve
        // "Ai:ApiKeys:GeminiService=x" maskelenmeye devam eder (review r7, L-2; hunt r8).
        if (!char.IsUpper(text[keyStart]) || keyEnd + 1 >= text.Length || text[keyEnd] != ':' || text[keyEnd + 1] != ' ')
        {
            return false;
        }

        var raw = text.AsSpan(keyStart, keyEnd - keyStart);
        foreach (var suffix in TypeSuffixes)
        {
            if (raw.Length > suffix.Length && raw.EndsWith(suffix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return IdentityErrorCodes.Contains(raw.ToString());
    }

    private static readonly string[] TypeSuffixes =
    [
        "Exception", "Service", "Handler", "Controller", "Validator", "Provider", "Manager", "Policy", "Hasher", "Store",
        "Middleware", "Filter", "Factory", "Options", "Logger", "Worker", "Job", "Command", "Query", "Request", "Response",
        "Result", "Event", "Settings", "Configuration", "Attribute", "Requirement",
    ];

    private static readonly HashSet<string> IdentityErrorCodes = new(StringComparer.Ordinal)
    {
        "PasswordTooShort", "PasswordRequiresDigit", "PasswordRequiresLower", "PasswordRequiresUpper",
        "PasswordRequiresNonAlphanumeric", "PasswordRequiresUniqueChars", "PasswordMismatch", "InvalidToken",
    };

    private static readonly HashSet<string> ValueWords =
        new(StringComparer.OrdinalIgnoreCase) { "key", "value", "string", "text", "değeri", "değer", "metni", "kodu", "code" };

    private static int SkipAnyWord(string text, int pos)
    {
        if (pos + 1 >= text.Length || text[pos] != ' ' || !char.IsLetter(text[pos + 1]))
        {
            return -1;
        }

        var end = pos + 1;
        while (end < text.Length && char.IsLetter(text[end]) && end - pos <= 24)
        {
            end++;
        }

        return end < text.Length && char.IsLetter(text[end]) ? -1 : end;
    }

    private static bool IsCookieKey(string text, int keyStart, int keyEnd) =>
        SecretMasker.NormalizeFieldName(text[keyStart..keyEnd]).EndsWith("cookie", StringComparison.Ordinal);

    /// <summary>Ayırıcısız biçimler: CLI bayrağı <c>--password değer</c> ve SQL <c>PASSWORD 'değer'</c>.</summary>
    private static int MaskWithoutSeparator(string text, int keyStart, int keyEnd, bool apiKey, SecretMaskOutput output)
    {
        var p = SkipInlineSpace(text, keyEnd);
        if (p == keyEnd || p >= text.Length)
        {
            return keyEnd;
        }

        // 'cookie ".AspNetCore.Antiforgery…"' çerez adıdır, değer değil (hunt r9); --cookie ve cookie "x" gizlenir (review r9, M-2).
        if (text[keyStart] != '-' && IsCookieKey(text, keyStart, keyEnd) && p + 1 < text.Length && text[p] == '"' && text[p + 1] == '.')
        {
            return keyEnd;
        }

        // SQL dize önekleri: E'…' / N'…' / U&'…' (PostgreSQL, SQL Server) ve $$…$$ (hunt r3).
        if (p + 1 < text.Length && text[p] is 'E' or 'e' or 'N' or 'n' && text[p + 1] == '\'')
        {
            p++;
        }
        else if (p + 2 < text.Length && text[p] is 'U' or 'u' && text[p + 1] == '&' && text[p + 2] == '\'')
        {
            p += 2;
        }
        else if (p + 1 < text.Length && text[p] == '$' && text[p + 1] == '$')
        {
            var close = text.IndexOf("$$", p + 2, StringComparison.Ordinal);
            var lineEnd = LineEnd(text, p);
            var end = close >= 0 && close < lineEnd ? close : lineEnd;
            return Math.Max(Emit(text, p + 2, end, apiKey, output), Math.Min(text.Length, end + 2));
        }

        if (text[p] is '"' or '\'')
        {
            var (start, end, after) = ReadQuoted(text, p, text[p]);
            return Math.Max(Emit(text, start, end, apiKey, output), after);
        }

        if (text[keyStart] == '-' && text[p] is not ('-' or '\r' or '\n'))
        {
            var end = p;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            return Emit(text, p, end, apiKey, output);
        }

        return keyEnd;
    }

    /// <summary>
    /// XML öğesi: <c>&lt;[ns:]ad [öznitelik…]&gt;değer&lt;</c> (CDATA dahil); ad gizliyse <c>value="…"</c> özniteliği ve
    /// içerik yalnızca alt öğelerse kapanış etiketine kadar tüm içerik de gizlenir (H3-E).
    /// </summary>
    private static bool TryMaskXmlElement(string text, int keyStart, int keyEnd, bool apiKey, SecretMaskOutput output, out int resume)
    {
        resume = keyEnd;
        var q = keyStart - 1;
        if (q >= 0 && text[q] == ':')
        {
            q--;
            while (q >= 0 && (IsKeyChar(text[q]) || text[q] == '.'))
            {
                q--;
            }
        }

        if (q < 0 || text[q] != '<' || keyEnd >= text.Length || !(text[keyEnd] is '>' or '/' || char.IsWhiteSpace(text[keyEnd])))
        {
            return false;
        }

        var close = text.IndexOf('>', keyEnd);
        if (close < 0)
        {
            return false;
        }

        MaskValueAttribute(text, keyEnd, close, apiKey, output);
        resume = close + 1;
        if (text[close - 1] == '/')
        {
            return true;
        }

        if (TryXmlContent(text, close, out var start, out var end))
        {
            resume = Math.Max(resume, Emit(text, start, end, apiKey, output));
            return true;
        }

        // İçerik boş ve alt öğe başlıyorsa: kapanış etiketine kadar (en fazla 8 KB) tümü gizlenir.
        var lt = text.IndexOf('<', close + 1);
        if (lt >= 0 && lt + 1 < text.Length && text[lt + 1] != '/' && Trim(text, close + 1, lt) == close + 1)
        {
            var closing = string.Concat("</", text.AsSpan(q + 1, keyEnd - q - 1), ">");
            var closeTag = text.IndexOf(closing, lt, Math.Min(text.Length - lt, 8192), StringComparison.Ordinal);
            if (closeTag > lt)
            {
                output.Replace(close + 1, closeTag, SecretMasker.Hidden);
                resume = closeTag;
            }
        }

        return true;
    }

    /// <summary>Etiket içindeki <c>value="…"</c> (büyük/küçük harf önemsiz) özniteliğinin değerini gizler.</summary>
    private static void MaskValueAttribute(string text, int from, int close, bool apiKey, SecretMaskOutput output)
    {
        var p = from;
        while (p < close)
        {
            var hit = text.IndexOf("value", p, close - p, StringComparison.OrdinalIgnoreCase);
            if (hit < 0)
            {
                return;
            }

            var s = SkipInlineSpace(text, hit + 5);
            if (char.IsWhiteSpace(text[hit - 1]) && s < close && text[s] == '=')
            {
                s = SkipInlineSpace(text, s + 1);
                if (s < close && text[s] is '"' or '\'')
                {
                    var end = text.IndexOf(text[s], s + 1, close - s - 1);
                    if (end > s)
                    {
                        Emit(text, s + 1, end, apiKey, output);
                        return;
                    }
                }
            }

            p = hit + 5;
        }
    }

    /// <summary>Açılış etiketinden sonraki metin içeriği (CDATA dahil); boşsa <c>false</c>.</summary>
    private static bool TryXmlContent(string text, int close, out int start, out int end)
    {
        start = close + 1;
        const string CData = "<![CDATA[";
        var contentStart = start;
        while (contentStart < text.Length && char.IsWhiteSpace(text[contentStart]))
        {
            contentStart++;
        }

        if (string.CompareOrdinal(text, contentStart, CData, 0, CData.Length) == 0)
        {
            var cdataEnd = text.IndexOf("]]>", contentStart, StringComparison.Ordinal);
            end = cdataEnd < 0 ? LineEnd(text, contentStart) : cdataEnd + 3; // Kapanmamış CDATA: satır sonu (hunt r4, perf).
            return true;
        }

        var lt = text.IndexOf('<', start);
        end = Trim(text, start, lt < 0 ? text.Length : lt);
        return end > start;
    }

    private static int Emit(string text, int start, int end, bool apiKey, SecretMaskOutput output)
    {
        if (end <= start)
        {
            return Math.Max(start, end);
        }

        // API key: tek parça değer sk-…abc; boşluk içeren değer (key + başka metin) tümüyle gizlenir (H3-A).
        output.Replace(start, end, apiKey ? SecretMasker.MaskApiKeyValue(text[start..end]) : SecretMasker.Hidden);
        return end;
    }

    private static bool IsBlockIndicator(string text, int start, int lineEnd)
    {
        if (text[start] is not ('|' or '>'))
        {
            return false;
        }

        var p = start + 1;
        var max = Math.Min(lineEnd, p + 3);
        while (p < max && (char.IsAsciiDigit(text[p]) || text[p] is '+' or '-'))
        {
            p++;
        }

        return RestOfLineBlank(text, p, lineEnd);
    }

    private static bool RestOfLineBlank(string text, int p, int lineEnd)
    {
        p = SkipInlineSpace(text, p);
        return p >= lineEnd || text[p] == '#';
    }

    /// <summary>Anahtar satırından daha içeride girintili sonraki satırları (boş satırlar dahil) maskeler.</summary>
    private static int MaskIndentedLines(string text, int keyStart, int from, SecretMaskOutput output)
    {
        var lineStart = keyStart == 0 ? 0 : text.LastIndexOfAny(['\r', '\n'], keyStart - 1) + 1;
        var keyIndent = Columns(text, lineStart);
        var p = NextLineStart(text, from);
        if (p < output.IndentedUntil)
        {
            // Bu satırlar bir önceki tetikleyicide zaten tarandı (hunt r4, perf: tekrar eden "pwd:\"\"#").
            return from;
        }

        var last = from;
        while (p < text.Length)
        {
            var indent = Indent(text, p);
            var contentStart = p + indent;
            var end = LineEnd(text, p);
            if (contentStart >= end)
            {
                p = NextLineStart(text, end);
                continue;
            }

            if (Columns(text, p) <= keyIndent)
            {
                break;
            }

            output.Replace(contentStart, end, SecretMasker.Hidden);
            last = end;
            p = NextLineStart(text, end);
        }

        output.IndentedUntil = Math.Max(output.IndentedUntil, p);
        return last;
    }

    private static int Indent(string text, int lineStart)
    {
        var p = lineStart;
        while (p < text.Length && text[p] is ' ' or '\t')
        {
            p++;
        }

        return p - lineStart;
    }

    /// <summary>Satır başı girintisinin sütun genişliği (sekme = 4); karşılaştırma için.</summary>
    private static int Columns(string text, int lineStart)
    {
        var columns = 0;
        for (var p = lineStart; p < text.Length && text[p] is ' ' or '\t'; p++)
        {
            columns += text[p] == '\t' ? 4 : 1;
        }

        return columns;
    }

    private static int NextLineStart(string text, int pos)
    {
        var p = LineEnd(text, pos);
        if (p < text.Length && text[p] == '\r')
        {
            p++;
        }

        if (p < text.Length && text[p] == '\n')
        {
            p++;
        }

        return p;
    }

    private static bool IsKeyChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '-';

    private static bool IsKeyStart(string text, int i) =>
        IsKeyChar(text[i]) && (i == 0 || !char.IsLetterOrDigit(text[i - 1]) || AfterEscape(text, i));

    /// <summary>Anahtar bir kaçıştan hemen sonra başlayabilir: <c>%22</c>, <c>"</c>, <c>\n</c>/<c>\r</c>/<c>\t</c>.</summary>
    private static bool AfterEscape(string text, int i) =>
        (i >= 3 && text[i - 3] == '%' && char.IsAsciiHexDigit(text[i - 2]) && char.IsAsciiHexDigit(text[i - 1]))
        || (i >= 6 && text[i - 6] == '\\' && text[i - 5] == 'u' && IsHex4(text, i - 4))
        || (i >= 2 && text[i - 2] == '\\' && text[i - 1] is 'n' or 'r' or 't');

    private static bool IsHex4(string text, int p) =>
        p + 4 <= text.Length && char.IsAsciiHexDigit(text[p]) && char.IsAsciiHexDigit(text[p + 1])
        && char.IsAsciiHexDigit(text[p + 2]) && char.IsAsciiHexDigit(text[p + 3]);

    private static bool IsSecretKey(string text, int keyStart, int keyEnd, out bool apiKey)
    {
        apiKey = false;
        if (keyEnd - keyStart < 3)
        {
            // Kısa ad kendisi gizli olamaz; yalnızca yolu (Ai:ApiKeys:0) gizli olabilir (hunt r4, perf).
            return keyStart > 0 && text[keyStart - 1] == ':' && PathHasSecretSegment(text, keyStart, out apiKey);
        }

        if (IsDescriptiveName(text, keyStart, keyEnd))
        {
            // "PasswordPolicyException: mesaj", "PasswordResetService: mesaj", Identity kodu "PasswordRequiresDigit: …":
            // tip/kod adıdır, ardındaki metin değer değildir (hunt r6/r7, hasar). Düz gizli ad (Smtp.Password:) etkilenmez.
            return false;
        }

        var name = SecretMasker.NormalizeFieldName(text[keyStart..keyEnd]);
        if (name.Length == 0 || SecretMasker.IsConnectionStringFieldName(name))
        {
            return false;
        }

        // Sorgu dizesindeki "key" parametresi (Google vb.): ?key=… / &key=…
        if (name == "key" && keyStart > 0 && text[keyStart - 1] is '?' or '&')
        {
            apiKey = true;
            return true;
        }

        apiKey = SecretMasker.IsApiKeyFieldName(name);
        if (apiKey || SecretMasker.IsHiddenFieldName(name))
        {
            return true;
        }

        // .NET ortam değişkeni yolu: Ai__ApiKeys__ElevenLabs (H3-D).
        var raw = text[keyStart..keyEnd];
        if (raw.Contains("__", StringComparison.Ordinal))
        {
            foreach (var segment in raw.Split("__", StringSplitOptions.RemoveEmptyEntries))
            {
                var segmentName = SecretMasker.NormalizeFieldName(segment);
                if (SecretMasker.IsApiKeyFieldName(segmentName) || SecretMasker.IsHiddenFieldName(segmentName))
                {
                    apiKey = SecretMasker.IsApiKeyFieldName(segmentName);
                    return true;
                }
            }
        }

        return PathHasSecretSegment(text, keyStart, out apiKey);
    }

    /// <summary><c>Ai:ApiKeys:Gemini</c> gibi bir yolda, anahtardan önceki bölümlerden biri gizli mi.</summary>
    private static bool PathHasSecretSegment(string text, int keyStart, out bool apiKey)
    {
        apiKey = false;
        var q = keyStart - 1;
        // Yalnızca ':' — '.' ad alanlarını (Microsoft.AspNetCore.Authorization.…: mesaj) gizli yol sanıyordu (hunt r4, hasar).
        for (var depth = 0; depth < 8 && q > 0 && text[q] == ':'; depth++)
        {
            var segEnd = q;
            q--;
            while (q >= 0 && IsKeyChar(text[q]) && segEnd - q <= MaxKeyLength)
            {
                q--;
            }

            if (segEnd - q - 1 < 3)
            {
                // Gizli ad en az 3 harf: "1c:2e:…" gibi kısa bölümler normalleştirilmeden geçilir (hunt r4, perf).
                if (segEnd - q - 1 == 0)
                {
                    return false;
                }

                continue;
            }

            var name = SecretMasker.NormalizeFieldName(text[(q + 1)..segEnd]);

            if (SecretMasker.IsApiKeyFieldName(name) || SecretMasker.IsHiddenFieldName(name))
            {
                apiKey = SecretMasker.IsApiKeyFieldName(name);
                return true;
            }
        }

        return false;
    }

    private static bool IsInlineSpace(char c) => c is ' ' or '\t' or ' ' or ' ' or ' ';

    private static bool IsZeroWidth(char c) => c is '​' or '‌' or '‍' or '⁠' or '﻿' or '‎' or '‏';

    private static int SkipInlineSpace(string text, int p)
    {
        while (p < text.Length && (IsInlineSpace(text[p]) || IsZeroWidth(text[p])))
        {
            p++;
        }

        return p;
    }

    /// <summary>Konumda bir tırnak var mı (düz, ters bölü kaçışlı, <c>"</c>/<c>'</c>, <c>%22</c>/<c>%27</c>); varsa uzunluğu.</summary>
    private static int QuoteLength(string text, int p)
    {
        if (p >= text.Length)
        {
            return 0;
        }

        var q = p;
        while (q < text.Length && text[q] == '\\')
        {
            q++;
        }

        if (q < text.Length && text[q] is '"' or '\'')
        {
            return q - p + 1;
        }

        if (text[p] == '\\' && p + 5 < text.Length && text[p + 1] == 'u' && (string.CompareOrdinal(text, p + 2, "0022", 0, 4) == 0 || string.CompareOrdinal(text, p + 2, "0027", 0, 4) == 0))
        {
            return 6;
        }

        if (text[p] == '%' && p + 2 < text.Length && text[p + 1] == '2' && text[p + 2] is '2' or '7')
        {
            return 3;
        }

        return 0;
    }

    /// <summary>Kesme işaretli Türkçe ek: <c>Token'ınız</c>, <c>Şifre'yi</c> (en fazla 6 harf); yoksa konum değişmez.</summary>
    private static int SkipTurkishSuffix(string text, int pos)
    {
        if (pos + 1 >= text.Length || text[pos] is not ('\'' or '’') || !char.IsLetter(text[pos + 1]))
        {
            return pos;
        }

        var p = pos + 1;
        while (p < text.Length && p - pos <= 6 && char.IsLetter(text[p]))
        {
            p++;
        }

        return p < text.Length && char.IsLetter(text[p]) ? pos : p;
    }

    /// <summary>Anahtardan sonra: isteğe bağlı tırnak, boşluk, ayırıcı, boşluk.</summary>
    private static bool TryReadSeparator(string text, int pos, out int valueStart, out bool urlEncoded)
    {
        valueStart = pos;
        urlEncoded = false;
        var p = SkipTurkishSuffix(text, pos);
        p = p > pos ? p : SkipKeyDecoration(text, pos + QuoteLength(text, pos));
        p = SkipInlineSpace(text, p);
        if (p + 1 < text.Length && text[p] is ':' or '：' && text[p + 1] == '=')
        {
            p += 2;
        }
        else if (p < text.Length && text[p] is '=' or ':' or '＝' or '：')
        {
            p++;
        }
        else if (p + 2 < text.Length && text[p] == '%' && text[p + 1] == '3' && text[p + 2] is 'D' or 'd' or 'A' or 'a')
        {
            p += 3;
            urlEncoded = true;
        }
        else
        {
            return false;
        }

        // "https://" gibi URL şemaları anahtar/değer değildir.
        if (text[p - 1] == ':' && p + 1 < text.Length && text[p] == '/' && text[p + 1] == '/')
        {
            return false;
        }

        valueStart = SkipInlineSpace(text, p);
        return true;
    }

    /// <summary>
    /// Anahtarla ayırıcı arasındaki süsleri atlar (H3-D): Serilog sözlüğü <c>["Password"]=</c>, form dizisi
    /// <c>password[]</c>/<c>Passwords[0]</c>/<c>user[password]</c>/<c>%5D</c>, Markdown <c>**Password**:</c>, <c>`password`:</c>.
    /// </summary>
    private static int SkipKeyDecoration(string text, int p)
    {
        for (var round = 0; round < 3 && p < text.Length; round++)
        {
            var start = p;
            if (text[p] == '[')
            {
                var q = p + 1;
                while (q < text.Length && q - p <= 7 && char.IsAsciiDigit(text[q]))
                {
                    q++;
                }

                if (q < text.Length && text[q] == ']')
                {
                    p = q + 1;
                }
            }
            else if (text[p] == ']')
            {
                p++;
            }
            else if (p + 2 < text.Length && text[p] == '%' && text[p + 1] == '5' && text[p + 2] is 'D' or 'd')
            {
                p += 3;
            }
            else
            {
                while (p < text.Length && p - start < 3 && text[p] is '*' or '`')
                {
                    p++;
                }
            }

            if (p == start)
            {
                break;
            }
        }

        return p;
    }

    /// <summary>
    /// Anahtar bir bağlantı dizesinin içinde mi: önceki ';' bölümü <c>Ad=değer</c> içeriyor (<c>Host=db;Password=</c>) ya da
    /// anahtar satır başında ve sonraki ilk ';'den sonra yeni bir <c>Ad=</c> geliyor. Düzyazıdaki "signed in; SessionId=…"
    /// bağlantı dizesi değildir (hunt r11, hasar).
    /// </summary>
    private static bool ConnectionStringContext(string text, int keyStart)
    {
        var p = keyStart - 1;
        while (p >= 0 && IsInlineSpace(text[p]))
        {
            p--;
        }

        if (p >= 0)
        {
            if (text[p] != ';')
            {
                return false;
            }

            var segmentStart = Math.Max(text.LastIndexOfAny([';', '\n', '\r'], Math.Max(p - 1, 0), Math.Min(p, 256)) + 1, p - 256);
            return p > 0 && text.IndexOf('=', Math.Max(segmentStart, 0), p - Math.Max(segmentStart, 0)) >= 0;
        }

        var lineEnd = LineEnd(text, keyStart);
        var semicolon = text.IndexOf(';', keyStart, lineEnd - keyStart);
        return semicolon >= 0 && LooksLikeKeySeparator(text, semicolon + 1, allowColon: false, allowSpacedName: true);
    }

    /// <summary>Değeri okur: (başlangıç, bitiş, değerden sonraki konum — kapanan tırnak dahil).</summary>
    private static (int Start, int End, int After) ReadValue(string text, int pos, bool connectionString, bool urlEncoded)
    {
        if (pos >= text.Length)
        {
            return (pos, pos, pos);
        }

        var quote = QuoteLength(text, pos);
        if (quote > 0)
        {
            if (text[pos] == '%' || (quote == 6 && text[pos + 1] == 'u'))
            {
                return ReadDelimited(text, pos + quote, text.Substring(pos, quote));
            }

            if (quote > 1)
            {
                return ReadEscapedQuoted(text, pos + quote - 1, quote - 1);
            }

            return ReadQuoted(text, pos, text[pos]);
        }

        // Satırda eşleşen kapanışa kadar (tek satırlık JSON'da sonraki alanlar korunsun, hunt r4); yoksa satır sonu.
        // Bağlantı dizesinde ayraç yalnızca kapanıştan sonra ';' ya da satır sonu geliyorsa alıntıdır
        // (Pwd={p@ss;w0rd}; ve {"pwd":"a;b"}); değilse sıradan karakterdir (Password={abc}def123; → ';'e kadar) (hunt r6/r7).
        var bracketLineEnd = text[pos] is '{' or '[' ? LineEnd(text, pos) : pos;
        // Bağlantı dizesinde alıntı en fazla 256 karakter aranır: "Pwd={a;" tekrarında karesel süre olmasın (hunt r8, perf).
        var bracketScanEnd = connectionString ? Math.Min(bracketLineEnd, pos + 256) : bracketLineEnd;
        var bracketClose = bracketLineEnd > pos ? MatchingBracket(text, pos, bracketScanEnd) : -1;
        var quotedByBracket = bracketClose >= 0
            && (bracketClose + 1 >= bracketLineEnd || char.IsWhiteSpace(text[bracketClose + 1]) || text[bracketClose + 1] is ';' or '"' or '\'');
        // Serbest metinde de kapanış değeri bitirmiyorsa ("Password = {Hunter}2024") ayraç alıntı değildir (hunt r12).
        var closesValue = bracketClose >= 0
            && (bracketClose + 1 >= bracketLineEnd || char.IsWhiteSpace(text[bracketClose + 1])
                || text[bracketClose + 1] is ';' or ',' or '"' or '\'' or '}' or ']' or ')');
        if (text[pos] is '{' or '[' && (connectionString ? quotedByBracket : bracketClose < 0 || closesValue))
        {
            var lineEnd = bracketLineEnd;
            var close = bracketClose;
            return close < 0 ? (pos, lineEnd, lineEnd) : (pos, close + 1, close + 1);
        }

        var end = pos;
        while (end < text.Length && !IsValueEnd(text, end, pos, connectionString, urlEncoded))
        {
            end++;
        }

        var trimmed = Trim(text, pos, end);
        return (pos, trimmed, trimmed);
    }

    /// <summary>Tırnak içini atlayarak <paramref name="open"/>'daki ayracın satırdaki eşini bulur; yoksa -1.</summary>
    private static int MatchingBracket(string text, int open, int lineEnd)
    {
        var depth = 0;
        var quote = '\0';
        for (var p = open; p < lineEnd; p++)
        {
            var c = text[p];
            if (quote != '\0')
            {
                if (c == '\\')
                {
                    p++;
                }
                else if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (c == '"')
            {
                quote = c;
            }
            else if (c is '{' or '[')
            {
                depth++;
            }
            else if (c is '}' or ']' && --depth == 0)
            {
                return p;
            }
        }

        return -1;
    }

    private static int Trim(string text, int start, int end)
    {
        while (end > start && (char.IsWhiteSpace(text[end - 1]) || IsZeroWidth(text[end - 1])))
        {
            end--;
        }

        return end;
    }

    /// <summary>Kaçış derinliği <paramref name="depth"/> olan tırnaklı değer: aynı sayıda ters bölü + tırnakla biter.</summary>
    private static (int Start, int End, int After) ReadEscapedQuoted(string text, int quotePos, int depth)
    {
        var quote = text[quotePos];
        var start = quotePos + 1;
        var lineEnd = LineEnd(text, start);
        for (var p = start; p < lineEnd; p++)
        {
            if (text[p] != quote)
            {
                continue;
            }

            var run = 0;
            while (p - run - 1 >= start && text[p - run - 1] == '\\')
            {
                run++;
            }

            if (run == depth)
            {
                return (start, p - depth, p + 1);
            }
        }

        return (start, lineEnd, lineEnd);
    }

    /// <summary>Kodlanmış tırnakla (<c>%22</c>, <c>"</c>) sınırlı değer; kapanmazsa satır sonu.</summary>
    private static (int Start, int End, int After) ReadDelimited(string text, int start, string delimiter)
    {
        var lineEnd = LineEnd(text, start);
        var close = text.IndexOf(delimiter, start, lineEnd - start, StringComparison.OrdinalIgnoreCase);
        return close < 0 ? (start, lineEnd, lineEnd) : (start, close, close + delimiter.Length);
    }

    private static (int Start, int End, int After) ReadQuoted(string text, int pos, char quote)
    {
        var p = pos + 1;
        while (p < text.Length && text[p] is not ('\r' or '\n'))
        {
            if (text[p] == '\\' && p + 1 < text.Length)
            {
                p += 2;
                continue;
            }

            if (text[p] == quote)
            {
                if (p + 1 < text.Length && text[p + 1] == quote)
                {
                    p += 2; // "" kaçışı (bağlantı dizesi).
                    continue;
                }

                return (pos + 1, p, p + 1);
            }

            p++;
        }

        return (pos + 1, Math.Min(p, text.Length), Math.Min(p, text.Length)); // Kapanmamış tırnak: satır sonuna kadar.
    }

    private static int LineEnd(string text, int pos)
    {
        var end = text.IndexOfAny(['\r', '\n'], Math.Min(pos, text.Length));
        return end < 0 ? text.Length : end;
    }

    private static bool IsValueEnd(string text, int p, int valueStart, bool connectionString, bool urlEncoded)
    {
        var c = text[p];
        if (c is '\r' or '\n')
        {
            return true;
        }

        if (connectionString)
        {
            // ';' yalnızca ardından yeni bir "Ad=" (ya da metin/boşluk sonu) geliyorsa biter: Password=Hunter;2024 tümüyle gizli (hunt r9).
            return c == ';' && (p + 1 >= text.Length || char.IsWhiteSpace(text[p + 1]) || text[p + 1] is '"' or '\'' || LooksLikeKeySeparator(text, p + 1, allowColon: false, allowSpacedName: true));
        }

        if (urlEncoded)
        {
            return char.IsWhiteSpace(c) || c == '&' || (c == '%' && p + 2 < text.Length && text[p + 1] == '2' && text[p + 2] == '6');
        }

        return c switch
        {
            ';' => LooksLikeKeySeparator(text, p + 1, allowColon: false),
            ',' => LooksLikeKeySeparator(text, p + 1, allowColon: true),
            '&' => LooksLikeKeySeparator(text, p + 1, allowColon: false),
            '}' => p > valueStart && (p + 1 == text.Length || text[p + 1] is ',' or ';' or ' ' or '\t' or '\r' or '\n' or '}' or ']' or ')'),
            _ => false,
        };
    }

    /// <summary>Konumdan sonra (sınırlı bakış) <c>[boşluk]["]Ad["][boşluk]=</c> ya da <c>:</c> geliyor mu.</summary>
    private static bool LooksLikeKeySeparator(string text, int p, bool allowColon, bool allowSpacedName = false)
    {
        var limit = Math.Min(text.Length, p + LookaheadNameLength + 8);
        while (p < limit && IsInlineSpace(text[p]))
        {
            p++;
        }

        if (p < limit && text[p] is '"' or '\'')
        {
            p++;
        }

        // Bağlantı dizesi anahtarları boşluklu olabilir: "User Id", "Include Error Detail" (hunt r10).
        var nameStart = p;
        while (p < limit && (IsKeyChar(text[p]) || (allowSpacedName && text[p] == ' ' && p > nameStart && p + 1 < limit && char.IsLetter(text[p + 1]))))
        {
            p++;
        }

        if (p == nameStart)
        {
            return false;
        }

        if (p < limit && text[p] is '"' or '\'')
        {
            p++;
        }

        while (p < limit && IsInlineSpace(text[p]))
        {
            p++;
        }

        return p < limit && (text[p] == '=' || (allowColon && text[p] == ':'));
    }
}
