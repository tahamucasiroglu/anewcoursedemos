using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Production.Domain.Entities;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Admin Hata bildirimleri, Api tarafı (spec 0025 AC-1, AC-2, AC-4, AC-5, AC-6, AC-7). Bilinen veri seti bellek deposuna yazılır;
/// gerçek SQL <c>AdminErrorReportsIntegrationTests</c>'te.
/// </summary>
public sealed class AdminErrorReportsApiTests
{
    private const string List = "/api/admin/error-reports";
    private const string Statistics = "/api/admin/statistics";
    private const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ0ZXN0In0.c2lnbmF0dXJlLXRlc3Q";
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// <c>admin.panel</c> yetkisi olmayan kullanıcının token'ıyla liste, detay ve durum değiştirme 403 döner, veri dönmez ve durum değişmez;
    /// kimliksiz istek 401. (Kasıtlı bozma: controller'dan politika kaldırılınca kırmızı — plan Build evidence.)
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminReports_UserToken_403NoDataNoChange_Anonymous401()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var user = await AuthenticatedClient.CreateAsync(factory, ct);
        using var anonymous = factory.CreateClient();
        var number = await AddAsync(factory, ErrorReport.CreateWeb(Guid.NewGuid(), T0, "1", "GIZLIBILDIRIM metni", "/", "corr-x", []), ct);

