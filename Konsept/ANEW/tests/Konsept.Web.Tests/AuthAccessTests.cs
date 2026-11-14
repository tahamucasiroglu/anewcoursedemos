using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Web erişim ve oturum (spec 0002 AC-11, AC-12, AC-13, AC-14 Web tarafı): default-deny, giriş yapmışken giriş/kayıt,
/// kayan 14 günlük oturum, oturum düşünce sayfa ve Ajax davranışı, çıkış.
/// </summary>
public sealed class AuthAccessTests
{
    private static readonly string[] AnonymousAllowList = ["GET giris", "GET kayit", "POST giris", "POST kayit"];

    private static readonly string[] Pages = ["/", "/konseptten-video", "/uretilenler", "/silinenler", "/yayinlama", "/hata"];

    /// <summary>
    /// Kimliksiz açılabilen uçlar yalnızca giriş ve kayıttır; giriş yapmamış biri her sayfadan giriş sayfasına, istediği
    /// sayfa dönüş adresi olarak gönderilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Anonymous_EveryPage_RedirectsToLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        var anonymous = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => $"{e.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods[0]} {e.RoutePattern.RawText!.TrimStart('/')}");
        anonymous.Order(StringComparer.Ordinal).ShouldBe(AnonymousAllowList);

        foreach (var page in Pages)
        {
            using var response = await client.GetAsync(new Uri(page, UriKind.Relative), ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect, page);
            response.Headers.Location!.AbsolutePath.ShouldBe("/giris");
            QueryHelpers.ParseQuery(response.Headers.Location.Query)["returnUrl"].ToString().ShouldBe(page);
        }

