using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Spec 0008 AC-9: dönem takvimine kayıt ekleme (tek gün dahil), düzenleme, silme; bitiş başlangıçtan önce, dönem dışı
/// gün, boş/uzun başlık, geçersiz tür reddi ve değişiklik yok; başlangıç gününe göre sıra; silinen kayıt yönetim
/// listesinde ve okul takviminde görünmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CalendarAdminTests
{
    private readonly TestDatabase database;

    public CalendarAdminTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_Update_Delete_ValidatesAndKeepsEntriesInsideTerm()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)));
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid term = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");
        string entries = $"/api/admin/terms/{term}/calendar-entries";

        var (created, holiday, _) = await CallAsync(client, HttpMethod.Post, entries, token, new { title = " Cumhuriyet Bayramı ", kind = "Holiday", startDate = "2026-10-29", endDate = "2026-10-29" });
        created.ShouldBe(HttpStatusCode.OK, "tek günlük kayıt");
        holiday.GetProperty("title").GetString().ShouldBe("Cumhuriyet Bayramı");
        holiday.GetProperty("kind").GetString().ShouldBe("Holiday");
        Guid exam = await CreateCalendarEntryAsync(client, token, term, "Ara sınav haftası", "Exam", "2026-11-09", "2026-11-13");
        Guid trip = await CreateCalendarEntryAsync(client, token, term, "Müze gezisi", "Event", "2026-10-02", "2026-10-02");
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = "Dönem başı", kind = "Event", startDate = "2026-09-01", endDate = "2026-09-01" })).ShouldBe(HttpStatusCode.OK, "dönemin ilk günü içeride");

        (await TitlesAsync(client, token, entries)).ShouldBe(["Dönem başı", "Müze gezisi", "Cumhuriyet Bayramı", "Ara sınav haftası"]);

        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = "Ters", kind = "Event", startDate = "2026-10-10", endDate = "2026-10-09" })).ShouldBe(HttpStatusCode.BadRequest);
        var (before, beforeProblem, _) = await CallAsync(client, HttpMethod.Post, entries, token, new { title = "Önce", kind = "Event", startDate = "2026-08-31", endDate = "2026-09-02" });
        before.ShouldBe(HttpStatusCode.BadRequest);
        Detail(beforeProblem).ShouldBe("Kayıt dönemin dışında");
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = "Sonra", kind = "Event", startDate = "2027-01-20", endDate = "2027-01-23" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = " ", kind = "Event", startDate = "2026-10-10", endDate = "2026-10-10" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = new string('x', 101), kind = "Event", startDate = "2026-10-10", endDate = "2026-10-10" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = "Parti", kind = "Party", startDate = "2026-10-10", endDate = "2026-10-10" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = "Türsüz", startDate = "2026-10-10", endDate = "2026-10-10" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, entries, token, new { title = "Tarihsiz", kind = "Event" })).ShouldBe(HttpStatusCode.BadRequest);
        (await QueryAsync(factory, c => c.CalendarEntries.IgnoreQueryFilters().CountAsync(e => e.TenantId == a.TenantId))).ShouldBe(4);

        var (updated, moved, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/calendar-entries/{trip}", token, new { title = "Okul gezisi", kind = "Event", startDate = "2026-12-01", endDate = "2026-12-02" });
        updated.ShouldBe(HttpStatusCode.OK);
        moved.GetProperty("title").GetString().ShouldBe("Okul gezisi");
        (await TitlesAsync(client, token, entries)).ShouldBe(["Dönem başı", "Cumhuriyet Bayramı", "Ara sınav haftası", "Okul gezisi"]);
        var (outside, outsideProblem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/calendar-entries/{trip}", token, new { title = "Okul gezisi", kind = "Event", startDate = "2027-01-22", endDate = "2027-01-25" });
        outside.ShouldBe(HttpStatusCode.BadRequest);
        Detail(outsideProblem).ShouldBe("Kayıt dönemin dışında");
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/calendar-entries/{trip}", token, new { title = "Okul gezisi", kind = "Gezi", startDate = "2026-12-01", endDate = "2026-12-02" })).ShouldBe(HttpStatusCode.BadRequest);
        JsonElement still = (await CallAsync(client, HttpMethod.Get, entries, token)).Body.EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == trip);
        still.GetProperty("startDate").GetString().ShouldBe("2026-12-01");
        still.GetProperty("kind").GetString().ShouldBe("Event");

        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/calendar-entries/{exam}", token)).ShouldBe(HttpStatusCode.NoContent);
        (await TitlesAsync(client, token, entries)).ShouldNotContain("Ara sınav haftası");
        var (_, calendar, _) = await CallAsync(client, HttpMethod.Get, "/api/calendar", token);
        calendar.GetProperty("entries").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ShouldNotContain(exam);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/calendar-entries/{exam}", token, new { title = "Geri", kind = "Exam", startDate = "2026-11-09", endDate = "2026-11-09" })).ShouldBe(HttpStatusCode.NotFound);
        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/calendar-entries/{exam}", token)).ShouldBe(HttpStatusCode.NotFound);
        (await QueryAsync(factory, c => c.CalendarEntries.IgnoreQueryFilters().Where(e => e.Id == exam).Select(e => e.DeletedAt).SingleAsync())).ShouldNotBeNull("yumuşak silme");
    }

    private static async Task<string?[]> TitlesAsync(HttpClient client, string token, string path)
    {
        var (_, list, _) = await CallAsync(client, HttpMethod.Get, path, token);
        return [.. list.EnumerateArray().Select(e => e.GetProperty("title").GetString())];
    }
}