        foreach (var path in new[] { List, List + "?status=all&source=web", $"{List}/{number}" })
        {
            using var forbidden = await user.GetAsync(new Uri(path, UriKind.Relative), ct);
            using var unauthenticated = await anonymous.GetAsync(new Uri(path, UriKind.Relative), ct);
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden, path);
            (await forbidden.Content.ReadAsStringAsync(ct)).ShouldNotContain("GIZLIBILDIRIM");
            unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }

        using var change = await user.PostAsJsonAsync($"{List}/{number}/status", new { status = "resolved", note = "yetkisiz" }, ct);
        using var anonymousChange = await anonymous.PostAsJsonAsync($"{List}/{number}/status", new { status = "ignored" }, ct);

        change.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        anonymousChange.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var stored = factory.ErrorReports.Reports.Single(r => r.Number == number);
        stored.Status.ShouldBe(ErrorReportStatus.New);
        stored.AdminNote.ShouldBeNull();
    }

    /// <summary>
    /// Varsayılan görünüm açık (Yeni + İnceleniyor) bildirimleri en yeni üstte 25'erli listeler (zaman, kaynak, bildiren, kısa mesaj,
    /// durum); sayfalar tüm açık kayıtlara erişir; kaynak ve durum filtreleri bilinen veri setinde doğru sonuç verir; filtre bayrağı boş
    /// durum metnini ayırır; bildirenin konsept/video içeriği yanıtta yoktur (BR-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task List_KnownDataset_OpenDefault_Paging_Filters()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var empty = await admin.GetFromJsonAsync<JsonElement>(List, ct);
        var ayse = User.Register("Ayşe Yılmaz", "ayse@ornek.test", "hash", T0.AddDays(-30));
        (await factory.Users.TryAddAsync(ayse, ct)).ShouldBeTrue();
        await factory.ProducedVideos.AddAsync(
            new ProducedVideo
            {
                Id = Guid.CreateVersion7(),
                UserId = ayse.Id,
                ProfileId = Guid.NewGuid(),
                ConceptId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                Title = "GIZLIVIDEOBASLIK",
                ConceptText = "{\"t\":\"GIZLIKONSEPT\"}",
                Engine = "remotion",
                FileName = "v.mp4",
                StartedAt = T0,
                EndedAt = T0,
            },
            ct);

        // 20 web (Ayşe) Yeni · 8 ajan (Ayşe) İnceleniyor · 3 API (kullanıcısız) Yeni · 2 web Çözüldü · 1 ajan Yok sayıldı.
        for (var i = 0; i < 20; i++)
        {
            await AddAsync(factory, ErrorReport.CreateWeb(ayse.Id, T0.AddMinutes(i), "1", $"web bildirimi {i:00} " + new string('x', 200), "/", null, []), ct);
        }

        for (var i = 0; i < 8; i++)
        {
            await AddAsync(factory, ErrorReport.CreateAgent(ayse.Id, T0.AddHours(1).AddMinutes(i), "1", $"ajan hatası {i}", "0.5.0", "Windows", null, null, []), ct, ErrorReportStatus.Reviewing);
        }

        for (var i = 0; i < 3; i++)
        {
            await AddAsync(factory, ErrorReport.CreateApi(null, T0.AddHours(2).AddMinutes(i), "1", $"api hatası {i}", "corr-api"), ct);
        }

        await AddAsync(factory, ErrorReport.CreateWeb(ayse.Id, T0.AddHours(3), "1", "çözülen bildirim 1", "/", null, []), ct, ErrorReportStatus.Resolved);
        await AddAsync(factory, ErrorReport.CreateWeb(ayse.Id, T0.AddHours(3).AddMinutes(1), "1", "çözülen bildirim 2", "/", null, []), ct, ErrorReportStatus.Resolved);
        var newest = await AddAsync(factory, ErrorReport.CreateAgent(ayse.Id, T0.AddHours(4), "1", "yok sayılan", "0.5.0", null, null, null, []), ct, ErrorReportStatus.Ignored);

        var page1 = await admin.GetFromJsonAsync<JsonElement>(List, ct);
        var page2 = await admin.GetFromJsonAsync<JsonElement>(List + "?page=2", ct);
        var open = Items(page1).Concat(Items(page2)).ToList();

        empty.GetProperty("total").GetInt32().ShouldBe(0);
        empty.GetProperty("filtered").GetBoolean().ShouldBeFalse();
        page1.GetProperty("total").GetInt32().ShouldBe(31);
        page1.GetProperty("pageSize").GetInt32().ShouldBe(25);
        page1.GetProperty("status").GetString().ShouldBe("open");
        page1.GetProperty("filtered").GetBoolean().ShouldBeFalse();
        Items(page1).Count.ShouldBe(25);
        Items(page2).Count.ShouldBe(6);
        open.Select(r => r.GetProperty("number").GetInt64()).ShouldBe(open.Select(r => r.GetProperty("number").GetInt64()).OrderDescending());
        open.Select(r => r.GetProperty("number").GetInt64()).Distinct().Count().ShouldBe(31);
        open.ShouldAllBe(r => r.GetProperty("status").GetString() == "new" || r.GetProperty("status").GetString() == "reviewing");
        var first = open[0];
        first.GetProperty("source").GetString().ShouldBe("api");
        first.GetProperty("reporterName").GetString().ShouldBe("—");
        first.GetProperty("summary").GetString().ShouldBe("api hatası 2");
        first.GetProperty("createdAt").GetDateTimeOffset().ShouldBe(T0.AddHours(2).AddMinutes(2));
        var web = open.First(r => r.GetProperty("source").GetString() == "web");
        web.GetProperty("reporterName").GetString().ShouldBe("Ayşe Yılmaz");
        web.GetProperty("reporterEmail").GetString().ShouldBe("ayse@ornek.test");
        web.GetProperty("summary").GetString()!.Length.ShouldBe(121); // 120 + "…"

        (await Total(admin, "?source=agent", ct)).ShouldBe(8);
        (await Total(admin, "?source=agent&status=all", ct)).ShouldBe(9);
        (await Total(admin, "?source=api", ct)).ShouldBe(3);
        (await Total(admin, "?source=web&status=new", ct)).ShouldBe(20);
        (await Total(admin, "?status=reviewing", ct)).ShouldBe(8);
        (await Total(admin, "?status=resolved", ct)).ShouldBe(2);
        (await Total(admin, "?status=ignored", ct)).ShouldBe(1);
        (await Total(admin, "?status=all", ct)).ShouldBe(34);
        (await Total(admin, "?status=bilinmeyen&source=xyz", ct)).ShouldBe(31); // bilinmeyen → varsayılan
        var none = await admin.GetFromJsonAsync<JsonElement>(List + "?source=api&status=resolved", ct);
        none.GetProperty("total").GetInt32().ShouldBe(0);
        none.GetProperty("filtered").GetBoolean().ShouldBeTrue();
        var all = await admin.GetFromJsonAsync<JsonElement>(List + "?status=all", ct);
        Items(all)[0].GetProperty("number").GetInt64().ShouldBe(newest);

        var raw = await admin.GetStringAsync(new Uri(List + "?status=all", UriKind.Relative), ct)
            + await admin.GetStringAsync(new Uri($"{List}/{newest}", UriKind.Relative), ct);
        raw.ShouldNotContain("GIZLIVIDEOBASLIK");
        raw.ShouldNotContain("GIZLIKONSEPT");
        raw.ShouldNotContain("hash");
    }

    /// <summary>
    /// Detay açıklama, teknik bilgi (ekler, sayfa, işletim sistemi), korelasyon numarası ve versiyonları gösterir; <c>&lt;script&gt;</c>
    /// metni değiştirilmeden düz metin olarak döner (gösterim istemcide textContent ile); var olmayan bildirim 404.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Detail_ShowsFields_ScriptTextKeptAsText()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var number = await AddAsync(
            factory,
            ErrorReport.CreateAgent(Guid.NewGuid(), T0, "0.5.0", "Render çöktü <script>alert(1)</script>", "0.5.1", "Windows 11", "ffmpeg", "indirme", ["satır 1", "satır 2"]),
            ct);

        var detail = await admin.GetFromJsonAsync<JsonElement>($"{List}/{number}", ct);
        using var missing = await admin.GetAsync(new Uri($"{List}/999999", UriKind.Relative), ct);

        var report = detail.GetProperty("report");
        report.GetProperty("number").GetInt64().ShouldBe(number);
        report.GetProperty("source").GetString().ShouldBe("agent");
        report.GetProperty("status").GetString().ShouldBe("new");
        report.GetProperty("description").GetString().ShouldBe("Render çöktü <script>alert(1)</script>");
        report.GetProperty("agentVersion").GetString().ShouldBe("0.5.1");
        report.GetProperty("productVersion").GetString().ShouldBe("0.5.0");
        report.GetProperty("operatingSystem").GetString().ShouldBe("Windows 11");
        report.GetProperty("component").GetString().ShouldBe("ffmpeg");
        report.GetProperty("step").GetString().ShouldBe("indirme");
        report.GetProperty("attachments").GetString().ShouldBe("satır 1\nsatır 2");
        detail.GetProperty("reporterName").GetString().ShouldBe("—");
        detail.GetProperty("adminNote").ValueKind.ShouldBe(JsonValueKind.Null);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await missing.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Hata bildirimi bulunamadı.");
    }

    /// <summary>
    /// İçinde API key, şifre ve token geçen bildirimin (web yolundan, gerçek maskeleme) listede ve detayda key'i yalnızca maskeli
    /// (<c>sk-…a3f</c>), şifreyi ve token'ı hiç göstermez; admin notundaki sır da maskelenir (H-2; kasıtlı bozma: maske kapalı → kırmızı).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Detail_SecretsMasked_InListAndDetail()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var user = await AuthenticatedClient.CreateAsync(factory, ct);
        using var admin = await factory.CreateAdminClientAsync(ct);
        using var sent = await user.PostAsJsonAsync(
            "/api/error-reports",
            new { description = $"anahtarım sk-proj-DESCKEY1234567a3f password=Hunter2Gizli token={Jwt} bitti", page = "/ayarlar?token=Tok3nGizliDeger", correlationId = "corr-25" },
            ct);
        var number = (await sent.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("number").GetInt64();
        using var noted = await admin.PostAsJsonAsync($"{List}/{number}/status", new { status = "reviewing", note = "kullanıcının key'i sk-proj-NOTEKEY99887766b4c" }, ct);

        var listRaw = await admin.GetStringAsync(new Uri(List, UriKind.Relative), ct);
        var detailRaw = await admin.GetStringAsync(new Uri($"{List}/{number}", UriKind.Relative), ct);
        var detail = JsonDocument.Parse(detailRaw).RootElement;
        var description = detail.GetProperty("report").GetProperty("description").GetString()!;

        sent.StatusCode.ShouldBe(HttpStatusCode.OK);
        noted.StatusCode.ShouldBe(HttpStatusCode.OK);
        foreach (var raw in new[] { listRaw, detailRaw })
        {
            raw.ShouldNotContain("DESCKEY1234567");
            raw.ShouldNotContain("NOTEKEY99887766");
            raw.ShouldNotContain("Hunter2Gizli");
            raw.ShouldNotContain("Tok3nGizliDeger");
            raw.ShouldNotContain(Jwt);
        }

        description.ShouldContain("sk-…a3f");
        description.ShouldContain("anahtarım");
        detail.GetProperty("adminNote").GetString()!.ShouldContain("sk-…b4c");
        detail.GetProperty("report").GetProperty("correlationId").GetString().ShouldBe("corr-25");
    }

    /// <summary>
    /// Durum değişince İstatistikler'deki açık sayı güncellenir (Çözüldü/Yok sayıldı düşer, İnceleniyor açık kalır, geri alınan yeniden
    /// sayılır); not saklanır ve detayda görünür (500 kabul, 501 reddedilir); geçersiz durum ve var olmayan bildirim reddedilir ve durum
    /// değişmez; sunucu hatasında durum değişmez ve hata gövdesinde ayrıntı yoktur; silme ucu yoktur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Status_Change_OpenCountNoteAndRejections()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var a = await AddAsync(factory, ErrorReport.CreateWeb(Guid.NewGuid(), T0, "1", "birinci bildirim", "/", null, []), ct);
        var b = await AddAsync(factory, ErrorReport.CreateWeb(Guid.NewGuid(), T0.AddMinutes(1), "1", "ikinci bildirim", "/", null, []), ct);
        var initial = await Open(admin, ct);

        using var reviewing = await Change(admin, a, new { status = "reviewing", note = new string('n', 500) }, ct);
        var afterReviewing = await Open(admin, ct);
        using var resolved = await Change(admin, a, new { status = "resolved" }, ct);
        using var ignored = await Change(admin, b, new { status = "IGNORED", note = "  tekrar eden  " }, ct);
        var afterClosed = await Open(admin, ct);
        using var reopened = await Change(admin, a, new { status = "new" }, ct);
        var afterReopen = await Open(admin, ct);
        var detailB = await admin.GetFromJsonAsync<JsonElement>($"{List}/{b}", ct);

        initial.ShouldBe(2);
        reviewing.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterReviewing.ShouldBe(2);
        resolved.StatusCode.ShouldBe(HttpStatusCode.OK);
        ignored.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterClosed.ShouldBe(0);
        reopened.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterReopen.ShouldBe(1);
        detailB.GetProperty("report").GetProperty("status").GetString().ShouldBe("ignored");
        detailB.GetProperty("adminNote").GetString().ShouldBe("tekrar eden");
        detailB.GetProperty("statusChangedAt").GetDateTimeOffset().ShouldBe(factory.Time.GetUtcNow());

        using var tooLong = await Change(admin, b, new { status = "resolved", note = new string('n', 501) }, ct);
        using var invalid = await Change(admin, b, new { status = "silindi" }, ct);
        using var missingStatus = await Change(admin, b, new { note = "durum yok" }, ct);
        using var unknown = await Change(admin, 999999, new { status = "resolved" }, ct);
        factory.ErrorReports.FailUpdates = true;
        using var failed = await Change(admin, b, new { status = "resolved" }, ct);
        factory.ErrorReports.FailUpdates = false;
        using var delete = await admin.DeleteAsync(new Uri($"{List}/{b}", UriKind.Relative), ct);
        using var deleteOld = await admin.DeleteAsync(new Uri($"/api/error-reports/{b}", UriKind.Relative), ct);

        tooLong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await tooLong.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Not en fazla 500 karakter olabilir.");
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        missingStatus.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        failed.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await failed.Content.ReadAsStringAsync(ct)).ShouldNotContain("gizli_tablo");
        ((int)delete.StatusCode).ShouldBeOneOf(404, 405);
        ((int)deleteOld.StatusCode).ShouldBeOneOf(404, 405);
        var stored = factory.ErrorReports.Reports.Single(r => r.Number == b);
        stored.Status.ShouldBe(ErrorReportStatus.Ignored);
        stored.AdminNote.ShouldBe("tekrar eden");
        factory.ErrorReports.Reports.Count(r => r.Source != ErrorReportSource.Api).ShouldBe(2);
    }

    private static List<JsonElement> Items(JsonElement page) => [.. page.GetProperty("items").EnumerateArray()];

    private static async Task<int> Total(HttpClient admin, string query, CancellationToken ct) =>
        (await admin.GetFromJsonAsync<JsonElement>(List + query, ct)).GetProperty("total").GetInt32();

    private static async Task<int> Open(HttpClient admin, CancellationToken ct) =>
        (await admin.GetFromJsonAsync<JsonElement>(Statistics, ct)).GetProperty("openErrorReports").GetInt32();

    private static async Task<HttpResponseMessage> Change(HttpClient admin, long number, object body, CancellationToken ct) =>
        await admin.PostAsJsonAsync($"{List}/{number}/status", body, ct);

    private static async Task<long> AddAsync(IdentityApiFactory factory, ErrorReport report, CancellationToken ct, ErrorReportStatus status = ErrorReportStatus.New)
    {
        await factory.ErrorReports.AddAsync(report, ct);
        if (status != ErrorReportStatus.New)
        {
            report.ChangeStatus(status, null, report.CreatedAt);
        }

        return report.Number;
    }
}
