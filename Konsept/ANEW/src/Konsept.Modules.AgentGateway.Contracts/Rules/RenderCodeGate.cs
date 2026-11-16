using System.Text.RegularExpressions;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Contracts.Rules;

/// <summary>
/// Render öncesi yasaklı yapı kapısı (spec 0018 Req 2–3, L1; Karar 5 §4): online modelin yazdığı video kodu ajana gitmeden önce Pi'de ve render
/// öncesi ajanda aynı kurallarla denetlenir. Kapı bir güvenlik sınırı değil, erken red katmanıdır (asıl sınırlar ajandaki iş klasörü, ağ
/// koruması, Job Object ve Node izinleridir); bu yüzden muhafazakârdır: şüpheli olan reddedilir, ret düzeltme döngüsüne gider.
/// Kurallar: boyut sınırı; URL şemaları ve protokolsüz <c>//</c>; kaçış dizileri (<c>\u</c>, <c>\x</c>, CSS onaltılık, <c>&amp;#</c>) — dolaylı
/// yazımla gizlenmeyi önler; <c>@import</c>; satır içi olay öznitelikleri; dış belge/bağlantı etiketleri; kaynak öznitelikleri ve CSS
/// <c>url()</c> yalnızca <see cref="AllowedResources"/>; betiklerde dinamik kod çalıştırma, ağ API'leri ve küresel nesnelere dolaylı erişim.
/// </summary>
public static partial class RenderCodeGate
{
    /// <summary>Kodun en büyük boyutu (karakter).</summary>
    public const int MaxLength = 200_000;

    /// <summary>Sahne klasöründe kodun kullanabileceği yerel dosyalar (ajan koyar).</summary>
    public static readonly IReadOnlyList<string> AllowedResources = ["image.png", "gsap.min.js"];

    private static readonly string[] ForbiddenIdentifiers =
    [
        "eval", "Function", "import", "require", "fetch", "XMLHttpRequest", "WebSocket", "WebTransport", "EventSource", "navigator",
        "RTCPeerConnection", "webkitRTCPeerConnection", "RTCDataChannel", "Worker", "SharedWorker", "serviceWorker", "importScripts",
        "process", "location", "setTimeout", "setInterval", "setImmediate", "constructor", "__proto__", "Reflect", "Proxy", "WebAssembly",
        "BroadcastChannel", "indexedDB", "caches", "open", "write", "writeln", "insertAdjacentHTML", "outerHTML", "srcdoc", "setAttribute",
        "setAttributeNS",
    ];

    /// <summary>Bilinen GSAP CDN adresini sahne klasöründeki yerel kopyaya çevirir (L0 sözleşmesi; iki tarafta aynı).</summary>
    /// <param name="code">Kod.</param>
    /// <returns>Normalleştirilmiş kod.</returns>
    public static string Normalize(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return GsapCdn().Replace(code, "gsap.min.js");
    }

    /// <summary>Kodu denetler (önce <see cref="Normalize"/> uygulanmış olmalı).</summary>
    /// <param name="code">HTML belgesi.</param>
    /// <returns>Karar.</returns>
    public static RenderCodeVerdict Check(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RenderCodeVerdict.Reject("empty document");
        }

        if (code.Length > MaxLength)
        {
            return RenderCodeVerdict.Reject("document too large");
        }

        code = XmlNamespace().Replace(code, "\"\""); // SVG/XHTML ad alanı adresleri bir şey indirmez; denetimden önce çıkarılır.

        if (Scheme().IsMatch(code) || ScriptScheme().IsMatch(code) || NetworkPath().IsMatch(code))
        {
            return RenderCodeVerdict.Reject("external url or network path");
        }

        if (Escape().IsMatch(code))
        {
            return RenderCodeVerdict.Reject("escape sequence (write characters directly)");
        }

        if (code.Contains("@import", StringComparison.OrdinalIgnoreCase))
        {
            return RenderCodeVerdict.Reject("forbidden: @import");
        }

        if (InlineHandler().Match(code) is { Success: true } handler)
        {
            return RenderCodeVerdict.Reject("forbidden inline event handler: " + handler.Groups[1].Value);
        }

