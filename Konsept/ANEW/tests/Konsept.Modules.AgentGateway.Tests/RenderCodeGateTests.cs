using Konsept.Modules.AgentGateway.Contracts.Rules;
using Shouldly;

namespace Konsept.Modules.AgentGateway.Tests;

/// <summary>
/// Render öncesi yasaklı yapı kapısı (spec 0018 Req 3, AC-4; rapor §4 T1–T6, T10–T11). Kapı Pi'de ve ajanda aynıdır. Kasıtlı bozma: ilgili
/// kural (ör. <c>ForbiddenIdentifiers</c>'tan <c>fetch</c>, <c>Escape</c> deseni) kaldırılınca ilgili satır kırmızıya döner.
/// </summary>
public sealed class RenderCodeGateTests
{
    private const string Harmless = """
        <!doctype html>
        <html lang="tr"><head><meta charset="UTF-8" />
        <script src="https://cdn.jsdelivr.net/npm/gsap@3.15.0/dist/gsap.min.js"></script>
        <style>html,body{margin:0;width:1080px;height:1920px;overflow:hidden;background:#123 url(image.png)}</style></head>
        <body>
        <div id="root" data-composition-id="main" data-start="0" data-duration="5" data-width="1080" data-height="1920">
          <img class="clip" src="image.png" data-start="0" data-duration="5" data-track-index="0" />
          <svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"><circle cx="50" cy="50" r="40" fill="url(#g)" /></svg>
          <h1 id="t" class="clip" data-start="0" data-duration="5" data-track-index="1">Bizans'ın son sabahı — "İstanbul"</h1>
        </div>
        <script>
          const tl = gsap.timeline({ paused: true });
          const words = ["Açılış", "Son"];
          tl.fromTo("#t", { opacity: 0, y: 40 }, { opacity: 1, y: 0, duration: 1, ease: "power2.out" }, 0);
          window.__timelines["main"] = tl;
        </script>
        </body></html>
        """;

    /// <summary>AC-5 / T11 (aşırı sıkılaştırma koruması): zararsız şablon kod geçer; bilinen GSAP CDN adresi yerel kopyaya çevrilir.</summary>
    [Fact]
    public void HarmlessTemplate_Passes_AfterCdnNormalized()
    {
        var code = RenderCodeGate.Normalize(Harmless);
        code.ShouldContain("<script src=\"gsap.min.js\"></script>");
        code.ShouldNotContain("cdn.jsdelivr.net");
        RenderCodeGate.Check(code).ShouldSatisfyAllConditions(v => v.Ok.ShouldBeTrue(), v => v.Reason.ShouldBeNull());
    }

    /// <summary>
    /// AC-1 / AC-2 / AC-4 (T1–T6, T10): ağ API'leri, uzak kaynaklar, IP adresi, WebSocket/WebRTC, <c>file://</c>, klasör dışı yol, dinamik kod
    /// ve dolaylı erişimle gizlenmiş hâlleri reddedilir.
    /// </summary>
    /// <param name="snippet">Zararsız belgeye eklenen parça.</param>
    [Theory]
    [InlineData("<script>fetch('/x')</script>")]
    [InlineData("<script>new XMLHttpRequest()</script>")]
    [InlineData("<img src=\"http://192.168.1.100:18777/x.png\" />")]
    [InlineData("<img src=\"//evil.example/x.png\" />")]
    [InlineData("<div style=\"background:url(https://evil.example/a.png)\"></div>")]
    [InlineData("<script>const s = new WebSocket(\"x\");</script>")]
    [InlineData("<script>const p = new RTCPeerConnection();</script>")]
    [InlineData("<script>navigator.sendBeacon('/a')</script>")]
    [InlineData("<img src=\"file:///C:/Windows/win.ini\" />")]
    [InlineData("<img src=\"../../token.dat\" />")]
    [InlineData("<img src=\"..%2f..%2fsecret\" />")]
    [InlineData("<iframe src=\"image.png\"></iframe>")]
    [InlineData("<link rel=\"stylesheet\" href=\"a.css\" />")]
    [InlineData("<style>@import 'a.css';</style>")]
    [InlineData("<img src=\"image.png\" onerror=\"x()\" />")]
    [InlineData("<script>eval('1')</script>")]
    [InlineData("<script>new Function('return 1')()</script>")]
    [InlineData("<script>import('./m.js')</script>")]
    [InlineData("<script>window[\"fe\" + \"tch\"]('/x')</script>")]
    [InlineData("<script>globalThis['ev' + 'al']('1')</script>")]
    [InlineData("<script>[].constructor.constructor('return 1')()</script>")]
    [InlineData("<script>\\u0066etch('/x')</script>")]
    [InlineData("<script>const u = \"\\x68ttp\";</script>")]
    [InlineData("<img src=\"&#104;ttp://a/b\" />")]
    [InlineData("<script>setTimeout(\"go()\", 1)</script>")]
    [InlineData("<script>location.href = 'x'</script>")]
    [InlineData("<script>const img = document.createElement('img'); img.src = base;</script>")]
    [InlineData("<script>el.setAttribute('src', u)</script>")]
    [InlineData("<a href=\"javascript:go()\">x</a>")]
    [InlineData("<img src=\"data:image/png;base64,AAAA\" />")]
    [InlineData("<script>open('x')</script>")]
    [InlineData("<script>const w = new Worker('w.js')</script>")]
    [InlineData("<img src=\"\\\\\\\\server\\\\share\\\\a.png\" />")]
    public void ForbiddenConstructs_Rejected(string snippet)
    {
        var code = Harmless.Replace("</body>", snippet + "</body>", StringComparison.Ordinal);
        var verdict = RenderCodeGate.Check(RenderCodeGate.Normalize(code));
        verdict.Ok.ShouldBeFalse(snippet);
        verdict.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>Boş ve aşırı büyük belge reddedilir (karmaşıklık sınırı).</summary>
    [Fact]
    public void EmptyOrHuge_Rejected()
    {
        RenderCodeGate.Check(string.Empty).Ok.ShouldBeFalse();
        RenderCodeGate.Check(Harmless + new string(' ', RenderCodeGate.MaxLength)).Ok.ShouldBeFalse();
    }

    /// <summary>Betik dışındaki görünür metin (Türkçe anlatım) yasaklı sözcük içerse de reddedilmez; yalnızca betik denetlenir.</summary>
    [Fact]
    public void VisibleText_WithWords_NotRejected()
    {
        var code = Harmless.Replace("Bizans'ın son sabahı", "Kapıyı open etmek ve fetch sözcüğü yalnızca metin", StringComparison.Ordinal);
        RenderCodeGate.Check(RenderCodeGate.Normalize(code)).Ok.ShouldBeTrue();
    }
}
