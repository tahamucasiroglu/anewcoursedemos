using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Günlük şeridi ve geçmişi Api tarafı (spec 0021 AC-1, AC-3, AC-6–AC-10, AC-12): sahiplik, gün sınırları, 50'lik sayfa, filtre,
/// süresiz saklama, süre ve profil alanları.
/// </summary>
public sealed class JournalApiTests : IClassFixture<FastApiFactory>
{
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="JournalApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public JournalApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    private static DateOnly Today => JournalDay.DayOf(DateTimeOffset.UtcNow);

    /// <summary>
    /// A'nın kaydı B'ye hiçbir yoldan (şerit, geçmiş, tarih parametresiyle doğrudan istek) görünmez; kimliksiz Günlük isteği 401'dir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_OwnOnly_OtherUserAndAnonymousRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var emailA = AuthenticatedClient.NewEmail();
        using var userA = await AuthenticatedClient.CreateAsync(_factory, ct, emailA);
        using var userB = await _factory.CreateAuthenticatedClientAsync(ct);
        var idA = _factory.Users.Get(emailA).Id;
        var day = Today.AddDays(-2);
        await AddAsync(idA, At(day, 0), EventLevel.Error, "A-GIZLI-KAYIT");
        await AddAsync(idA, DateTimeOffset.UtcNow, EventLevel.Warning, "A-GIZLI-SON");

        var aDay = await DayAsync(userA, $"date={Iso(day)}", ct);
        var bDay = await DayAsync(userB, $"date={Iso(day)}", ct);
        var bToday = await DayAsync(userB, string.Empty, ct);
        var bStatus = await userB.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        var aStatus = await userA.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        using var anonymous = _factory.CreateClient();
        using var anonymousResponse = await anonymous.GetAsync(new Uri($"/api/journal?date={Iso(day)}", UriKind.Relative), ct);

