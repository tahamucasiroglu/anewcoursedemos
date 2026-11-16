using System.Text.RegularExpressions;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Render sayfasının içerik güvenliği politikası (spec 0018 L5; review r1 H-1): ajan, LLM kodunu sahne klasörüne yazarken başa kendi CSP
/// <c>&lt;meta&gt;</c> etiketini ekler. <c>'unsafe-eval'</c> yok (dolaylı erişimle kurulan <c>Function</c>/<c>eval</c> çalışma anında reddedilir),
/// bağlantı, görsel, medya ve yazı tipi yalnızca motorun yerel sunucusundan (<c>'self'</c>), çerçeve/nesne yok; işçi yalnızca motorun kendi
/// kare yakalama işçisi için (<c>'self' blob:</c>; LLM kodu <c>Worker</c> adını kullanamaz — kapı). LLM kendi
/// <c>meta http-equiv</c> etiketini yazamaz (kapı reddeder); tarayıcı birden çok politikanın kesişimini uygular.
/// </summary>
public static partial class RenderCsp
{
    /// <summary>Politika.</summary>
    public const string Policy =
        "default-src 'none'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; " +
        "media-src 'self' blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; child-src 'none'; frame-src 'none'; " +
        "object-src 'none'; base-uri 'none'; form-action 'none'";

    /// <summary>Eklenen etiket.</summary>
    public const string Tag = "<meta http-equiv=\"Content-Security-Policy\" content=\"" + Policy + "\">";

    /// <summary>
    /// Etiketi belgenin en başına (yalnızca baştaki <c>&lt;!doctype&gt;</c>'tan sonra) ekler: ayrıştırıcı onu örtük <c>&lt;head&gt;</c>'e alır,
    /// sonradan gelen <c>&lt;html&gt;</c>/<c>&lt;head&gt;</c> birleşir. Böylece hiçbir betik politikadan önce çalışmaz ve etiket bir dizenin
    /// içine düşemez (review r2 H-1).
    /// </summary>
    /// <param name="html">LLM'in yazdığı belge (kapıdan geçmiş).</param>
    /// <returns>Politikalı belge.</returns>
    public static string Apply(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        return LeadingDoctype().Match(html) is { Success: true } doctype
            ? html.Insert(doctype.Index + doctype.Length, Tag)
            : Tag + html;
    }

    [GeneratedRegex(@"\A\s*<!doctype\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex LeadingDoctype();
}
