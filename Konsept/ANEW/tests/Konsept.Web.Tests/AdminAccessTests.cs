using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Concrete;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Admin girişi ve yetkisiz durumlar, Web tarafı (spec 0011 AC-1, AC-2, AC-3, AC-5): üst bar ikonu yalnızca <c>admin.panel</c>
/// yetkisiyle, "Ayarlar"ın hemen yanında; yetkisiz sayfa "Bu sayfaya erişim yetkiniz yok" (403), yetkisiz Ajax "Bu işlem için
/// yetkiniz yok" (403); yetkiler her istekte Api'nin oturum yanıtından kurulur (Api erişilemezse yetki yok); kayıt formuna
/// eklenen rol alanı Api'ye iletilmez.
/// </summary>
public sealed partial class AdminAccessTests
{
    private const string PageForbidden = "Bu sayfaya erişim yetkiniz yok";

    private const string ActionForbidden = "Bu işlem için yetkiniz yok";

    /// <summary>Kullanıcı rolündeki biri (yetkisiz oturum yanıtı) üst barda "Admin" ikonunu görmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TopBar_UserRole_NoAdminIcon()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);

        page.QuerySelector("a[href='/admin']").ShouldBeNull();
        page.QuerySelector("[aria-label='Admin']").ShouldBeNull();
    }

    /// <summary>
    /// Admin rolündeki kullanıcı "Admin" ikonunu "Ayarlar"ın hemen yanında görür; ikon gerçek bir bağlantıdır (klavyeyle odaklanır,
    /// Enter ile açılır), erişilebilir adı "Admin"dir ve pasif değildir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TopBar_AdminRole_AdminIconAfterSettings_KeyboardReachable()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateAdminClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var settings = page.QuerySelector(".topbar__actions [aria-label='Ayarlar']")!;
        var admin = settings.NextElementSibling!;

        admin.LocalName.ShouldBe("a");
        admin.GetAttribute("href").ShouldBe("/admin");
        admin.GetAttribute("aria-label").ShouldBe("Admin");
        admin.ClassList.ShouldContain("icon-button");
        admin.HasAttribute("tabindex").ShouldBeFalse();
        admin.HasAttribute("aria-disabled").ShouldBeFalse();
        admin.ClassList.ShouldNotContain("passive");
        admin.QuerySelector("svg")!.GetAttribute("aria-hidden").ShouldBe("true");
        page.QuerySelectorAll("a[href='/admin']").Length.ShouldBe(1);
    }

    /// <summary>
    /// Admin sayfası açılır (0010'dan beri boş durum yerine sekmeli panel — plan 0010 A-1); 0011'in özet ucu Ajax ile Api'den alınmaya
    /// devam eder.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPage_AdminRole_ShowsPanel()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.GetAdminOverviewAsync(Arg.Any<CancellationToken>()).Returns(Result<AdminOverviewResponse>.Success(new AdminOverviewResponse([])));
        using var client = await factory.CreateAdminClientAsync(ct);

        using var response = await client.GetAsync(new Uri("/admin", UriKind.Relative), ct);
        var page = await WebFactory.ParseAsync(await response.Content.ReadAsStringAsync(ct), ct);
        using var overview = await SendAjaxAsync(client, "/admin/ozet", ct);
        var body = await overview.Content.ReadFromJsonAsync<JsonElement>(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.QuerySelector("h1.page__title")!.TextContent.ShouldBe("Admin");
        page.QuerySelector("#admin-panel [role='tablist']").ShouldNotBeNull(); // plan 0010 A-1: boş durum yerine panel.
        page.QuerySelector("#admin-tab-statistics")!.GetAttribute("aria-selected").ShouldBe("true");
        page.QuerySelector("a[href='/admin']")!.GetAttribute("aria-current").ShouldBe("page");
        overview.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("sections").EnumerateArray().ShouldBeEmpty();
    }

    /// <summary>
    /// Kullanıcı rolündeki biri admin adresini elle yazınca "Bu sayfaya erişim yetkiniz yok" görür (403, ekran okuyucuya
    /// duyurulur); Api'ye admin isteği gitmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPage_UserRole_ShowsPageForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        using var admin = await client.GetAsync(new Uri("/admin", UriKind.Relative), ct);
        admin.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        admin.Headers.Location!.AbsolutePath.ShouldBe("/yetkisiz");
        using var forbidden = await client.GetAsync(admin.Headers.Location, ct);
        var page = await WebFactory.ParseAsync(await forbidden.Content.ReadAsStringAsync(ct), ct);

        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var alert = page.QuerySelector("main [role='alert']")!;
        alert.TextContent.Trim().ShouldBe(PageForbidden);
        alert.ClassList.ShouldContain("forbidden");
        page.QuerySelector("#admin").ShouldBeNull();
        await factory.Api.DidNotReceive().GetAdminOverviewAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Kullanıcı rolündeki birinin admin Ajax isteği yönlendirilmez: 403 JSON ve "Bu işlem için yetkiniz yok"; hiçbir admin
    /// verisi dönmez, Api'ye gidilmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminAjax_UserRole_Returns403ActionForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        using var response = await SendAjaxAsync(client, "/admin/ozet", ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        body.GetProperty("code").GetString().ShouldBe("auth.forbidden");
        body.GetProperty("message").GetString().ShouldBe(ActionForbidden);
        body.TryGetProperty("sections", out _).ShouldBeFalse();
        await factory.Api.DidNotReceive().GetAdminOverviewAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Web'de yetki görünse bile karar Api'nindir: Api 403 dönerse (gövdesiz) istemci bunu Forbidden'a, Web de 403 JSON + "Bu
    /// işlem için yetkiniz yok"a çevirir; Api'nin metni taşınmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminAjax_ApiForbids_MapsToActionForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var apiClient = new KonseptApiClient(
            new HttpClient(new RecordingHandler(string.Empty, HttpStatusCode.Forbidden)) { BaseAddress = new Uri("http://api.test/") },
            new ApiClientOptions(null));
        var fromApi = await apiClient.GetAdminOverviewAsync(ct);
        await using var factory = new WebFactory();
        factory.Api.GetAdminOverviewAsync(Arg.Any<CancellationToken>()).Returns(Result<AdminOverviewResponse>.Failure(fromApi.Error!));
        using var client = await factory.CreateAdminClientAsync(ct);

        using var response = await SendAjaxAsync(client, "/admin/ozet", ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        fromApi.Error!.Type.ShouldBe(Konsept.Shared.Kernel.Enums.ErrorType.Forbidden);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        body.GetProperty("code").GetString().ShouldBe("auth.forbidden");
        body.GetProperty("message").GetString().ShouldBe(ActionForbidden);
        body.TryGetProperty("sections", out _).ShouldBeFalse();
    }

    /// <summary>
    /// Yetkiler her istekte Api'nin oturum yanıtından kurulur (kapalı başarısızlık): rol geri alınınca ikon bir sonraki istekte
    /// kaybolur ve admin sayfası yetkisiz olur; Api erişilemezken de admin ikonu ve sayfası gizlidir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Permissions_RebuiltEachRequest_ApiDown_FailsClosed()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateAdminClientAsync(ct);

        var asAdmin = await WebFactory.GetDocumentAsync(client, "/", ct);
        factory.UseSessionPermissions();
        var demoted = await WebFactory.GetDocumentAsync(client, "/", ct);
        factory.UseSessionPermissions("admin.panel");
        factory.Api.GetSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<CurrentUserResponse>.Failure(ApiClientErrors.Unavailable));
        var apiDown = await WebFactory.GetDocumentAsync(client, "/", ct);
        using var adminWhileDown = await client.GetAsync(new Uri("/admin", UriKind.Relative), ct);

        asAdmin.QuerySelector("a[href='/admin']").ShouldNotBeNull();
        demoted.QuerySelector("a[href='/admin']").ShouldBeNull();
        apiDown.QuerySelector("a[href='/admin']").ShouldBeNull();
        adminWhileDown.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        adminWhileDown.Headers.Location!.AbsolutePath.ShouldBe("/yetkisiz");
    }

    /// <summary>
    /// Yetkisiz ve admin metinleri her iki temada WCAG AA kontrastlıdır (≥ 4.5:1) ve ekran okuyucuya duyurulur
    /// (<c>role="alert"</c>); renkler yalnızca tema değişkenlerinden gelir.
    /// </summary>
    [Fact]
    public void AdminAndForbidden_ContrastAA_BothThemes_Announced()
    {
        var css = File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));
        var themes = VariableBlock().Matches(css).Select(m => Variables(m.Groups["body"].Value)).ToList();
        themes.Count.ShouldBe(2);

        foreach (var selector in new[] { ".forbidden", ".admin__message", ".empty-state" })
        {
            var rule = Rule(css, selector);
            var color = ColorVar().Match(rule).Groups["name"].Value;
            var background = BackgroundVar().Match(rule).Groups["name"].Value;
            color.ShouldStartWith("--color-", customMessage: selector);
            background.ShouldStartWith("--color-", customMessage: selector);
            foreach (var theme in themes)
            {
                Contrast(theme[color], theme[background]).ShouldBeGreaterThanOrEqualTo(4.5, $"{selector}: {color} / {background}");
            }
        }

        var admin = File.ReadAllText(Path.Combine(WebPaths.WebProject, "Views", "Admin", "Index.cshtml"));
        var forbidden = File.ReadAllText(Path.Combine(WebPaths.WebProject, "Views", "Pages", "Yetkisiz.cshtml"));
        admin.ShouldContain("id=\"admin-message\" role=\"alert\"");
        forbidden.ShouldContain("role=\"alert\"");
    }

    /// <summary>
    /// Kayıt formuna rol/yetki alanı eklemek Api'ye hiçbir şey taşımaz: Web'in kayıt isteği yalnızca ad, e-posta ve şifre içerir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RegisterForm_ExtraRoleField_NotForwarded()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);
        var fields = new Dictionary<string, string>
        {
            ["Name"] = "Sinsi",
            ["Email"] = "sinsi@ornek.test",
            ["Password"] = "Sifre1234",
            ["Role"] = "admin",
            ["role"] = "admin",
            ["Permissions"] = "admin.panel",
            ["isAdmin"] = "true",
        };

        using var response = await SignedInClient.PostFormAsync(client, "/kayit", fields, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.Api.Received(1).RegisterAsync(
            Arg.Is<RegisterApiRequest>(r => r == new RegisterApiRequest("Sinsi", "sinsi@ornek.test", "Sifre1234")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
        typeof(RegisterApiRequest).GetProperties().Select(p => p.Name).Order(StringComparer.Ordinal).ShouldBe(["Email", "Name", "Password"]);
        var json = JsonSerializer.Serialize(new RegisterApiRequest("A", "a@ornek.test", "Sifre1234"));
        json.ShouldNotContain("role", Case.Insensitive);
        json.ShouldNotContain("perm", Case.Insensitive);
    }

    private static Task<HttpResponseMessage> SendAjaxAsync(HttpClient client, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json");
        return client.SendAsync(request, ct);
    }

    private static string Rule(string css, string selector)
    {
        var match = Regex.Match(css, @"\n" + Regex.Escape(selector) + @" \{(?<body>[^}]*)\}", RegexOptions.None, TimeSpan.FromSeconds(1));
        match.Success.ShouldBeTrue(selector);
        return match.Groups["body"].Value;
    }

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

    [GeneratedRegex(@"(?<![-\w])color:\s*var\((?<name>--color-[a-z-]+)\)")]
    private static partial Regex ColorVar();

    [GeneratedRegex(@"background:\s*var\((?<name>--color-[a-z-]+)\)")]
    private static partial Regex BackgroundVar();
}
