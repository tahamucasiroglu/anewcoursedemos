using System.Text.RegularExpressions;
using TahaKonsept.Agent.Engines.Video.Safety.Base;

namespace TahaKonsept.Agent.Engines.Video.Safety.Concrete;

/// <summary>
/// Tüm motorlar için varsayılan güvenlik kuralları (PLAN §3: ağ erişimi kapalı, <c>file://</c> yasak, deterministik kod).
/// SVG/XML ad alanı adresleri (<c>http://www.w3.org/...</c>) serbesttir.
/// </summary>
public sealed class DefaultSceneCodeGuard : SceneCodeGuardBase
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly IReadOnlyList<SceneCodeRule> DefaultRules =
    [
        new(new Regex(@"\bfetch\s*\(", Options), "fetch() kullanılamaz (ağ erişimi kapalı)."),
        new(new Regex(@"\bXMLHttpRequest\b", Options), "XMLHttpRequest kullanılamaz (ağ erişimi kapalı)."),
        new(new Regex(@"\b(WebSocket|EventSource)\b", Options), "WebSocket/EventSource kullanılamaz (ağ erişimi kapalı)."),
        new(new Regex(@"navigator\s*\.\s*sendBeacon", Options), "sendBeacon kullanılamaz (ağ erişimi kapalı)."),
        new(new Regex(@"\bimport\s*\(", Options), "Dinamik import() kullanılamaz."),
        new(new Regex(@"file:/", Options), "file:// adresleri yasak; dosyalara yalnızca göreli adla başvurun."),
        new(new Regex(@"(?<![\w.])(https?:)?//(?!www\.w3\.org/)[a-z0-9.-]+\.[a-z]{2,}", Options), "Dış adres (http/https/CDN) kullanılamaz; yalnızca paketlenmiş yerel dosyalar."),
        new(new Regex(@"@import\s+url", Options), "CSS @import url kullanılamaz (dış font/stil yok)."),
        new(new Regex(@"\bMath\s*\.\s*random\s*\(", Options), "Math.random() yasak (render deterministik olmalı); sabit tohumlu sözde rastgele kullanın."),
        new(new Regex(@"\bDate\s*\.\s*now\s*\(|\bnew\s+Date\s*\(\s*\)", Options), "Date.now()/new Date() yasak (render deterministik olmalı)."),
        new(new Regex(@"\b(require\s*\(\s*['""](fs|child_process|net|http|https)['""]|process\s*\.\s*env)", Options), "Node API'leri (fs, child_process, process.env…) kullanılamaz."),
        new(new Regex(@"\beval\s*\(|new\s+Function\s*\(", Options), "eval()/new Function() kullanılamaz."),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<SceneCodeRule> Rules => DefaultRules;
}
