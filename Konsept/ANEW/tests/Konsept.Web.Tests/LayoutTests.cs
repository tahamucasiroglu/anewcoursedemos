using System.Text.RegularExpressions;
using Konsept.Web.Tests.Support;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Üst bar, sayfalar ve tema (spec 0001 AC-1, AC-2, AC-3) — Razor çıktısı AngleSharp ile.
/// </summary>
public sealed partial class LayoutTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="LayoutTests"/> class.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public LayoutTests(WebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Gets her sayfa: yol, başlık, boş durum metni.</summary>
    public static TheoryData<string, string, string> Pages => new()
    {
        { "/", "Konseptler", "Henüz konsept yok." },
        { "/konseptten-video", "Konseptten Video", "Henüz videoya hazır konsept yok." },
        { "/uretilenler", "Üretilenler", "Henüz üretilmiş video yok." },
        { "/silinenler", "Silinenler", "Silinen bir şey yok." },
        { "/yayinlama", "Yayınlama", "Çok yakında sizlerleyiz" },
    };

    /// <summary>Üst bar tasarımdaki sırayla: logo + ad, aylık harcama, sayfalar, sağda kontroller ve tema düğmesi.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Layout_TopBar_RendersItemsInDesignOrder()
    {
        using var client = await _factory.CreateSignedInClientAsync(TestContext.Current.CancellationToken);
        var doc = await WebFactory.GetDocumentAsync(client, "/", TestContext.Current.CancellationToken);

        var bar = doc.QuerySelector("header.topbar")!;
        bar.Children.Select(c => c.ClassName).ShouldBe(
            ["topbar__brand", "topbar__spend passive", "topbar__nav", "topbar__actions"]);
        bar.QuerySelector(".topbar__logo").ShouldNotBeNull();
        bar.QuerySelector(".topbar__name")!.TextContent.ShouldBe("ANEW Konsept");
        bar.QuerySelectorAll(".topbar__nav a").Select(a => a.TextContent).ShouldBe(
            ["Konseptler", "Konseptten Video", "Üretilenler", "Silinenler", "Yayınlama"]);
        // Spec 0003 AC-18 (plan 0003 Risk 2, A-1): profil seçimi gerçek bir <select> ("Profil seç"), sıranın ilk öğesi; kalem "Seçili profili düzenle".
        bar.QuerySelector(".topbar__actions")!.FirstElementChild!.QuerySelector("select")!.GetAttribute("aria-label").ShouldBe("Profil seç");
        bar.QuerySelectorAll(".topbar__actions > button").Select(b => b.GetAttribute("aria-label")).ShouldBe(
            ["Seçili profili düzenle", "Yeni profil", "Hata bildir", "Ayarlar", "Açık/koyu temayı değiştir"]);
    }

    /// <summary>Her sayfa bağlantısı kendi sayfasını açar ve boş durum metnini gösterir.</summary>
    /// <param name="path">Yol.</param>
    /// <param name="title">Sayfa başlığı.</param>
    /// <param name="emptyText">Boş durum metni.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Pages_EachLink_RendersEmptyState(string path, string title, string emptyText)
    {
        using var client = await _factory.CreateSignedInClientAsync(TestContext.Current.CancellationToken);
        var home = await WebFactory.GetDocumentAsync(client, "/", TestContext.Current.CancellationToken);
        home.QuerySelectorAll(".topbar__nav a").Select(a => a.GetAttribute("href")).ShouldContain(path);

        var doc = await WebFactory.GetDocumentAsync(client, path, TestContext.Current.CancellationToken);

        doc.QuerySelector("h1.page__title")!.TextContent.ShouldBe(title);
        doc.QuerySelector(".empty-state")!.TextContent.ShouldBe(emptyText);
        doc.QuerySelector($".topbar__nav a[href='{path}']")!.GetAttribute("aria-current").ShouldBe("page");
    }

    /// <summary>Yayınlama "Çok yakında sizlerleyiz" gösterir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Publishing_Page_ShowsComingSoon()
    {
        using var client = await _factory.CreateSignedInClientAsync(TestContext.Current.CancellationToken);

        var doc = await WebFactory.GetDocumentAsync(client, "/yayinlama", TestContext.Current.CancellationToken);

        doc.QuerySelector(".empty-state")!.TextContent.ShouldBe("Çok yakında sizlerleyiz");
    }

    /// <summary>Pasif kontroller "Yakında" ipucu ve erişilebilir ad taşır, klavyeyle odaklanabilir; tema düğmesi etkindir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Layout_PassiveControls_HaveTooltipAndAccessibleName()
    {
        using var client = await _factory.CreateSignedInClientAsync(TestContext.Current.CancellationToken);
        var doc = await WebFactory.GetDocumentAsync(client, "/", TestContext.Current.CancellationToken);

        var passive = doc.QuerySelectorAll(".passive").ToList();
        passive.Count.ShouldBe(2); // Spec 0004 Req 14: "Ayarlar" (plan 0004 A-1) ve spec 0003: profil seçimi, kalem, artı artık pasif değil (plan 0003 A-1); yeni davranış ProfileBarTests'te.
        foreach (var control in passive)
        {
            control.GetAttribute("aria-disabled").ShouldBe("true");
            control.GetAttribute("data-tooltip").ShouldBe("Yakında");
            control.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
            (control.LocalName == "button" || control.GetAttribute("tabindex") == "0").ShouldBeTrue();
            control.HasAttribute("disabled").ShouldBeFalse();
        }

        var theme = doc.GetElementById("theme-toggle")!;
        theme.GetAttribute("aria-disabled").ShouldBeNull();
        theme.GetAttribute("aria-label").ShouldBe("Açık/koyu temayı değiştir");
    }

    /// <summary>Renkler yalnızca tema değişkenlerinden gelir (K-7): renk değeri sadece :root değişken bloklarında.</summary>
    [Fact]
    public void Layout_Css_UsesOnlyThemeVariables()
    {
        var css = File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));
        var withoutComments = CommentPattern().Replace(css, string.Empty);
        var withoutVariableBlocks = VariableBlockPattern().Replace(withoutComments, string.Empty);

        VariableBlockPattern().Matches(withoutComments).Count.ShouldBe(2);
        ColorLiteralPattern().Matches(withoutVariableBlocks).Select(m => m.Value).ShouldBeEmpty();
    }

    /// <summary>Sayfa açılışta açık modla gelir; koyu mod için değişken bloğu vardır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Layout_DefaultTheme_IsLight()
    {
        using var client = await _factory.CreateSignedInClientAsync(TestContext.Current.CancellationToken);
        var doc = await WebFactory.GetDocumentAsync(client, "/", TestContext.Current.CancellationToken);

        doc.DocumentElement.GetAttribute("data-theme").ShouldBe("light");
        doc.QuerySelector("head script[src*='theme.js']").ShouldNotBeNull();
    }

    /// <summary>CSP inline script ve stile izin vermez; sayfada gövdesi olan script yoktur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Layout_SecurityHeaders_ForbidInlineScripts()
    {
        using var client = await _factory.CreateSignedInClientAsync(TestContext.Current.CancellationToken);
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var doc = await WebFactory.GetDocumentAsync(client, "/", TestContext.Current.CancellationToken);

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.ShouldContain("script-src 'self'");
        csp.ShouldContain("style-src 'self'");
        csp.ShouldNotContain("unsafe-inline");
        doc.QuerySelectorAll("script").ShouldAllBe(s => s.HasAttribute("src") && string.IsNullOrWhiteSpace(s.TextContent));
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@":root(\[data-theme=""dark""\])?\s*\{[^}]*\}")]
    private static partial Regex VariableBlockPattern();

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|\b(rgb|rgba|hsl|hsla)\(")]
    private static partial Regex ColorLiteralPattern();
}
