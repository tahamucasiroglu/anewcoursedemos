using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Concrete;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Admin paneli, Web tarafı (spec 0010 AC-1, AC-2, AC-5, AC-8): panel İstatistikler sekmesi seçili açılır, sekme sırası ve aralık
/// seçici; Ajax uçları Api'ye parametreleri iletir; yetkisiz kullanıcı 403, girişsiz kullanıcı giriş sayfası; Api hatasında
/// yalnızca sade mesaj + korelasyon numarası; denetimler klavyeyle erişilebilir, renkler iki temada WCAG AA.
/// </summary>
public sealed partial class AdminPanelTests
{
    /// <summary>
    /// Admin paneli açar: sekmeler İstatistikler · Aktiviteler sırasıyla, İstatistikler seçili ve paneli görünür, Aktiviteler gizli;
    /// aralık seçici 7 / 30 / 90 ve varsayılan 30; Ajax uçları Api'ye aralığı ve filtreleri iletir, yanıtı aynen geçirir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPanel_AdminRole_OpensWithStatisticsTabSelected()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.GetAdminStatisticsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"totalUsers":4}""").RootElement));
        factory.Api.GetAdminActivitiesAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"total":0}""").RootElement));
        using var client = await factory.CreateAdminClientAsync(ct);

        using var response = await client.GetAsync(new Uri("/admin", UriKind.Relative), ct);
        var page = await WebFactory.ParseAsync(await response.Content.ReadAsStringAsync(ct), ct);
        using var stats = await SendAjaxAsync(client, "/admin/istatistikler?gun=90", ct);
        using var activities = await SendAjaxAsync(client, "/admin/aktiviteler?sayfa=2&kullanici=ay%C5%9Fe&baslangic=2026-11-01&bitis=2026-11-10", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.QuerySelectorAll("#admin-panel [role='tab']").Select(t => t.TextContent.Trim()).ShouldBe(["İstatistikler", "Kullanıcılar", "Aktiviteler", "Hata bildirimleri"]); // plan 0024 A-1, plan 0025 A-1.
        page.QuerySelector("#admin-tab-statistics")!.GetAttribute("aria-selected").ShouldBe("true");
        page.QuerySelector("#admin-statistics")!.HasAttribute("hidden").ShouldBeFalse();
        page.QuerySelector("#admin-activities")!.HasAttribute("hidden").ShouldBeTrue();
        page.QuerySelectorAll("#admin-range option").Select(o => o.GetAttribute("value")).ShouldBe(["7", "30", "90"]);
        page.QuerySelector("#admin-range option[selected]")!.GetAttribute("value").ShouldBe("30");
        page.QuerySelectorAll(".admin-card dt").Select(d => d.TextContent).ShouldBe(["Toplam kullanıcı", "Üretilen toplam klip", "Açık hata bildirimi", "Banlı kullanıcı"]); // plan 0024 A-1.
        page.QuerySelector("#admin-panel")!.GetAttribute("data-statistics-url").ShouldBe("/admin/istatistikler");
        page.QuerySelector("#admin-panel")!.GetAttribute("data-activities-url").ShouldBe("/admin/aktiviteler");
        stats.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await stats.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("totalUsers").GetInt32().ShouldBe(4);
        activities.StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).GetAdminStatisticsAsync(90, Arg.Any<CancellationToken>());
        await factory.Api.Received(1).GetAdminActivitiesAsync(2, "ayşe", "2026-11-01", "2026-11-10", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Yetkisi olmayan kullanıcı panel adresinde "/yetkisiz"e gider, iki Ajax ucu 403 JSON "Bu işlem için yetkiniz yok" döner ve
    /// Api'ye istek gitmez; giriş yapmamış kullanıcı panel adresinde giriş sayfasına yönlenir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPanel_UserRole_Forbidden_Anonymous_RedirectsToLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var user = await factory.CreateSignedInClientAsync(ct);
        using var anonymous = SignedInClient.CreateAnonymous(factory);

        using var page = await user.GetAsync(new Uri("/admin", UriKind.Relative), ct);
        using var anonymousPage = await anonymous.GetAsync(new Uri("/admin", UriKind.Relative), ct);
        page.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        page.Headers.Location!.AbsolutePath.ShouldBe("/yetkisiz");
        anonymousPage.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        anonymousPage.Headers.Location!.AbsolutePath.ShouldBe("/giris");
        foreach (var path in new[] { "/admin/istatistikler?gun=30", "/admin/aktiviteler?sayfa=1" })
        {
            using var ajax = await SendAjaxAsync(user, path, ct);
            var body = await ajax.Content.ReadFromJsonAsync<JsonElement>(ct);
            ajax.StatusCode.ShouldBe(HttpStatusCode.Forbidden, path);
            body.GetProperty("message").GetString().ShouldBe("Bu işlem için yetkiniz yok");
        }

        await factory.Api.DidNotReceive().GetAdminStatisticsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().GetAdminActivitiesAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Api 500 döndüğünde (ProblemDetails + korelasyon numarası + ayrıntı) Web yalnızca kod, sade mesaj ve korelasyon numarasını
    /// taşır; SQL/istisna ayrıntısı tarayıcıya gitmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminAjax_ApiError_CorrelationOnly_NoInternals()
    {
        var ct = TestContext.Current.CancellationToken;
        const string Problem = """{"title":"Beklenmeyen bir hata oluştu","status":500,"detail":"Npgsql: SELECT * FROM gizli at Konsept.X","correlationId":"0HN-77"}""";
        var apiClient = new KonseptApiClient(
            new HttpClient(new RecordingHandler(Problem, HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("http://api.test/") },
            new ApiClientOptions(null));
        var failure = await apiClient.GetAdminStatisticsAsync(30, ct);
        await using var factory = new WebFactory();
        factory.Api.GetAdminStatisticsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(failure);
        using var client = await factory.CreateAdminClientAsync(ct);

        using var response = await SendAjaxAsync(client, "/admin/istatistikler?gun=30", ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(500);
        JsonDocument.Parse(text).RootElement.GetProperty("correlationId").GetString().ShouldBe("0HN-77");
        text.ShouldNotContain("SELECT");
        text.ShouldNotContain("Npgsql");
        text.ShouldNotContain("Konsept.X");
    }

    /// <summary>
    /// Klavye ve kontrast: sekmeler, aralık seçici, filtreler ve sayfalama gerçek düğme/form denetimleridir ve etiketlidir;
    /// durum ve hata alanları metinle duyurulur (<c>role=status/alert</c>); kart, grafik, tablo ve boş durum renkleri iki temada
    /// WCAG AA (≥ 4.5:1), grafik çubuğu ≥ 3:1; renkler yalnızca tema değişkenlerinden.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPanel_KeyboardAndContrast()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateAdminClientAsync(ct);
        using var response = await client.GetAsync(new Uri("/admin", UriKind.Relative), ct);
        var page = await WebFactory.ParseAsync(await response.Content.ReadAsStringAsync(ct), ct);

        page.QuerySelectorAll("#admin-panel [role='tab']").ShouldAllBe(t => t.TagName == "BUTTON" && t.GetAttribute("type") == "button");
        foreach (var id in new[] { "admin-range", "admin-filter-user", "admin-filter-from", "admin-filter-to" })
        {
            page.QuerySelector($"label[for='{id}']").ShouldNotBeNull(id);
        }

        page.QuerySelector("#admin-activity-filter button[type='submit']").ShouldNotBeNull();
        page.QuerySelector("#admin-prev")!.TagName.ShouldBe("BUTTON");
        page.QuerySelector("#admin-next")!.TagName.ShouldBe("BUTTON");
        page.QuerySelectorAll("#admin-panel [tabindex]").ShouldAllBe(e => int.Parse(e.GetAttribute("tabindex")!, CultureInfo.InvariantCulture) <= 0);
        page.QuerySelector("#admin-statistics-status")!.GetAttribute("role").ShouldBe("status");
        page.QuerySelector("#admin-activities-status")!.GetAttribute("role").ShouldBe("status");
        page.QuerySelector("#admin-statistics-error")!.GetAttribute("role").ShouldBe("alert");
        page.QuerySelector("#admin-filter-warning")!.GetAttribute("role").ShouldBe("alert");
        page.QuerySelectorAll("#admin-activity-table th[scope='col']").Length.ShouldBe(3);

        var css = File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));
        var themes = VariableBlock().Matches(css).Select(m => Variables(m.Groups["body"].Value)).ToList();
        themes.Count.ShouldBe(2);
        foreach (var selector in new[] { ".admin-card", ".admin-chart", ".admin-chart__empty", ".admin-table" })
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

        var bar = FillVar().Match(Rule(css, ".admin-chart__bar")).Groups["name"].Value;
        foreach (var theme in themes)
        {
            Contrast(theme[bar], theme["--color-surface"]).ShouldBeGreaterThanOrEqualTo(3.0, bar);
        }
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

    [GeneratedRegex(@"fill:\s*var\((?<name>--color-[a-z-]+)\)")]
    private static partial Regex FillVar();
}