        if (ForbiddenTag().Match(code) is { Success: true } tag)
        {
            return RenderCodeVerdict.Reject("forbidden tag: " + tag.Groups[1].Value.ToLowerInvariant());
        }

        foreach (Match match in ResourceAttribute().Matches(code))
        {
            if (!IsAllowedResource(match.Groups[3].Value))
            {
                return RenderCodeVerdict.Reject("resource not allowed: " + Short(match.Groups[3].Value));
            }
        }

        foreach (Match match in CssUrl().Matches(code))
        {
            if (!IsAllowedResource(match.Groups[1].Value))
            {
                return RenderCodeVerdict.Reject("resource not allowed: " + Short(match.Groups[1].Value));
            }
        }

        foreach (var script in Scripts(code))
        {
            if (ComputedGlobal().Match(script) is { Success: true } global)
            {
                return RenderCodeVerdict.Reject("forbidden: computed access on " + global.Groups[1].Value);
            }

            foreach (var name in ForbiddenIdentifiers)
            {
                if (Regex.IsMatch(script, @"(?<![\w$])" + Regex.Escape(name) + @"(?![\w$])", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
                {
                    return RenderCodeVerdict.Reject("forbidden: " + name);
                }
            }
        }

        return RenderCodeVerdict.Pass;
    }

    private static bool IsAllowedResource(string value) =>
        value.Length == 0 || value.StartsWith('#') && AnchorId().IsMatch(value) || AllowedResources.Contains(value, StringComparer.Ordinal);

    private static string Short(string value) => value.Length > 40 ? value[..40] : value;

    private static IEnumerable<string> Scripts(string code)
    {
        var index = 0;
        while (true)
        {
            var open = code.IndexOf("<script", index, StringComparison.OrdinalIgnoreCase);
            if (open < 0)
            {
                yield break;
            }

            var start = code.IndexOf('>', open);
            if (start < 0)
            {
                yield return code[open..];
                yield break;
            }

            var close = code.IndexOf("</script", start, StringComparison.OrdinalIgnoreCase);
            yield return close < 0 ? code[(start + 1)..] : code[(start + 1)..close];
            if (close < 0)
            {
                yield break;
            }

            index = close + 8;
        }
    }

    [GeneratedRegex(@"https?://(?:cdn\.jsdelivr\.net/npm/gsap@[0-9.]+/dist|cdnjs\.cloudflare\.com/ajax/libs/gsap/[0-9.]+|unpkg\.com/gsap@[0-9.]+/dist)/gsap\.min\.js", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex GsapCdn();

    [GeneratedRegex(@"\b(?:https?|wss?|ftps?|file|smb|ms-[a-z-]+)\s*:\s*[\\/]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Scheme();

    [GeneratedRegex(@"[""'`(=]\s*(?:data|blob|javascript|vbscript|filesystem|about|chrome|view-source)\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ScriptScheme();

    [GeneratedRegex(@"[""'`(=]\s*[\\/]{2}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex NetworkPath();

    [GeneratedRegex(@"\\(?:u|x|[0-9a-fA-F])|&#", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Escape();

    [GeneratedRegex(@"(?<![\w$.-])(on[a-z]+)\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex InlineHandler();

    [GeneratedRegex(@"<\s*(iframe|object|embed|frame|frameset|base|link|form|portal|applet|meta\s[^>]*http-equiv)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ForbiddenTag();

    [GeneratedRegex(@"\b(src|href|srcset|poster|action|formaction|ping|manifest|longdesc|lowsrc|dynsrc)\s*=\s*([""'`]?)([^""'`\s>]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ResourceAttribute();

    [GeneratedRegex(@"\burl\s*\(\s*[""']?([^""')\s]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CssUrl();

    [GeneratedRegex(@"(?<![\w$])(window|self|globalThis|top|parent|frames|opener|document)\s*(?:\?\.)?\s*\[", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ComputedGlobal();

    [GeneratedRegex(@"^#[A-Za-z0-9_-]*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex AnchorId();

    [GeneratedRegex(@"[""']http://www\.w3\.org/(?:2000/svg|1999/xlink|1999/xhtml|XML/1998/namespace)[""']", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex XmlNamespace();
}
