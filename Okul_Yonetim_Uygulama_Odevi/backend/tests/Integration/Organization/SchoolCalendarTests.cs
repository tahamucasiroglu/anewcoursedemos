using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Organization;

/// <summary>
/// Spec 0008 AC-6, AC-10 (okul takvimi, sahte saat): Türkiye gece yarısından sonra doğru dönem; yaklaşan dönem ibaresi;
/// kayıtlar tarih sırasıyla, silinen yok; dönem yoksa / yalnızca geçmiş dönem varsa boş, kayıt yoksa boş liste; her rol
/// aynı takvimi görür.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SchoolCalendarTests
{
    private readonly TestDatabase database;

    public SchoolCalendarTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Calendar_AfterTurkeyMidnight_ShowsStartedTerm_WithEntriesInOrder_ForEveryRole()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 31, 21, 30, 0, TimeSpan.Zero));
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        await CreateTermAsync(client, founder, "2025-2026", "2025-09-01", "2026-06-19");
        Guid autumn = await CreateTermAsync(client, founder, "2026-2027 Güz", "2026-09-01", "2027-01-22");
        await CreateCalendarEntryAsync(client, founder, autumn, "Ara sınav haftası", "Exam", "2026-11-09", "2026-11-13");
        await CreateCalendarEntryAsync(client, founder, autumn, "Cumhuriyet Bayramı", "Holiday", "2026-10-29", "2026-10-29");
        Guid removed = await CreateCalendarEntryAsync(client, founder, autumn, "İptal edilen", "Event", "2026-10-01", "2026-10-01");
        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/calendar-entries/{removed}", founder)).ShouldBe(HttpStatusCode.NoContent);

        foreach (Role role in new[] { Role.Teacher, Role.Guardian, Role.Accounting })
        {
            string email = UniqueEmail("takvim");
            await AddMemberAsync(factory, email, a.TenantId, role, a.BranchId);
            string token = await TokenForSchoolAsync(client, email, a.TenantId);
            var (status, calendar, _) = await CallAsync(client, HttpMethod.Get, "/api/calendar", token);

            status.ShouldBe(HttpStatusCode.OK);
            JsonElement term = calendar.GetProperty("term");
            term.GetProperty("id").GetGuid().ShouldBe(autumn, "UTC 31 Ağustos 21:30 = Türkiye 1 Eylül 00:30");
            term.GetProperty("name").GetString().ShouldBe("2026-2027 Güz");
            term.GetProperty("startDate").GetString().ShouldBe("2026-09-01");
            term.GetProperty("endDate").GetString().ShouldBe("2027-01-22");
            term.GetProperty("isCurrent").GetBoolean().ShouldBeTrue();
            term.GetProperty("isUpcoming").GetBoolean().ShouldBeFalse();
            calendar.GetProperty("entries").EnumerateArray()
                .Select(e => (e.GetProperty("kind").GetString(), e.GetProperty("title").GetString(), e.GetProperty("startDate").GetString(), e.GetProperty("endDate").GetString()))
                .ShouldBe([("Holiday", "Cumhuriyet Bayramı", "2026-10-29", "2026-10-29"), ("Exam", "Ara sınav haftası", "2026-11-09", "2026-11-13")]);
        }
    }

    [Fact]
    public async Task Calendar_BetweenTerms_ShowsUpcoming_WithEmptyEntries_AndNoneWhenOnlyPastOrNoTerms()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        var (_, none, _) = await CallAsync(client, HttpMethod.Get, "/api/calendar", founder);
        none.GetProperty("term").ValueKind.ShouldBe(JsonValueKind.Null, "dönem yok: 'Henüz dönem tanımlanmadı.'");
        none.GetProperty("entries").GetArrayLength().ShouldBe(0);

        await CreateTermAsync(client, founder, "2025-2026", "2025-09-01", "2026-06-19");
        var (_, onlyPast, _) = await CallAsync(client, HttpMethod.Get, "/api/calendar", founder);
        onlyPast.GetProperty("term").ValueKind.ShouldBe(JsonValueKind.Null, "yalnızca geçmiş dönem varsa güncel dönem yok");

        Guid spring = await CreateTermAsync(client, founder, "2027 Bahar", "2027-02-08", "2027-06-18");
        Guid autumn = await CreateTermAsync(client, founder, "2026 Güz", "2026-09-01", "2027-01-22");
        await CreateCalendarEntryAsync(client, founder, spring, "Bahar şenliği", "Event", "2027-05-01", "2027-05-01");
        var (_, upcoming, _) = await CallAsync(client, HttpMethod.Get, "/api/calendar", founder);
        upcoming.GetProperty("term").GetProperty("id").GetGuid().ShouldBe(autumn, "en yakın başlayacak dönem");
        upcoming.GetProperty("term").GetProperty("isUpcoming").GetBoolean().ShouldBeTrue("'Yaklaşan dönem' ibaresi");
        upcoming.GetProperty("term").GetProperty("isCurrent").GetBoolean().ShouldBeFalse();
        upcoming.GetProperty("entries").GetArrayLength().ShouldBe(0, "dönemde kayıt yok: 'Bu dönemde takvim kaydı yok.'");
    }
}
