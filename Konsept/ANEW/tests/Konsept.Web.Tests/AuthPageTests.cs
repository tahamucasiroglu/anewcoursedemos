using System.Net;
using AngleSharp.Dom;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Giriş ve kayıt sayfaları (AngleSharp; Api sahte): kayıt/giriş akışı ve kullanıcı menüsü, alan hataları, kalan hak ve kilit
/// mesajları, güvenli dönüş adresi, erişilebilirlik, sunucuya ulaşılamıyor, Web logunda sır yok (spec 0002 AC-1…AC-5, AC-9,
/// AC-10, AC-13, AC-17, AC-18, AC-19).
/// </summary>
public sealed class AuthPageTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    /// <summary>Gets dönüş adresi tablosu: verilen → gidilen (yalnızca site içi adres kabul edilir).</summary>
    public static TheoryData<string, string> ReturnUrls => new()
    {
        { "/uretilenler", "/uretilenler" },
        { "//evil.com", "/" },
        { "http://evil.com", "/" },
        { "/\\evil.com", "/" },
    };

    /// <summary>
    /// Geçerli kayıt kullanıcıyı giriş yapmış hâlde Konseptler'e götürür; üst barın en sağındaki kullanıcı menüsünde adı ve
    /// "Çıkış yap" görünür.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_Success_SignsInRedirectsToConcepts_UserMenuShowsName()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        using var response = await SignedInClient.PostFormAsync(client, "/kayit", Fields(("Name", "Ayşe"), ("Email", "ayse@x.com"), ("Password", "Sifre1234")), ct);
        var home = await WebFactory.GetDocumentAsync(client, "/", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/");
        await factory.Api.Received(1).RegisterAsync(
            Arg.Is<RegisterApiRequest>(r => r.Name == "Ayşe" && r.Email == "ayse@x.com" && r.Password == "Sifre1234"), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        home.QuerySelector("h1.page__title")!.TextContent.ShouldBe("Konseptler");
        var actions = home.QuerySelector(".topbar__actions")!.Children;
        actions[^1].ClassName.ShouldBe("user-menu");
        actions[^1].QuerySelector(".user-menu__name")!.TextContent.ShouldBe(WebFactory.UserName);
        actions[^1].QuerySelector("button[type=submit]")!.TextContent.ShouldBe("Çıkış yap");
    }

    /// <summary>Kayıtlı e-posta: "Bu e-posta zaten kayıtlı" e-posta alanında gösterilir, oturum açılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_DuplicateEmail_ShowsAlreadyRegistered()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.RegisterAsync(Arg.Any<RegisterApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.EmailTakenCode, "Bu e-posta zaten kayıtlı.", ErrorType.Conflict)));
        using var client = SignedInClient.CreateAnonymous(factory);

        var page = await PostAndParseAsync(client, "/kayit", Fields(("Name", "İrem"), ("Email", "IREM@X.COM"), ("Password", "Sifre1234")), ct);

        page.GetElementById("Email-error")!.TextContent.ShouldBe("Bu e-posta zaten kayıtlı");
        page.GetElementById("Email")!.GetAttribute("value").ShouldBe("IREM@X.COM");
        (await client.GetAsync(new Uri("/", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    /// <summary>
    /// Geçersiz kayıt: her hatalı alan kendi Türkçe mesajını gösterir; ad ve e-posta formda kalır, şifre boşaltılır; formda tek
    /// şifre alanı vardır; odak ilk hatalı alanda.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_Invalid_FieldMessages_KeepsNameEmail_ClearsPassword_SinglePasswordField()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.RegisterAsync(Arg.Any<RegisterApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.ValidationCode, "Girilen bilgiler geçersiz.", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]>
                {
                    ["Name"] = ["Ad en fazla 100 karakter olabilir."],
                    ["Password"] = ["Şifre en az bir harf ve en az bir rakam içermeli."],
                },
            }));
        using var client = SignedInClient.CreateAnonymous(factory);
        var longName = new string('a', 101);

        var page = await PostAndParseAsync(client, "/kayit", Fields(("Name", longName), ("Email", "ayse@x.com"), ("Password", "12345678")), ct);

        page.GetElementById("Name-error")!.TextContent.ShouldBe("Ad en fazla 100 karakter olabilir.");
        page.GetElementById("Password-error")!.TextContent.ShouldBe("Şifre en az bir harf ve en az bir rakam içermeli.");
        page.GetElementById("Email-error")!.TextContent.ShouldBeEmpty();
        page.GetElementById("Name")!.GetAttribute("value").ShouldBe(longName);
        page.GetElementById("Email")!.GetAttribute("value").ShouldBe("ayse@x.com");
        page.GetElementById("Password")!.HasAttribute("value").ShouldBeFalse();
        page.QuerySelectorAll("input[type=password]").Length.ShouldBe(1);
        page.GetElementById("Name")!.HasAttribute("autofocus").ShouldBeTrue();
        page.GetElementById("Name")!.GetAttribute("aria-invalid").ShouldBe("true");
        page.GetElementById("form-message")!.TextContent.ShouldBe("Lütfen işaretli alanları düzeltin.");
    }

    /// <summary>Başarılı giriş site içi dönüş adresine gider; site dışı adres yok sayılır ve Konseptler açılır.</summary>
    /// <param name="returnUrl">Verilen dönüş adresi.</param>
    /// <param name="expected">Beklenen yönlendirme.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [MemberData(nameof(ReturnUrls))]
    public async Task Login_Success_ReturnsToLocalUrl_IgnoresExternal(string returnUrl, string expected)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        using var response = await SignedInClient.PostFormAsync(
            client, "/giris", Fields(("Email", "a@x.com"), ("Password", "Sifre1234")), ct, "?returnUrl=" + Uri.EscapeDataString(returnUrl));

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe(expected);
    }

    /// <summary>Yanlış şifrede kalan hak; kilitte açılma saati Europe/Istanbul "HH:mm" biçiminde; odak şifre alanında.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_WrongPassword_ShowsRemaining_LockedShowsIstanbulTime()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(
            Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.WrongPasswordCode, "x", ErrorType.Unauthorized)
            {
                Metadata = new Dictionary<string, object> { [ApiClientErrors.RemainingAttemptsKey] = 2 },
            }),
            Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.WrongPasswordCode, "x", ErrorType.Unauthorized)
            {
                Metadata = new Dictionary<string, object> { [ApiClientErrors.RemainingAttemptsKey] = 1 },
            }),
            Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.LockedCode, "x", ErrorType.Unauthorized)
            {
                Metadata = new Dictionary<string, object> { [ApiClientErrors.LockedUntilKey] = new DateTimeOffset(2026, 11, 14, 9, 15, 0, TimeSpan.Zero) },
            }));

        var first = await PostAndParseAsync(client, "/giris", Fields(("Email", "a@x.com"), ("Password", "Yanlis123")), ct);
        var second = await PostAndParseAsync(client, "/giris", Fields(("Email", "a@x.com"), ("Password", "Yanlis123")), ct);
        var locked = await PostAndParseAsync(client, "/giris", Fields(("Email", "a@x.com"), ("Password", "Yanlis123")), ct);

        first.GetElementById("form-message")!.TextContent.ShouldBe("Yanlış girdiniz, 2 hakkınız kaldı");
        second.GetElementById("form-message")!.TextContent.ShouldBe("Yanlış girdiniz, 1 hakkınız kaldı");
        locked.GetElementById("form-message")!.TextContent.ShouldBe("Çok fazla yanlış deneme. Hesabınız 12:15'de açılacak");
        locked.GetElementById("Password")!.HasAttribute("autofocus").ShouldBeTrue();
        first.GetElementById("Password")!.GetAttribute("aria-describedby")!.Split(' ').ShouldContain("form-message");
        locked.GetElementById("Password")!.GetAttribute("aria-describedby")!.Split(' ').ShouldContain("form-message");
        locked.GetElementById("Email")!.GetAttribute("aria-describedby")!.Split(' ').ShouldNotContain("form-message");
        locked.GetElementById("Email")!.GetAttribute("value").ShouldBe("a@x.com");
    }

    /// <summary>Kilit açılış saati Europe/Istanbul'da gösterilir ve saniyesi varsa bir sonraki dakikaya yuvarlanır (erken gösterilmez).</summary>
    /// <param name="utc">Açılış zamanı (UTC, ISO).</param>
    /// <param name="expected">Gösterilen saat.</param>
    [Theory]
    [InlineData("2026-11-14T09:15:00Z", "12:15")]
    [InlineData("2026-11-14T09:15:00.0010000Z", "12:16")]
    [InlineData("2026-11-14T09:15:30Z", "12:16")]
    [InlineData("2026-11-14T20:59:59Z", "00:00")]
    public void UnlockTime_IstanbulRoundedUpToMinute(string utc, string expected)
    {
        var lockedUntil = DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture);

        Konsept.Web.Auth.Base.AuthPageServiceBase.FormatUnlockTime(lockedUntil).ShouldBe(expected);
    }

    /// <summary>Kayıtlı olmayan e-posta: yalnızca "E-posta ya da şifre hatalı" (kalan hak yok).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_UnknownEmail_ShowsGenericMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.InvalidCredentialsCode, "x", ErrorType.Unauthorized)));
        using var client = SignedInClient.CreateAnonymous(factory);

        var page = await PostAndParseAsync(client, "/giris", Fields(("Email", "yok@x.com"), ("Password", "Sifre1234")), ct);

        page.GetElementById("form-message")!.TextContent.ShouldBe("E-posta ya da şifre hatalı");
        page.Body!.TextContent.ShouldNotContain("hakkınız");
    }

    /// <summary>IP hız sınırında "Çok fazla deneme, biraz sonra tekrar deneyin" görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_TooManyAttempts_ShowsMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.TooManyAttemptsCode, "x", ErrorType.TooManyRequests)));
        using var client = SignedInClient.CreateAnonymous(factory);

        var page = await PostAndParseAsync(client, "/giris", Fields(("Email", "a@x.com"), ("Password", "Sifre1234")), ct);

        page.GetElementById("form-message")!.TextContent.ShouldBe("Çok fazla deneme, biraz sonra tekrar deneyin");
    }

    /// <summary>Oturum düştüğü için girişe gelindiğinde "Oturumunuz sona erdi" bilgisi görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_SessionEnded_ShowsNotice()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        var page = await WebFactory.GetDocumentAsync(client, "/giris?oturum=bitti&returnUrl=%2Fsilinenler", ct);

        page.QuerySelector(".auth__notice")!.TextContent.ShouldBe("Oturumunuz sona erdi");
        page.QuerySelector("input[name=ReturnUrl]")!.GetAttribute("value").ShouldBe("/silinenler");
    }

    /// <summary>
    /// Formlar klavyeyle kullanılabilir: her alanın görünür etiketi var, sekme sırası alan sırası (pozitif tabindex yok), hata
    /// alanla ilişkili ve canlı bölgede duyurulur, odak ilk hatalı alana gider, şifre maskeli.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AuthForms_LabelsAriaLiveFocusFirstInvalid_TabOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.ValidationCode, "x", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["Email"] = ["E-posta adresinizi girin."], ["Password"] = ["Şifrenizi girin."] },
            }));
        using var client = SignedInClient.CreateAnonymous(factory);

        foreach (var (path, fields) in new[] { ("/giris", new[] { "Email", "Password" }), ("/kayit", new[] { "Name", "Email", "Password" }) })
        {
            var page = await WebFactory.GetDocumentAsync(client, path, ct);
            var form = page.QuerySelector("form[data-auth-form]")!;
            form.QuerySelectorAll("input:not([type=hidden]), button").Select(e => string.IsNullOrEmpty(e.Id) ? e.LocalName : e.Id).ShouldBe([.. fields, "button"]);
            foreach (var field in fields)
            {
                page.QuerySelector($"label[for='{field}']")!.TextContent.ShouldNotBeNullOrWhiteSpace();
                page.GetElementById(field)!.GetAttribute("aria-describedby")!.ShouldContain($"{field}-error");
            }

            page.QuerySelectorAll("[tabindex]").ShouldAllBe(e => int.Parse(e.GetAttribute("tabindex")!, System.Globalization.CultureInfo.InvariantCulture) <= 0);
            page.GetElementById("Password")!.GetAttribute("type").ShouldBe("password");
            var live = page.GetElementById("form-message")!;
            live.GetAttribute("role").ShouldBe("alert");
            live.GetAttribute("aria-live").ShouldBe("assertive");
        }

        var invalid = await PostAndParseAsync(client, "/giris", Fields(("Email", string.Empty), ("Password", string.Empty)), ct);

        invalid.GetElementById("Email")!.HasAttribute("autofocus").ShouldBeTrue();
        invalid.GetElementById("Password")!.HasAttribute("autofocus").ShouldBeFalse();
        invalid.GetElementById("Email")!.GetAttribute("aria-invalid").ShouldBe("true");
        invalid.GetElementById("Email-error")!.TextContent.ShouldBe("E-posta adresinizi girin.");
        invalid.GetElementById("Email")!.GetAttribute("aria-describedby")!.Split(' ').ShouldBe(["Email-error", "form-message"]);
        invalid.GetElementById("form-message")!.TextContent.ShouldBe("Lütfen işaretli alanları düzeltin.");
    }

    /// <summary>
    /// Giriş ve kayıt sayfaları gönderim korumasını yapan <c>auth.js</c> modülünü yükler; form gönderilirken gösterilecek metni
    /// (<c>data-text-sending</c>) resx'ten taşır (review R3-4).
    /// </summary>
    /// <param name="path">Sayfa.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("/giris")]
    [InlineData("/kayit")]
    public async Task AuthPages_LoadAuthScript_FormCarriesSendingText(string path)
    {
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        var page = await WebFactory.GetDocumentAsync(client, path, TestContext.Current.CancellationToken);

        page.QuerySelectorAll("script[type=module][src^='/js/auth.js']").Length.ShouldBe(1);
        page.QuerySelector("form[data-auth-form]")!.GetAttribute("data-text-sending").ShouldBe("Gönderiliyor…");
    }

    /// <summary>
    /// Kullanıcı menüsü klavyeyle açılır (<c>details/summary</c>), tema düğmesinden sonradır ve "Çıkış yap" CSRF belirteçli bir
    /// POST formudur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task UserMenu_KeyboardAccessible_LogoutForm()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);

        var actions = page.QuerySelector(".topbar__actions")!.Children;
        actions[^2].Id.ShouldBe("theme-toggle");
        var menu = actions[^1];
        menu.LocalName.ShouldBe("details");
        var summary = menu.QuerySelector("summary")!;
        summary.TextContent.Trim().ShouldBe(WebFactory.UserName);
        var form = menu.QuerySelector("form")!;
        form.GetAttribute("method").ShouldBe("post");
        form.GetAttribute("action").ShouldBe("/cikis");
        form.QuerySelector("input[name='__RequestVerificationToken']")!.GetAttribute("value").ShouldNotBeNullOrWhiteSpace();
        form.QuerySelector("button[type=submit]")!.TextContent.ShouldBe("Çıkış yap");
    }

    /// <summary>
    /// Sunucuya ulaşılamazsa form "Şu an işleminizi yapamıyoruz (hata no: …)" gösterir, iç ayrıntı göstermez, girilen ad/e-posta
    /// kalır; korelasyon numarası Web logunda bulunur.
    /// </summary>
    /// <param name="path">Form (<c>/giris</c> ya da <c>/kayit</c>).</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("/giris")]
    [InlineData("/kayit")]
    public async Task AuthForms_ApiUnreachable_GenericErrorWithCorrelation_KeepsInput(string path)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(ApiClientErrors.Unavailable));
        factory.Api.RegisterAsync(Arg.Any<RegisterApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(ApiClientErrors.Unavailable));
        using var client = SignedInClient.CreateAnonymous(factory);

        var page = await PostAndParseAsync(client, path, Fields(("Name", "Ayşe"), ("Email", "ayse@x.com"), ("Password", "Sifre1234")), ct);

        var message = page.GetElementById("form-message")!.TextContent;
        message.ShouldStartWith("Şu an işleminizi yapamıyoruz (hata no: ");
        var correlation = message["Şu an işleminizi yapamıyoruz (hata no: ".Length..^1];
        correlation.ShouldNotBeNullOrWhiteSpace();
        page.Body!.TextContent.ShouldNotContain("Exception");
        page.Body.TextContent.ShouldNotContain("   at ");
        page.GetElementById("Email")!.GetAttribute("value").ShouldBe("ayse@x.com");
        if (path == "/kayit")
        {
            page.GetElementById("Name")!.GetAttribute("value").ShouldBe("Ayşe");

            // Alan hatası yok: odak mesajın kendisine gider, ekran okuyucu okur (review R1-7).
            var live = page.GetElementById("form-message")!;
            live.HasAttribute("autofocus").ShouldBeTrue();
            live.GetAttribute("tabindex").ShouldBe("-1");
            page.QuerySelectorAll("input[autofocus]").ShouldBeEmpty();
        }
        else
        {
            page.GetElementById("Password")!.GetAttribute("aria-describedby")!.Split(' ').ShouldContain("form-message");
        }

        await Eventually(() => factory.ReadLogText().Contains(correlation, StringComparison.Ordinal), ct);
    }

    /// <summary>Başarılı ve başarısız giriş, kayıt ve çıkış sırasında Web logu şifreyi ya da token'ları içermez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task WebAuthFlows_NoPasswordOrTokenInWebLog()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var password = "Gizli" + Guid.NewGuid().ToString("N")[..10] + "5";
        using var client = SignedInClient.CreateAnonymous(factory);
        (await SignedInClient.PostFormAsync(client, "/kayit", Fields(("Name", "Ayşe"), ("Email", "ayse@x.com"), ("Password", password)), ct)).Dispose();
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("form.user-menu__logout input[name='__RequestVerificationToken']")!.GetAttribute("value")!;
        using (var logout = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }))
        {
            (await client.PostAsync(new Uri("/cikis", UriKind.Relative), logout, ct)).Dispose();
        }

        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(ApiClientErrors.Unavailable));
        (await SignedInClient.PostFormAsync(client, "/giris", Fields(("Email", "ayse@x.com"), ("Password", password)), ct)).Dispose();
        var marker = Guid.NewGuid().ToString("N");