        (await client.GetAsync(new Uri("/giris", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync(new Uri("/kayit", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.DidNotReceive().GetSystemStatusAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Giriş yapmamış birinin Ajax isteği 401 JSON alır (yönlendirme değil), hiçbir veri dönmez, Api'ye gidilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Anonymous_AjaxEndpoints_Return401WithoutData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        using var status = await SendAjaxAsync(client, HttpMethod.Get, "/system/status", ct);
        using var agentTest = await SendAjaxAsync(client, HttpMethod.Post, "/system/agent-test", ct);
        var body = await status.Content.ReadFromJsonAsync<JsonElement>(ct);

        status.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        agentTest.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        body.GetProperty("code").GetString().ShouldBe("auth.session_expired");
        body.TryGetProperty("version", out _).ShouldBeFalse();
        body.TryGetProperty("agent", out _).ShouldBeFalse();
        await factory.Api.DidNotReceive().GetSystemStatusAsync(Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().TestAgentAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Giriş yapmış kullanıcı giriş ya da kayıt sayfasını açınca Konseptler'e gönderilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SignedIn_LoginOrRegisterPage_RedirectsToConcepts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        using var login = await client.GetAsync(new Uri("/giris", UriKind.Relative), ct);
        using var register = await client.GetAsync(new Uri("/kayit", UriKind.Relative), ct);

        login.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        login.Headers.Location!.OriginalString.ShouldBe("/");
        register.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        register.Headers.Location!.OriginalString.ShouldBe("/");
    }

    /// <summary>
    /// Kullanılan oturum 14 gün dolmadan geçerli kalır ve süresi yeniden başlar (erişim token'ı tek seferde yenilenir); 14 günden
    /// uzun kullanılmayan oturumla sayfa "Oturumunuz sona erdi" bilgisiyle girişe, Ajax 401'e gider (saat ilerletilerek).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ExpiredSession_PageRedirectsWithMessage_AjaxReturns401()
    {
        var ct = TestContext.Current.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
        await using var factory = new WebFactory(time);
        using var client = await factory.CreateSignedInClientAsync(ct);

        time.Advance(TimeSpan.FromDays(13));
        using var afterThirteen = await client.GetAsync(new Uri("/uretilenler", UriKind.Relative), ct);
        time.Advance(TimeSpan.FromDays(13));
        using var afterTwentySix = await client.GetAsync(new Uri("/uretilenler", UriKind.Relative), ct);
        time.Advance(TimeSpan.FromDays(14).Add(TimeSpan.FromMinutes(1)));
        using var expiredAjax = await SendAjaxAsync(client, HttpMethod.Get, "/system/status", ct);
        using var expiredPage = await client.GetAsync(new Uri("/uretilenler", UriKind.Relative), ct);
        var ajaxBody = await expiredAjax.Content.ReadFromJsonAsync<JsonElement>(ct);

        afterThirteen.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterTwentySix.StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(2).RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        expiredAjax.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ajaxBody.GetProperty("message").GetString().ShouldBe("Oturumunuz sona erdi");
        expiredPage.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AssertLoginRedirectWithSessionEnded(expiredPage, "/uretilenler");
    }

    /// <summary>
    /// Tarayıcı cookie'si biletten uzun yaşar (max-age &gt; 14 gün): 14 gün boşta kalan gerçek tarayıcı cookie'yi hâlâ gönderir,
    /// sunucu süresi dolmuş bileti görür ve giriş sayfası "Oturumunuz sona erdi" bilgisini gösterir (AC-13 sayfa yolu).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task IdleSession_BrowserKeepsCookie_LoginShowsSessionEnded()
    {
        var ct = TestContext.Current.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
        await using var factory = new WebFactory(time);
        using var client = SignedInClient.CreateAnonymous(factory);
        using var login = await SignedInClient.PostFormAsync(
            client, "/giris", new Dictionary<string, string> { ["Email"] = SignedInClient.Email, ["Password"] = SignedInClient.Password }, ct);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("konsept.auth=", StringComparison.Ordinal));
        var maxAge = int.Parse(System.Text.RegularExpressions.Regex.Match(cookie, "max-age=(\\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        time.Advance(TimeSpan.FromDays(14).Add(TimeSpan.FromMinutes(1)));
        using var page = await client.GetAsync(new Uri("/", UriKind.Relative), ct);
        var loginPage = await WebFactory.GetDocumentAsync(client, page.Headers.Location!.PathAndQuery, ct);

        TimeSpan.FromSeconds(maxAge).ShouldBeGreaterThan(TimeSpan.FromDays(14).Add(TimeSpan.FromMinutes(1)));
        AssertLoginRedirectWithSessionEnded(page, "/");
        loginPage.QuerySelector(".auth__notice")!.TextContent.ShouldBe("Oturumunuz sona erdi");
    }

    /// <summary>
    /// Api oturumları iptal edemezse çıkış başarılı gibi gösterilmez: oturum (cookie) kalır, korelasyon numaralı genel hata
    /// gösterilir (Req 15).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_ApiFails_KeepsSessionShowsErrorWithCorrelation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(Konsept.Web.ApiClient.ApiClientErrors.Unavailable));
        using var client = await factory.CreateSignedInClientAsync(ct);
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("form.user-menu__logout input[name='__RequestVerificationToken']")!.GetAttribute("value")!;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        using var logout = await client.PostAsync(new Uri("/cikis", UriKind.Relative), content, ct);
        var errorPage = await WebFactory.ParseAsync(await logout.Content.ReadAsStringAsync(ct), ct);
        using var next = await client.GetAsync(new Uri("/", UriKind.Relative), ct);

        logout.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        errorPage.QuerySelector("[role=alert]")!.TextContent.ShouldStartWith("Şu an işleminizi yapamıyoruz (hata no: ");
        logout.Headers.Contains("Set-Cookie").ShouldBeFalse();
        next.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Çıkış Api'de tüm oturumları kapatır, cookie'yi siler ve giriş sayfasına götürür; sonraki sayfa girişe gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_SignsOutRedirectsToLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("form.user-menu__logout input[name='__RequestVerificationToken']")!.GetAttribute("value")!;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        using var logout = await client.PostAsync(new Uri("/cikis", UriKind.Relative), content, ct);
        using var next = await client.GetAsync(new Uri("/", UriKind.Relative), ct);

        logout.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        logout.Headers.Location!.OriginalString.ShouldBe("/giris");
        await factory.Api.Received(1).LogoutAsync("jwt-login", Arg.Any<CancellationToken>());
        next.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        next.Headers.Location!.AbsolutePath.ShouldBe("/giris");
    }

    /// <summary>
    /// Birden çok tarayıcıda oturum: biri çıkış yapınca (Api oturumları iptal eder) diğerlerinin bir sonraki sayfası "oturum
    /// sona erdi" bilgisiyle girişe gider, Ajax isteği 401 alır (tarayıcıda api.js girişe yönlendirir).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RevokedSession_NextPageAndAjax_RedirectToLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var browserA = await factory.CreateSignedInClientAsync(ct);
        using var browserB = await factory.CreateSignedInClientAsync(ct);
        using var browserC = await factory.CreateSignedInClientAsync(ct);
        factory.Api.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            factory.Api.GetSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Result<CurrentUserResponse>.Failure(new("identity.session_invalid", "Oturumunuz sona erdi.", ErrorType.Unauthorized)));
            return Result<int>.Success(2);
        });
        var page = await WebFactory.GetDocumentAsync(browserA, "/", ct);
        var token = page.QuerySelector("form.user-menu__logout input[name='__RequestVerificationToken']")!.GetAttribute("value")!;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        (await browserA.PostAsync(new Uri("/cikis", UriKind.Relative), content, ct)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var nextPage = await browserB.GetAsync(new Uri("/silinenler", UriKind.Relative), ct);
        using var ajax = await SendAjaxAsync(browserC, HttpMethod.Get, "/system/status", ct);

        ajax.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        nextPage.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AssertLoginRedirectWithSessionEnded(nextPage, "/silinenler");
    }

    /// <summary>
    /// Erişim token'ı dolmuşken refresh geçici olarak başarısız olursa (Api erişilemiyor) oturum düşürülmez: dolmuş token'la
    /// oturum sorgusu yapılmaz, sayfa açılır, Ajax "API erişilemiyor" durumunu alır (oturum bitti değil), cookie silinmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RefreshTransientFailure_ExpiredToken_KeepsSession()
    {
        var ct = TestContext.Current.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
        await using var factory = new WebFactory(time);
        using var client = await factory.CreateSignedInClientAsync(ct);
        factory.Api.RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<AuthSessionResponse>.Failure(Konsept.Web.ApiClient.ApiClientErrors.Unavailable));
        factory.Api.GetSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<CurrentUserResponse>.Failure(new("api.error", "jwt süresi doldu", ErrorType.Unauthorized)));
        factory.Api.GetSystemStatusAsync(Arg.Any<CancellationToken>()).Returns(Result<SystemStatusResponse>.Failure(Konsept.Web.ApiClient.ApiClientErrors.Unavailable));
        time.Advance(TimeSpan.FromMinutes(20));
        factory.Api.ClearReceivedCalls();

        using var page = await client.GetAsync(new Uri("/uretilenler", UriKind.Relative), ct);
        using var ajax = await SendAjaxAsync(client, HttpMethod.Get, "/system/status", ct);
        var body = await ajax.Content.ReadFromJsonAsync<JsonElement>(ct);

        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.Headers.Contains("Set-Cookie").ShouldBeFalse();
        ajax.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("apiAvailable").GetBoolean().ShouldBeFalse();
        await factory.Api.DidNotReceive().GetSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Anonim çizilmiş giriş/kayıt formu, başka sekmede giriş yapıldıktan sonra gönderilince ham 400 (CSRF) yerine Konseptler'e
    /// yönlendirilir; Api'ye ikinci giriş/kayıt gitmez.
    /// </summary>
    /// <param name="path">Form.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("/giris")]
    [InlineData("/kayit")]
    public async Task StaleAnonymousForm_AfterSignInElsewhere_RedirectsToConcepts(string path)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);
        var stale = await WebFactory.GetDocumentAsync(client, path, ct);
        var token = stale.QuerySelector("form[data-auth-form] input[name='__RequestVerificationToken']")!.GetAttribute("value")!;
        (await SignedInClient.PostFormAsync(client, "/giris", new Dictionary<string, string> { ["Email"] = SignedInClient.Email, ["Password"] = SignedInClient.Password }, ct)).Dispose();
        factory.Api.ClearReceivedCalls();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Ayşe",
            ["Email"] = "ayse@x.com",
            ["Password"] = "Sifre1234",
        });
        using var response = await client.PostAsync(new Uri(path, UriKind.Relative), content, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/");
        await factory.Api.DidNotReceive().LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().RegisterAsync(Arg.Any<RegisterApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Düşmüş oturumla gönderilen POST (ör. <c>/cikis</c>) girişe dönüş adresi olmadan gider; yalnızca GET dönüş adresi olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RevokedSession_PostRequest_RedirectsWithoutReturnUrl()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("form.user-menu__logout input[name='__RequestVerificationToken']")!.GetAttribute("value")!;
        factory.Api.GetSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<CurrentUserResponse>.Failure(new("identity.session_invalid", "Oturumunuz sona erdi.", ErrorType.Unauthorized)));

        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        using var logout = await client.PostAsync(new Uri("/cikis", UriKind.Relative), content, ct);

        logout.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        logout.Headers.Location!.OriginalString.ShouldStartWith("/giris");
        var query = QueryHelpers.ParseQuery(new Uri(new Uri("http://localhost"), logout.Headers.Location).Query);
        query.ContainsKey("returnUrl").ShouldBeFalse();
        query["oturum"].ToString().ShouldBe("bitti");
    }

    private static void AssertLoginRedirectWithSessionEnded(HttpResponseMessage response, string returnUrl)
    {
        response.Headers.Location!.AbsolutePath.ShouldBe("/giris");
        var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
        query["returnUrl"].ToString().ShouldBe(returnUrl);
        query["oturum"].ToString().ShouldBe("bitti");
    }

    private static Task<HttpResponseMessage> SendAjaxAsync(HttpClient client, HttpMethod method, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json");
        return client.SendAsync(request, ct);
    }
}
