using System.Globalization;
using System.Text.RegularExpressions;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Giriş/kayıt karşılama bölümü (spec 0002 Req 19, AC-20): içerik ve kullanıcı verisi yokluğu, dar ekranda formun üstünde
/// yığılma, iki temada metin kontrastı (CSS değişkenlerinden, WCAG AA ≥ 4.5:1).
/// </summary>
public sealed partial class WelcomeSectionTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="WelcomeSectionTests"/> class.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public WelcomeSectionTests(WebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Karşılama bölümü giriş ve kayıt sayfalarında: ürün adı, açık/koyu logo, tanıtım, karşı sayfaya geçiş bağlantısı ve ajan
    /// notu; kullanıcı verisi (ad, e-posta, menü) içermez.
    /// </summary>
    /// <param name="path">Sayfa.</param>
    /// <param name="switchHref">Geçiş bağlantısının hedefi.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("/giris", "/kayit")]
    [InlineData("/kayit", "/giris")]
    public async Task AuthPages_WelcomeSection_ContentNoUserData(string path, string switchHref)
    {
        using var client = SignedInClient.CreateAnonymous(_factory);

        var page = await WebFactory.GetDocumentAsync(client, path, TestContext.Current.CancellationToken);

        var welcome = page.QuerySelector("section.welcome")!;
        welcome.QuerySelector(".welcome__title")!.TextContent.ShouldBe("ANEW Konsept");
        welcome.QuerySelectorAll("img.welcome__logo").Select(i => i.GetAttribute("src")!.Split('?')[0]).ShouldBe(["/img/app-icon.svg", "/img/app-icon-dark.svg"]);
        welcome.QuerySelectorAll("img.welcome__logo").ShouldAllBe(i => !string.IsNullOrWhiteSpace(i.GetAttribute("alt")));
        var intro = welcome.QuerySelector(".welcome__intro")!.TextContent;
        intro.ShouldContain("konsept");
        intro.ShouldContain("onaylarsın");
        intro.ShouldContain("videoya");
        intro.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length.ShouldBeInRange(2, 3);
        welcome.QuerySelector("a.welcome__link")!.GetAttribute("href").ShouldBe(switchHref);
        welcome.QuerySelector(".welcome__note")!.TextContent.ShouldBe("Masaüstü ajanını giriş yaptıktan sonra Ayarlar'dan indirebilirsin.");
        welcome.TextContent.ShouldNotContain(WebFactory.UserName);
        welcome.TextContent.ShouldNotContain(SignedInClient.Email);
        page.QuerySelector(".user-menu").ShouldBeNull();
        page.QuerySelector("#system-status").ShouldBeNull();
    }

    /// <summary>
    /// Hatalı gönderimden sonra da karşılama bölümü girilen değerleri içermez (review R3-5): yanlış şifreli girişte e-posta,
    /// alan hatalı kayıtta ad ve e-posta yalnızca formun alanlarında durur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AuthPages_AfterFailedPost_WelcomeHasNoEnteredValues()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(
            Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.WrongPasswordCode, "x", ErrorType.Unauthorized)
            {
                Metadata = new Dictionary<string, object> { [ApiClientErrors.RemainingAttemptsKey] = 2 },
            }));
        factory.Api.RegisterAsync(Arg.Any<RegisterApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(
            Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.ValidationCode, "x", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["Password"] = ["Şifre en az bir harf ve en az bir rakam içermeli."] },
            }));
        using var client = SignedInClient.CreateAnonymous(factory);
        const string loginEmail = "karsilama-giris@ornek.com";
        const string name = "Zeynep Karşılamatest";
        const string registerEmail = "karsilama-kayit@ornek.com";

        using var login = await SignedInClient.PostFormAsync(client, "/giris", new Dictionary<string, string> { ["Email"] = loginEmail, ["Password"] = "Yanlis12345" }, ct);
        var loginPage = await WebFactory.ParseAsync(await login.Content.ReadAsStringAsync(ct), ct);
        using var register = await SignedInClient.PostFormAsync(
            client, "/kayit", new Dictionary<string, string> { ["Name"] = name, ["Email"] = registerEmail, ["Password"] = "12345678" }, ct);
        var registerPage = await WebFactory.ParseAsync(await register.Content.ReadAsStringAsync(ct), ct);

        loginPage.GetElementById("Email")!.GetAttribute("value").ShouldBe(loginEmail);
        loginPage.QuerySelector("section.welcome")!.OuterHtml.ShouldNotContain(loginEmail);
        registerPage.GetElementById("Name")!.GetAttribute("value").ShouldBe(name);
        registerPage.GetElementById("Email")!.GetAttribute("value").ShouldBe(registerEmail);
        var welcome = registerPage.QuerySelector("section.welcome")!.OuterHtml;
        welcome.ShouldNotContain(name);
        welcome.ShouldNotContain(registerEmail);
    }

    /// <summary>Geniş ekranda iki sütun (karşılama + form); 640 px altında tek sütun ve karşılama DOM'da formdan önce (üstte).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AuthCss_Narrow_StacksWelcomeAboveForm()
    {
        using var client = SignedInClient.CreateAnonymous(_factory);
        var page = await WebFactory.GetDocumentAsync(client, "/giris", TestContext.Current.CancellationToken);
        var css = ReadCss();

        page.QuerySelector("main.auth")!.Children.Select(c => c.ClassName).ShouldBe(["welcome", "auth__panel"]);
        BaseAuthRule().Match(css).Groups["body"].Value.ShouldContain("grid-template-columns: minmax(0, 1fr) minmax(0, 1fr)");
        var narrow = NarrowMedia().Match(css);
        narrow.Success.ShouldBeTrue();
        narrow.Groups["body"].Value.ShouldContain(".auth {");
        narrow.Groups["body"].Value.ShouldContain("grid-template-columns: 1fr");
    }

    /// <summary>Karşılama ve form metin renkleri yüzeylerine karşı her iki temada WCAG AA (≥ 4.5:1).</summary>
    [Fact]
    public void AuthWelcome_ContrastAA_BothThemes()
    {
        var css = ReadCss();
        var blocks = VariableBlock().Matches(css).Select(m => Variables(m.Groups["body"].Value)).ToList();
        blocks.Count.ShouldBe(2);
        (string Text, string Background)[] pairs =
        [
            ("--color-text", "--color-surface"),
            ("--color-text-muted", "--color-surface"),
            ("--color-accent", "--color-surface"),
            ("--color-error", "--color-surface"),
            ("--color-on-primary", "--color-primary"),
            ("--color-text", "--color-bg"),
            ("--color-text", "--color-surface-alt"),
        ];

        // Metin dışı öğeler (WCAG 1.4.11, review R5-2): alan kenarlığı yüzeye ve alanın zeminine (--color-bg), odak halkası
        // yüzeye karşı ≥ 3:1.
        (string Part, string Background)[] nonText =
        [
            ("--color-input-border", "--color-surface"),
            ("--color-input-border", "--color-bg"),
            ("--color-focus", "--color-surface"),
        ];

        foreach (var theme in blocks)
        {
            foreach (var (text, background) in pairs)
            {
                Contrast(theme[text], theme[background]).ShouldBeGreaterThanOrEqualTo(4.5, $"{text} / {background}");
            }

            foreach (var (part, background) in nonText)
            {
                theme.ShouldContainKey(part);
                Contrast(theme[part], theme[background]).ShouldBeGreaterThanOrEqualTo(3.0, $"{part} / {background}");
            }
        }

        css.ShouldContain("border: 1px solid var(--color-input-border)");
    }

    private static string ReadCss() => File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));

    private static Dictionary<string, string> Variables(string body) =>
        VariableLine().Matches(body).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);

    private static double Contrast(string a, string b)
    {
        var (l1, l2) = (Luminance(a), Luminance(b));
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int index)
        {
            var c = int.Parse(hex.AsSpan(1 + (index * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(0)) + (0.7152 * Channel(1)) + (0.0722 * Channel(2));
    }

    [GeneratedRegex(@":root(\[data-theme=""dark""\])?\s*\{(?<body>[^}]*)\}")]
    private static partial Regex VariableBlock();

    [GeneratedRegex(@"(?<name>--color-[a-z-]+):\s*(?<value>#[0-9a-fA-F]{6})")]
    private static partial Regex VariableLine();

    [GeneratedRegex(@"\n\.auth \{(?<body>[^}]*)\}")]
    private static partial Regex BaseAuthRule();

    [GeneratedRegex(@"@media \(max-width: 639\.98px\) \{(?<body>.*?)\n\}", RegexOptions.Singleline)]
    private static partial Regex NarrowMedia();
}