#pragma warning disable CA1848 // Test: dosya logunun yazıldığını gösteren işaret.
        factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Web.Tests.Flush").LogWarning("flush {Marker}", marker);
#pragma warning restore CA1848

        await Eventually(() => factory.ReadLogText().Contains(marker, StringComparison.Ordinal), ct);
        var text = factory.ReadLogText();
        text.ShouldNotContain(password);
        text.ShouldNotContain("jwt-register");
        text.ShouldNotContain("knr_register");
        text.ShouldContain("Kimlik işlemi tamamlanamadı");
    }

    private static Dictionary<string, string> Fields(params (string Name, string Value)[] fields) =>
        fields.ToDictionary(f => f.Name, f => f.Value, StringComparer.Ordinal);

    private static async Task<IDocument> PostAndParseAsync(HttpClient client, string path, Dictionary<string, string> fields, CancellationToken ct)
    {
        using var response = await SignedInClient.PostFormAsync(client, path, fields, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await WebFactory.ParseAsync(await response.Content.ReadAsStringAsync(ct), ct);
    }

    private static async Task Eventually(Func<bool> condition, CancellationToken ct)
    {
        var until = DateTime.UtcNow + Limit;
        while (!condition())
        {
            if (DateTime.UtcNow > until)
            {
                throw new TimeoutException($"Koşul {Limit.TotalSeconds} sn içinde sağlanmadı.");
            }

            await Task.Delay(100, ct);
        }
    }
}
