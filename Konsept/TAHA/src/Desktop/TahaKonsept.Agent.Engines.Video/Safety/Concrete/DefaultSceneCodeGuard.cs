using System.Text.RegularExpressions;
using TahaKonsept.Agent.Engines.Video.Safety.Base;

namespace TahaKonsept.Agent.Engines.Video.Safety.Concrete;

/// <summary>
/// Tüm motorlar için varsayılan güvenlik kuralları (PLAN §3: ağ erişimi kapalı, <c>file://</c> yasak, deterministik kod).
/// SVG/XML ad alanı adresleri (<c>http://www.w3.org/...</c>) serbesttir. Açık <c>http(s)://</c> adreslerinin tümü (IP, <c>localhost</c>, port dahil),
/// protokolsüz <c>//alan.adı</c>, <c>//IP</c> ve <c>//localhost</c> reddedilir; <c>Function(...)</c> (new olmadan), küresel nesnelere köşeli parantezle
/// erişim (<c>window['fe'+'tch']</c>) ve WebRTC de yasaktır. Bu bir hızlı denetimdir (kara liste): asıl engel render sırasında tarayıcının ağ
/// kısıtlamasıdır (Revideo: ölü proxy; bkz. motorlar).
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
        new(new Regex(@"\bhttps?:\\?/\\?/(?!www\.w3\.org/)", Options), "Dış adres (http/https/CDN, IP, localhost) kullanılamaz; yalnızca paketlenmiş yerel dosyalar."),
        new(new Regex(@"(?<![\w.:/])//(?!www\.w3\.org/)([a-z0-9-]+\.)+[a-z]{2,}\b(?!\s*\()|(?<![\w.:/])//(\d{1,3}(\.\d{1,3}){3}|localhost\b|\[[0-9a-f:]+\])", Options), "Dış adres (//alan, //IP, //localhost) kullanılamaz; yalnızca paketlenmiş yerel dosyalar."),
        new(new Regex(@"\b(RTCPeerConnection|webkitRTCPeerConnection|RTCDataChannel)\b", Options), "WebRTC kullanılamaz (ağ erişimi kapalı)."),
        new(new Regex(@"\b(window|globalThis|self|top|parent|frames)\s*\[", Options), "Küresel nesneye köşeli parantezle erişim yasak (gizlenmiş API çağrısı)."),
        new(new Regex(@"@import\s+url", Options), "CSS @import url kullanılamaz (dış font/stil yok)."),
        new(new Regex(@"\bMath\s*\.\s*random\s*\(", Options), "Math.random() yasak (render deterministik olmalı); sabit tohumlu sözde rastgele kullanın."),
        new(new Regex(@"\bDate\s*\.\s*now\s*\(|\bnew\s+Date\s*\(\s*\)", Options), "Date.now()/new Date() yasak (render deterministik olmalı)."),
        new(new Regex(@"\b(require\s*\(\s*['""](fs|child_process|net|http|https)['""]|process\s*\.\s*env)", Options), "Node API'leri (fs, child_process, process.env…) kullanılamaz."),
        new(new Regex(@"\beval\s*\(|(?<![\w.])Function\s*\(|\.\s*constructor\s*\(\s*['""`]", Options), "eval()/Function() kullanılamaz."),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<SceneCodeRule> Rules => DefaultRules;
}
