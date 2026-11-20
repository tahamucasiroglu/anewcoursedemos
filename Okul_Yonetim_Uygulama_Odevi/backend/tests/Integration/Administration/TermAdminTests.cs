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
/// Spec 0008 AC-4, AC-5, AC-6 (yönetim listesi): dönem alan sınırları (366 kabul, 367 ret), okulda benzersiz ad, silme yok;
/// çakışma (bitiş = başlangıç çakışır, ertesi gün serbest, başka okul serbest); takvim kaydını dışarıda bırakan düzenleme
/// reddi; listede güncel/yaklaşan etiketi Türkiye gününe göre.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TermAdminTests
{
    private const string Path = "/api/admin/terms";
    private readonly TestDatabase database;

    public TermAdminTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_ValidatesFieldsAndLength_NameUniqueInSchool_NoDelete()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = "", startDate = "2026-09-01", endDate = "2027-01-22" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = new string('x', 101), startDate = "2026-09-01", endDate = "2027-01-22" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = "Eksik", endDate = "2027-01-22" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = "Eksik", startDate = "2026-09-01" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = "Aynı gün", startDate = "2026-09-01", endDate = "2026-09-01" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = "Ters", startDate = "2026-09-01", endDate = "2026-08-01" })).ShouldBe(HttpStatusCode.BadRequest);
        var (tooLong, problem, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { name = "367 gün", startDate = "2026-09-01", endDate = "2027-09-02" });
        tooLong.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain("Dönem en fazla 366 gün sürebilir");
        (await QueryAsync(factory, c => c.Terms.IgnoreQueryFilters().CountAsync(t => t.TenantId == a.TenantId))).ShouldBe(0);

        var (created, term, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { name = "2026-2027 Güz", startDate = "2026-09-01", endDate = "2027-09-01" });
        created.ShouldBe(HttpStatusCode.OK, "366 günlük dönem kabul edilir");
        term.GetProperty("startDate").GetString().ShouldBe("2026-09-01");
        term.GetProperty("endDate").GetString().ShouldBe("2027-09-01");
        Guid id = term.GetProperty("id").GetGuid();

        var (dup, dupProblem, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { name = "2026-2027 GÜZ", startDate = "2028-09-01", endDate = "2029-01-22" });
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(dupProblem).ShouldBe("Bu adla bir dönem zaten var");

        (await StatusAsync(client, HttpMethod.Delete, $"{Path}/{id}", token)).ShouldBe(HttpStatusCode.MethodNotAllowed, "dönem silinmez");
        (await StatusAsync(client, HttpMethod.Post, $"{Path}/{id}/deactivate", token)).ShouldBe(HttpStatusCode.NotFound, "dönem pasifleştirilmez");
        (await QueryAsync(factory, c => c.Terms.IgnoreQueryFilters().CountAsync(t => t.TenantId == a.TenantId))).ShouldBe(1);
    }

    [Fact]
    public async Task Overlap_EndEqualsStartRejected_NextDayAccepted_OtherSchoolFree_EditKeepsEntriesInside()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string tokenB = await TokenForSchoolAsync(client, b.FounderEmail, b.TenantId);
        Guid autumn = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");

        var (overlap, problem, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { name = "Bahar", startDate = "2027-01-22", endDate = "2027-06-18" });
        overlap.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu tarihler başka bir dönemle çakışıyor");
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { name = "Kapsayan", startDate = "2026-08-01", endDate = "2027-02-01" })).ShouldBe(HttpStatusCode.BadRequest);
        Guid spring = await CreateTermAsync(client, token, "Bahar", "2027-01-23", "2027-06-18");
        (await StatusAsync(client, HttpMethod.Post, Path, tokenB, new { name = "Güz", startDate = "2026-09-01", endDate = "2027-01-22" })).ShouldBe(HttpStatusCode.OK, "başka okulun dönemiyle çakışma serbest");

        var (editOverlap, editProblem, _) = await CallAsync(client, HttpMethod.Put, $"{Path}/{spring}", token, new { name = "Bahar", startDate = "2027-01-20", endDate = "2027-06-18" });
        editOverlap.ShouldBe(HttpStatusCode.BadRequest);
        Detail(editProblem).ShouldBe("Bu tarihler başka bir dönemle çakışıyor");

        await CreateCalendarEntryAsync(client, token, autumn, "Yarıyıl sınavı", "Exam", "2027-01-15", "2027-01-15");
        var (outside, outsideProblem, _) = await CallAsync(client, HttpMethod.Put, $"{Path}/{autumn}", token, new { name = "Güz dönemi", startDate = "2026-09-01", endDate = "2027-01-10" });
        outside.ShouldBe(HttpStatusCode.BadRequest);
        Detail(outsideProblem).ShouldBe("Dönemin dışında kalan takvim kayıtları var");
        JsonElement unchanged = await FindTermAsync(client, token, autumn);
        unchanged.GetProperty("name").GetString().ShouldBe("Güz");
        unchanged.GetProperty("endDate").GetString().ShouldBe("2027-01-22");

        var (edited, term, _) = await CallAsync(client, HttpMethod.Put, $"{Path}/{autumn}", token, new { name = "Güz dönemi", startDate = "2026-09-07", endDate = "2027-01-15" });
        edited.ShouldBe(HttpStatusCode.OK);
        term.GetProperty("name").GetString().ShouldBe("Güz dönemi");
        term.GetProperty("startDate").GetString().ShouldBe("2026-09-07");
    }

    [Fact]
    public async Task AdminList_NewestFirst_MarksUpcomingBetweenTerms_AndCurrentFromTurkeyMidnight()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2027, 1, 29, 21, 30, 0, TimeSpan.Zero));
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid past = await CreateTermAsync(client, token, "2025-2026", "2025-09-01", "2026-06-19");
        Guid autumn = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");
        Guid spring = await CreateTermAsync(client, token, "Bahar", "2027-02-08", "2027-06-18");

        var (_, list, _) = await CallAsync(client, HttpMethod.Get, Path, token);
        list.EnumerateArray().Select(t => t.GetProperty("id").GetGuid()).ShouldBe([spring, autumn, past]);
        Flags(list, spring).ShouldBe((false, true), "dönem arasında henüz başlamamış en yakın dönem yaklaşan");
        Flags(list, autumn).ShouldBe((false, false));
        Flags(list, past).ShouldBe((false, false));

        time.SetUtcNow(new DateTimeOffset(2027, 2, 7, 21, 0, 0, TimeSpan.Zero));
        token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        var (_, later, _) = await CallAsync(client, HttpMethod.Get, Path, token);
        Flags(later, spring).ShouldBe((true, false), "UTC 7 Şubat 21:00 = Türkiye 8 Şubat 00:00: başlangıç günü dahil güncel");
    }

    private static (bool Current, bool Upcoming) Flags(JsonElement list, Guid id)
    {
        JsonElement term = list.EnumerateArray().Single(t => t.GetProperty("id").GetGuid() == id);
        return (term.GetProperty("isCurrent").GetBoolean(), term.GetProperty("isUpcoming").GetBoolean());
    }

    private static async Task<JsonElement> FindTermAsync(HttpClient client, string token, Guid id)
    {
        var (_, list, _) = await CallAsync(client, HttpMethod.Get, Path, token);
        return list.EnumerateArray().Single(t => t.GetProperty("id").GetGuid() == id);
    }
}