        aDay.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("message").GetString()).ShouldContain("A-GIZLI-KAYIT");
        (bDay.ToString() + bToday + bStatus).ShouldNotContain("A-GIZLI");
        aStatus.GetProperty("lastEvent").GetProperty("message").GetString().ShouldBe("A-GIZLI-SON");
        anonymousResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Günde ilk 50 kayıt en yenisi üstte; 51. ve sonrası "Daha fazla" (skip=50) ile gelir; tam 50 kayıtlı günde devamı yoktur;
    /// "Hata" filtresi Hata + Kritik, "Uyarı" filtresi yalnızca Uyarı gösterir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_Day_FiftyPerPageAndFilters()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var id = _factory.Users.Get(email).Id;
        var day = Today.AddDays(-3);
        var exactDay = Today.AddDays(-4);
        for (var i = 0; i < 55; i++)
        {
            var level = i switch { 0 => EventLevel.Critical, 1 => EventLevel.Error, < 5 => EventLevel.Warning, _ => EventLevel.Info };
            await AddAsync(id, At(day, i), level, $"kayıt {i:00}");
        }

        for (var i = 0; i < 50; i++)
        {
            await AddAsync(id, At(exactDay, i), EventLevel.Info, $"tam {i:00}");
        }

        var first = await DayAsync(client, $"date={Iso(day)}", ct);
        var more = await DayAsync(client, $"date={Iso(day)}&skip=50", ct);
        var errors = await DayAsync(client, $"date={Iso(day)}&filter=error", ct);
        var warnings = await DayAsync(client, $"date={Iso(day)}&filter=warning", ct);
        var exact = await DayAsync(client, $"date={Iso(exactDay)}", ct);

        Messages(first).Count.ShouldBe(50);
        Messages(first)[0].ShouldBe("kayıt 54");
        first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        Messages(more).ShouldBe(["kayıt 04", "kayıt 03", "kayıt 02", "kayıt 01", "kayıt 00"]);
        more.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        Messages(errors).ShouldBe(["kayıt 01", "kayıt 00"]);
        errors.GetProperty("items")[1].GetProperty("level").GetInt32().ShouldBe((int)SystemEventLevel.Critical);
        Messages(warnings).ShouldBe(["kayıt 04", "kayıt 03", "kayıt 02"]);
        Messages(exact).Count.ShouldBe(50);
        exact.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    /// <summary>
    /// Bugün: sonraki gün yok (isToday); ileri ya da geçersiz tarih bugünü döndürür; yeni hesapta bugün ilk gündür; en eski kaydın günü
    /// ilk gündür ve ondan eski istek o güne sıkışır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_Bounds_TodayAndFirstDay()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);

        var today = await DayAsync(client, string.Empty, ct);
        var future = await DayAsync(client, $"date={Iso(Today.AddDays(3))}", ct);
        var invalid = await DayAsync(client, "date=abc", ct);
        var beforeAccount = await DayAsync(client, $"date={Iso(Today.AddDays(-1))}", ct);

        today.GetProperty("date").GetString().ShouldBe(Iso(Today));
        today.GetProperty("isToday").GetBoolean().ShouldBeTrue();
        today.GetProperty("isFirstDay").GetBoolean().ShouldBeTrue();
        future.GetProperty("date").GetString().ShouldBe(Iso(Today));
        invalid.GetProperty("date").GetString().ShouldBe(Iso(Today));
        beforeAccount.GetProperty("date").GetString().ShouldBe(Iso(Today));

        await AddAsync(_factory.Users.Get(email).Id, At(Today.AddDays(-5), 1), EventLevel.Info, "eski");
        var yesterday = await DayAsync(client, $"date={Iso(Today.AddDays(-1))}", ct);
        var oldest = await DayAsync(client, $"date={Iso(Today.AddDays(-5))}", ct);
        var older = await DayAsync(client, $"date={Iso(Today.AddDays(-10))}", ct);

        yesterday.GetProperty("isToday").GetBoolean().ShouldBeFalse();
        yesterday.GetProperty("isFirstDay").GetBoolean().ShouldBeFalse();
        oldest.GetProperty("isFirstDay").GetBoolean().ShouldBeTrue();
        older.GetProperty("date").GetString().ShouldBe(Iso(Today.AddDays(-5)));
    }

    /// <summary>8 ve 400 gün önceki kayıtlar hâlâ görünür (süresiz saklama); metindeki HTML olduğu gibi, API key maskeli döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_OldDays_StillVisible()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var id = _factory.Users.Get(email).Id;
        await AddAsync(id, At(Today.AddDays(-8), 2), EventLevel.Warning, "sekiz gün önce <script>alert(1)</script>");
        await AddAsync(id, At(Today.AddDays(-400), 3), EventLevel.Info, "dört yüz gün önce anahtar sk-ant-api03-abcdefa3f");

        var eight = await DayAsync(client, $"date={Iso(Today.AddDays(-8))}", ct);
        var fourHundred = await DayAsync(client, $"date={Iso(Today.AddDays(-400))}", ct);

        Messages(eight).ShouldBe(["sekiz gün önce <script>alert(1)</script>"]);
        var old = Messages(fourHundred).Single() ?? string.Empty;
        old.ShouldStartWith("dört yüz gün önce");
        old.ShouldNotContain("api03-abcdefa3f");
        fourHundred.GetProperty("isFirstDay").GetBoolean().ShouldBeTrue();
    }

    /// <summary>
    /// Şerit kendi son kaydını süre (başlangıç–bitiş) ve profil etiketiyle alır; profile ait olmayan kayıt etiketsizdir ve geçmiş
    /// profile göre süzülmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_StatusLastEvent_OwnWithDurationAndProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var id = _factory.Users.Get(email).Id;
        var end = DateTimeOffset.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<ISystemEventWriter>();
            await writer.WriteAsync(new SystemEventEntry("video.ready", SystemEventLevel.Info, "Video hazırlandı", id, end.AddSeconds(-57), end, Guid.NewGuid(), "Kanal B"), ct);
        }

        var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        var history = await DayAsync(client, string.Empty, ct);

        var last = status.GetProperty("lastEvent");
        last.GetProperty("message").GetString().ShouldBe("Video hazırlandı");
        last.GetProperty("profileName").GetString().ShouldBe("Kanal B");
        (last.GetProperty("endedAt").GetDateTimeOffset() - last.GetProperty("startedAt").GetDateTimeOffset()).ShouldBe(TimeSpan.FromSeconds(57));
        var items = history.GetProperty("items").EnumerateArray().ToList();
        items[0].GetProperty("profileName").GetString().ShouldBe("Kanal B");
        items.Skip(1).ShouldAllBe(i => i.GetProperty("profileName").ValueKind == JsonValueKind.Null); // giriş/kayıt: etiketsiz
        items.Count.ShouldBeGreaterThan(1);
    }

    private static DateTimeOffset At(DateOnly day, int second) => JournalDay.RangeOf(day).FromUtc.AddHours(10).AddSeconds(second);

    private static string Iso(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static List<string?> Messages(JsonElement day) =>
        day.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("message").GetString()).ToList();

    private static async Task<JsonElement> DayAsync(HttpClient client, string query, CancellationToken ct)
    {
        using var response = await client.GetAsync(new Uri("/api/journal?" + query, UriKind.Relative), ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    private Task AddAsync(Guid userId, DateTimeOffset at, EventLevel level, string message) =>
        _factory.Events.AddAsync(SystemEvent.Create("test.event", level, message, at, userId), CancellationToken.None);
}
