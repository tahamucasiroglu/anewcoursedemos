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
/// <item>API key alanında yalnızca ilk boşluksuz parça key sayılır; daha önce maskelenmişse (<c>sk-…xyz</c>) korunur.</item>
/// </list>
/// </summary>
internal static class SecretKeyValueScanner
{
    /// <summary>Anahtar olarak okunacak en uzun ad; daha uzunu anahtar sayılmaz.</summary>
    public const int MaxKeyLength = 256;

    private const int LookaheadNameLength = 64;

    /// <summary>Metindeki gizli anahtar/değer çiftlerinin değerini maskeler.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Maskelenmiş metin (değişiklik yoksa aynı örnek).</returns>
    public static string Mask(string text)
    {
        var output = new SecretMaskOutput(text);
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

            if (keyEnd - i > MaxKeyLength || !IsSecretKey(text, i, keyEnd, out var apiKey))
            {
                i = keyEnd;
                continue;
            }

            i = Math.Max(keyEnd, MaskPair(text, i, keyEnd, apiKey, output));
        }

        return output.Result();
    }

    /// <summary>Gizli anahtardan sonraki değeri bulup maskeler; taramanın süreceği konumu döner.</summary>
    private static int MaskPair(string text, int keyStart, int keyEnd, bool apiKey, SecretMaskOutput output)
    {
        if (TryXmlElement(text, keyStart, keyEnd, out var xmlStart, out var xmlEnd))
        {
            return Emit(text, xmlStart, xmlEnd, apiKey, output);
        }

        // KeyValuePair metni: [Password, değer]
        if (keyStart > 0 && text[keyStart - 1] == '[' && keyEnd + 1 < text.Length && text[keyEnd] == ',' && text[keyEnd + 1] == ' ')
        {
            var close = text.IndexOf(']', keyEnd);
            var lineEnd = LineEnd(text, keyEnd);
            return Emit(text, keyEnd + 2, close >= 0 && close < lineEnd ? close : lineEnd, apiKey, output);
        }

        if (!TryReadSeparator(text, keyEnd, out var valueStart, out var urlEncoded))
        {
            return MaskWithoutSeparator(text, keyStart, keyEnd, apiKey, output);
        }

        var (start, end, after) = ReadValue(text, valueStart, ConnectionStringContext(text, keyStart), urlEncoded);
        var lineEndOfValue = LineEnd(text, valueStart);
        if (end > start && IsBlockIndicator(text, start, lineEndOfValue))
        {
            return Math.Max(after, MaskIndentedLines(text, keyStart, lineEndOfValue, output));
        }

        if (end <= start)
        {
            // Boş değer: satırın kalanı boşsa (ya da yorum) değer sonraki girintili satırlardadır; değilse tarama sürer.
            return RestOfLineBlank(text, after, lineEndOfValue)
                ? Math.Max(after, MaskIndentedLines(text, keyStart, lineEndOfValue, output))
                : after;
        }

        var next = Emit(text, start, end, apiKey, output);
        if (text[start] is '{' or '[' && end == lineEndOfValue)
        {
            next = Math.Max(next, MaskIndentedLines(text, keyStart, lineEndOfValue, output));
        }

        return Math.Max(next, after);
    }

    /// <summary>Ayırıcısız biçimler: CLI bayrağı <c>--password değer</c> ve SQL <c>PASSWORD 'değer'</c>.</summary>
    private static int MaskWithoutSeparator(string text, int keyStart, int keyEnd, bool apiKey, SecretMaskOutput output)
    {
        var p = SkipInlineSpace(text, keyEnd);
        if (p == keyEnd || p >= text.Length)
        {
            return keyEnd;
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

    /// <summary>XML öğesi: <c>&lt;[ns:]ad [öznitelik…]&gt;değer&lt;</c> (CDATA dahil).</summary>
    private static bool TryXmlElement(string text, int keyStart, int keyEnd, out int start, out int end)
    {
        start = end = 0;
        var q = keyStart - 1;
        if (q >= 0 && text[q] == ':')
        {
            q--;
            while (q >= 0 && (IsKeyChar(text[q]) || text[q] == '.'))
            {
                q--;
            }
        }

        if (q < 0 || text[q] != '<' || keyEnd >= text.Length || !(text[keyEnd] == '>' || char.IsWhiteSpace(text[keyEnd])))
        {
            return false;
        }

        var close = text.IndexOf('>', keyEnd);
        if (close < 0 || text[close - 1] == '/')
        {
            return false;
        }

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
            end = cdataEnd < 0 ? text.Length : cdataEnd + 3;
            return true;
        }

        var lt = text.IndexOf('<', start);
        end = Trim(text, start, lt < 0 ? text.Length : lt);
        return true;
    }

    private static int Emit(string text, int start, int end, bool apiKey, SecretMaskOutput output)
    {
        if (end <= start)
        {
            return Math.Max(start, end);
        }

        if (apiKey)
        {
            var tokenEnd = start;
            while (tokenEnd < end && !char.IsWhiteSpace(text[tokenEnd]))
            {
                tokenEnd++;
            }

            var token = text[start..tokenEnd];
            output.Replace(start, tokenEnd, token == SecretMasker.Hidden || SecretMasker.IsMaskedApiKey(token) ? token : SecretMasker.MaskApiKey(token));
            return tokenEnd;
        }

        output.Replace(start, end, SecretMasker.Hidden);
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
        var keyIndent = Indent(text, lineStart);
        var p = NextLineStart(text, from);
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

            if (indent <= keyIndent)
            {
                break;
            }

            output.Replace(contentStart, end, SecretMasker.Hidden);
            last = end;
            p = NextLineStart(text, end);
        }

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

        return PathHasSecretSegment(text, keyStart, out apiKey);
    }

    /// <summary><c>Ai:ApiKeys:Gemini</c> gibi bir yolda, anahtardan önceki bölümlerden biri gizli mi.</summary>
    private static bool PathHasSecretSegment(string text, int keyStart, out bool apiKey)
    {
        apiKey = false;
        var q = keyStart - 1;
        for (var depth = 0; depth < 8 && q > 0 && text[q] == ':'; depth++)
        {
            var segEnd = q;
            q--;
            while (q >= 0 && IsKeyChar(text[q]) && segEnd - q <= MaxKeyLength)
            {
                q--;
            }

            var name = SecretMasker.NormalizeFieldName(text[(q + 1)..segEnd]);
            if (name.Length == 0)
            {
                return false;
            }

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

    /// <summary>Anahtardan sonra: isteğe bağlı tırnak, boşluk, ayırıcı, boşluk.</summary>
    private static bool TryReadSeparator(string text, int pos, out int valueStart, out bool urlEncoded)
    {
        valueStart = pos;
        urlEncoded = false;
        var p = pos + QuoteLength(text, pos);
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

    private static bool ConnectionStringContext(string text, int keyStart)
    {
        var p = keyStart - 1;
        while (p >= 0 && IsInlineSpace(text[p]))
        {
            p--;
        }

        if (p >= 0)
        {
            return text[p] == ';';
        }

        var lineEnd = LineEnd(text, keyStart);
        return text.IndexOf(';', keyStart, lineEnd - keyStart) >= 0;
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

        if (text[pos] is '{' or '[')
        {
            var lineEnd = LineEnd(text, pos);
            return (pos, lineEnd, lineEnd);
        }

        var end = pos;
        while (end < text.Length && !IsValueEnd(text, end, pos, connectionString, urlEncoded))
        {
            end++;
        }

        var trimmed = Trim(text, pos, end);
        return (pos, trimmed, trimmed);
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
            return c == ';';
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
    private static bool LooksLikeKeySeparator(string text, int p, bool allowColon)
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

        var nameStart = p;
        while (p < limit && IsKeyChar(text[p]))
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
