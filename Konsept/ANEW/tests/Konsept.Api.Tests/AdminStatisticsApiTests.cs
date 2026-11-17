using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Monitoring.Application.Concrete;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Production.Domain.Entities;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Admin İstatistikler ve Aktiviteler, Api tarafı (spec 0010 AC-2, AC-3, AC-4, AC-5, AC-6, AC-7). Bilinen veri seti bellek
/// depolarına yazılır; saat 2026-11-14 09:00 UTC (Istanbul 12:00) sabittir. Gerçek sorgular <c>AdminStatisticsIntegrationTests</c>'te.
/// </summary>
public sealed class AdminStatisticsApiTests
{
    private const string Statistics = "/api/admin/statistics";
    private const string Activities = "/api/admin/activities";
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);
    private static readonly DateOnly Today = new(2026, 11, 14);

    /// <summary>
    /// <c>admin.panel</c> yetkisi olmayan kullanıcının token'ıyla her iki uç 403 döner ve hiçbir veri dönmez; kimliksiz istek 401.
    /// (Kasıtlı bozma: controller'dan politika kaldırılınca kırmızı — plan Build evidence.)
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminData_UserToken_403NoData_Anonymous401()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var user = await AuthenticatedClient.CreateAsync(factory, ct);
        using var anonymous = factory.CreateClient();

        foreach (var path in new[] { Statistics, Statistics + "?days=90", Activities, Activities + "?page=1&user=ad" })
        {
            using var forbidden = await user.GetAsync(new Uri(path, UriKind.Relative), ct);
            using var unauthenticated = await anonymous.GetAsync(new Uri(path, UriKind.Relative), ct);
            var body = await forbidden.Content.ReadAsStringAsync(ct);

            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden, path);
            body.ShouldNotContain("totalUsers");
            body.ShouldNotContain("items");
            body.ShouldNotContain("@ornek.test");
            unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }
    }

    /// <summary>
    /// Bilinen veri setinde özet sayılar birebir: toplam kullanıcı, klip (bir videonun yeni sürümü ayrı sayılmaz), açık hata bildirimi
    /// (saklanan tüm "Yeni" bildirimler); 30 günlük seride günlük kayıt / klip / aktif kullanıcı (aynı gün iki olay bir kullanıcı,
    /// kullanıcısız olay sayılmaz), veri olmayan gün 0; 90 gün aralığı 40 gün önceki veriyi de gösterir; geçersiz aralık 30'dur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Statistics_KnownDataset_ExactTotalsAndDailySeries()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct); // admin bugün kayıt + giriş: bugün 1 kayıt, 1 aktif.
        var ayse = await AddUserAsync(factory, "Ayşe Yılmaz", "ayse@ornek.test", At(-3, 10), ct);
        var veli = await AddUserAsync(factory, "Veli Demir", "veli@ornek.test", At(-40, 10), ct);
        var conceptA = Guid.NewGuid();
        await AddVideoAsync(factory, ayse, conceptA, At(-2, 15), ct);
        await AddVideoAsync(factory, ayse, conceptA, At(-1, 15), ct); // yeni sürüm: ayrı klip değil.
        await AddVideoAsync(factory, veli, Guid.NewGuid(), At(0, 8), ct);
        await AddVideoAsync(factory, veli, Guid.NewGuid(), At(-40, 12), ct);
        await AddReportAsync(factory, ayse, ct);
        await AddReportAsync(factory, veli, ct);
        await AddEventAsync(factory, ayse, At(-3, 11), ct);
        await AddEventAsync(factory, ayse, At(-3, 23), ct); // aynı gün ikinci olay: aktif kullanıcı yine 1.
        await AddEventAsync(factory, ayse, At(-1, 0), ct); // Istanbul gece yarısı: -1. gün.
        await AddEventAsync(factory, veli, At(-1, 9), ct);
        await AddEventAsync(factory, null, At(-1, 9), ct); // kullanıcısız sistem olayı sayılmaz.

        var month = await admin.GetFromJsonAsync<JsonElement>(Statistics, ct);
        var quarter = await admin.GetFromJsonAsync<JsonElement>(Statistics + "?days=90", ct);
        var week = await admin.GetFromJsonAsync<JsonElement>(Statistics + "?days=7", ct);
        var invalid = await admin.GetFromJsonAsync<JsonElement>(Statistics + "?days=12", ct);

        month.GetProperty("totalUsers").GetInt32().ShouldBe(3);
        month.GetProperty("totalClips").GetInt32().ShouldBe(3);
        month.GetProperty("openErrorReports").GetInt32().ShouldBe(2);
        month.GetProperty("days").GetInt32().ShouldBe(30);
        var series = Series(month);
        series.Count.ShouldBe(30);
        series.Keys.First().ShouldBe(Day(-29));
        series.Keys.Last().ShouldBe(Day(0));
        series[Day(0)].ShouldBe((1, 1, 1));
        series[Day(-1)].ShouldBe((0, 0, 2));
        series[Day(-2)].ShouldBe((0, 1, 0));
        series[Day(-3)].ShouldBe((1, 0, 1));
        series[Day(-5)].ShouldBe((0, 0, 0));
        series.Values.Sum(v => v.Clips).ShouldBe(2);

        var long90 = Series(quarter);
        long90.Count.ShouldBe(90);
        long90[Day(-40)].ShouldBe((1, 1, 0));
        Series(week).Count.ShouldBe(7);
        invalid.GetProperty("days").GetInt32().ShouldBe(30);
    }

    /// <summary>Hiç veri yokken tüm sayılar ve tüm günlerin değerleri 0'dır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Statistics_Empty_AllZero()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = new AdminStatisticsService(
            new InMemoryUserDirectory(new InMemoryUserRepository()),
            new InMemoryClipStatistics(new InMemoryProducedVideoRepository()),
            new InMemoryAdminActivityRepository(new InMemorySystemEventRepository(), new InMemoryErrorReportRepository()),
            TimeProvider.System);

        var result = (await service.GetStatisticsAsync(null, ct)).Value!;
        var activities = (await service.GetActivitiesAsync(null, ct)).Value!;

        result.TotalUsers.ShouldBe(0);
        result.TotalClips.ShouldBe(0);
        result.OpenErrorReports.ShouldBe(0);
        result.Series.Count.ShouldBe(30);
        result.Series.ShouldAllBe(d => d.Registrations == 0 && d.Clips == 0 && d.ActiveUsers == 0);
        activities.Items.ShouldBeEmpty();
        activities.Total.ShouldBe(0);
        activities.Filtered.ShouldBeFalse();
    }

    /// <summary>
    /// Aktiviteler en yeni üstte, zaman + ad + e-posta + tür etiketiyle, 25'erli sayfalanır ve sayfalar tüm kayıtlara erişir;
    /// kullanıcı filtresi ad/e-postada büyük/küçük harf duyarsız "içerir"dir ve 2 karakterden kısaysa uygulanmaz; tarih aralığı
    /// Istanbul günlerine göre dahil süzer; uyan yoksa boş ve <c>filtered</c>; kullanıcısız olay listelenmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Activities_NewestFirst_Paged25_Filters()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct); // admin: kayıt + giriş = 2 olay (bugün 09:00 UTC).
        var ayse = await AddUserAsync(factory, "Ayşe Yılmaz", "ayse@ornek.test", At(-10, 9), ct);
        var veli = await AddUserAsync(factory, "Veli Demir", "veli@baska.test", At(-10, 9), ct);
        for (var i = 0; i < 30; i++)
        {
            await AddEventAsync(factory, ayse, At(-5, 0).AddMinutes(i * 10), ct, "agent.connected");
        }

        await AddEventAsync(factory, veli, At(-1, 23), ct, "error_report.sent");
        await AddEventAsync(factory, veli, At(-2, 0), ct, "identity.logged_out");
        await AddEventAsync(factory, veli, At(-3, 12), ct, "identity.login_failed");
        await AddEventAsync(factory, null, At(-1, 10), ct);

        var page1 = await admin.GetFromJsonAsync<JsonElement>(Activities, ct);
        var page2 = await admin.GetFromJsonAsync<JsonElement>(Activities + "?page=2", ct);
        var all = Items(page1).Concat(Items(page2)).ToList();
        var byUser = await admin.GetFromJsonAsync<JsonElement>(Activities + "?user=AYŞ", ct);
        var byEmail = await admin.GetFromJsonAsync<JsonElement>(Activities + "?user=BASKA.TE", ct);
        var shortSearch = await admin.GetFromJsonAsync<JsonElement>(Activities + "?user=v", ct);
        var dates = await admin.GetFromJsonAsync<JsonElement>($"{Activities}?from={Day(-2)}&to={Day(-1)}", ct);
        var none = await admin.GetFromJsonAsync<JsonElement>(Activities + "?user=zzz", ct);

        page1.GetProperty("total").GetInt32().ShouldBe(35);
        page1.GetProperty("pageSize").GetInt32().ShouldBe(25);
        page1.GetProperty("filtered").GetBoolean().ShouldBeFalse();
        Items(page1).Count.ShouldBe(25);
        Items(page2).Count.ShouldBe(10);
        all.Select(a => a.GetProperty("occurredAt").GetDateTimeOffset()).ShouldBeInOrder(SortDirection.Descending);
        all[0].GetProperty("userEmail").GetString().ShouldBe(IdentityApiFactory.AdminEmail);
        all[2].GetProperty("userName").GetString().ShouldBe("Veli Demir");
        all[2].GetProperty("userEmail").GetString().ShouldBe("veli@baska.test");
        all[2].GetProperty("activity").GetString().ShouldBe("Hata bildirdi");
        all[3].GetProperty("activity").GetString().ShouldBe("Çıkış");
        all[4].GetProperty("activity").GetString().ShouldBe("Başarısız giriş");
        all[5].GetProperty("activity").GetString().ShouldBe("Ajan bağlandı");
        all.Select(a => a.GetProperty("occurredAt").GetDateTimeOffset()).Distinct().Count().ShouldBe(34); // admin'in iki olayı aynı anda.

        byUser.GetProperty("total").GetInt32().ShouldBe(30);
        byUser.GetProperty("filtered").GetBoolean().ShouldBeTrue();
        Items(byUser).ShouldAllBe(a => a.GetProperty("userName").GetString() == "Ayşe Yılmaz");
        byEmail.GetProperty("total").GetInt32().ShouldBe(3);
        shortSearch.GetProperty("total").GetInt32().ShouldBe(35);
        shortSearch.GetProperty("filtered").GetBoolean().ShouldBeFalse();
        dates.GetProperty("total").GetInt32().ShouldBe(2);
        Items(dates).Select(a => a.GetProperty("activity").GetString()).ShouldBe(["Hata bildirdi", "Çıkış"]);
        none.GetProperty("total").GetInt32().ShouldBe(0);
        none.GetProperty("filtered").GetBoolean().ShouldBeTrue();
        Items(none).ShouldBeEmpty();
    }

    /// <summary>Başlangıç bitişten sonraysa tarih filtresi uygulanmaz ve yanıt bunu bildirir (uyarı arayüzde).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Activities_InvalidDateRange_Ignored()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var ayse = await AddUserAsync(factory, "Ayşe", "ayse@ornek.test", At(-10, 9), ct);
        await AddEventAsync(factory, ayse, At(-5, 10), ct);

        var reversed = await admin.GetFromJsonAsync<JsonElement>($"{Activities}?from={Day(-1)}&to={Day(-6)}", ct);
        var valid = await admin.GetFromJsonAsync<JsonElement>($"{Activities}?from={Day(-6)}&to={Day(-1)}", ct);

        reversed.GetProperty("dateFilterIgnored").GetBoolean().ShouldBeTrue();
        reversed.GetProperty("total").GetInt32().ShouldBe(3);
        valid.GetProperty("dateFilterIgnored").GetBoolean().ShouldBeFalse();
        valid.GetProperty("total").GetInt32().ShouldBe(1);

        using var huge = await admin.GetAsync(new Uri(Activities + "?page=2147483647", UriKind.Relative), ct); // review r1 L-1
        using var farDate = await admin.GetAsync(new Uri(Activities + "?to=9999-12-31", UriKind.Relative), ct);
        huge.StatusCode.ShouldBe(HttpStatusCode.OK);
        Items(await huge.Content.ReadFromJsonAsync<JsonElement>(ct)).ShouldBeEmpty();
        farDate.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.ErrorReports.Reports.ShouldBeEmpty(); // sınır girdisi otomatik hata kaydı açmaz.
        var ancient = await admin.GetFromJsonAsync<JsonElement>(Activities + "?to=1999-12-31", ct); // review r2 L-3: yok sayılmaz.
        ancient.GetProperty("total").GetInt32().ShouldBe(0);
        ancient.GetProperty("filtered").GetBoolean().ShouldBeTrue();
    }

    /// <summary>
    /// Yanıtlar kullanıcı içeriği taşımaz: bilinen içerikli veri setinde (Günlük metni, profil adı, video başlığı ve konsept metni,
    /// hata bildirimi açıklaması, API key benzeri metin) iki ucun ham yanıtında arama ile hiçbiri bulunmaz (BR-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminData_NoUserContent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var ayse = await AddUserAsync(factory, "Ayşe", "ayse@ornek.test", At(-3, 9), ct);
        await factory.Events.AddAsync(
            SystemEvent.Create("production.video", EventLevel.Info, "Video hazırlandı: GIZLIBASLIK sk-proj-GIZLIANAHTAR12345", At(-1, 10), ayse)
                .WithDetails(At(-1, 9), At(-1, 10), Guid.NewGuid(), "GIZLIPROFIL"),
            ct);
        await factory.Events.AddAsync(SystemEvent.Create("concepts.generation", EventLevel.Info, "Konsept üretimi: 2/2 tamamlandı GIZLIMETIN", At(-1, 11), ayse), ct);
        await AddVideoAsync(factory, ayse, Guid.NewGuid(), At(-1, 10), ct);
        await factory.ErrorReports.AddAsync(ErrorReport.CreateWeb(ayse, At(-1, 12), "0.0.0", "GIZLIACIKLAMA", "/", null, ["GIZLIGUNLUK"]), ct);

        var statistics = await admin.GetStringAsync(new Uri(Statistics, UriKind.Relative), ct);
        var activities = await admin.GetStringAsync(new Uri(Activities, UriKind.Relative), ct);

        activities.ShouldContain("Video tamamlandı");
        activities.ShouldContain("Konsept üretti (2)");
        foreach (var secret in new[] { "GIZLI", "sk-proj", "hazırlandı", "tamamlandı GIZ" })
        {
            statistics.ShouldNotContain(secret);
            activities.ShouldNotContain(secret);
        }
    }

    /// <summary>Sunucu hatasında yanıt sade ProblemDetails + korelasyon numarasıdır; SQL, istisna metni ya da yığın izi dönmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminData_ServerError_NoStackTrace()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        factory.AdminActivities.Fail = true;

        foreach (var path in new[] { Statistics, Activities })
        {
            using var response = await admin.GetAsync(new Uri(path, UriKind.Relative), ct);
            var text = await response.Content.ReadAsStringAsync(ct);

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError, path);
            JsonDocument.Parse(text).RootElement.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
            text.ShouldNotContain("SELECT");
            text.ShouldNotContain("gizli_tablo");
            text.ShouldNotContain("InvalidOperationException");
            text.ShouldNotContain(" at ");
        }
    }

    private static DateTimeOffset At(int dayOffset, int hour) =>
        new DateTimeOffset(Today.AddDays(dayOffset).ToDateTime(new TimeOnly(hour, 0)), Istanbul).ToUniversalTime();

    private static string Day(int offset) => Today.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Dictionary<string, (int Registrations, int Clips, int ActiveUsers)> Series(JsonElement statistics) =>
        statistics.GetProperty("series").EnumerateArray().ToDictionary(
            d => d.GetProperty("date").GetString()!,
            d => (d.GetProperty("registrations").GetInt32(), d.GetProperty("clips").GetInt32(), d.GetProperty("activeUsers").GetInt32()));

    private static List<JsonElement> Items(JsonElement page) => [.. page.GetProperty("items").EnumerateArray()];

    private static async Task<Guid> AddUserAsync(IdentityApiFactory factory, string name, string email, DateTimeOffset registeredAt, CancellationToken ct)
    {
        var user = User.Register(name, email, "hash", registeredAt);
        (await factory.Users.TryAddAsync(user, ct)).ShouldBeTrue();
        return user.Id;
    }

    private static Task AddVideoAsync(IdentityApiFactory factory, Guid userId, Guid conceptId, DateTimeOffset endedAt, CancellationToken ct) =>
        factory.ProducedVideos.AddAsync(
            new ProducedVideo
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ProfileId = Guid.NewGuid(),
                ConceptId = conceptId,
                JobId = Guid.NewGuid(),
                Title = "GIZLIVIDEOBASLIK",
                ConceptText = "{\"t\":\"GIZLIKONSEPT\"}",
                Engine = "remotion",
                FileName = "v.mp4",
                StartedAt = endedAt.AddMinutes(-5),
                EndedAt = endedAt,
            },
            ct);

    private static Task AddReportAsync(IdentityApiFactory factory, Guid userId, CancellationToken ct) =>
        factory.ErrorReports.AddAsync(ErrorReport.CreateWeb(userId, At(-1, 12), "0.0.0", "açıklama", "/", null, []), ct);

    private static Task AddEventAsync(IdentityApiFactory factory, Guid? userId, DateTimeOffset at, CancellationToken ct, string type = "identity.login_succeeded") =>
        factory.Events.AddAsync(SystemEvent.Create(type, EventLevel.Info, "olay", at, userId), ct);
}
