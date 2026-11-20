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
/// Spec 0008 AC-2 (okul duvarı): A'nın kurucusu B'nin seviyesini, dönemini, sınıfını ve takvim kaydını kimliğini bilerek
/// bile görüntüleyemez/değiştiremez (404); A'nın listeleri ve okul takvimi yalnızca A'nın kayıtlarını içerir; A'da B'nin
/// şubesiyle ya da seviyesiyle sınıf açılamaz; B'nin kayıtları değişmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicTenantIsolationTests
{
    private readonly TestDatabase database;

    public AcademicTenantIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FounderOfA_GetsNotFoundForEveryRecordOfB_ListsOnlyA_CannotUseBsBranchOrLevel()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)));
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string tokenA = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string tokenB = await TokenForSchoolAsync(client, b.FounderEmail, b.TenantId);
        Guid levelA = await CreateGradeLevelAsync(client, tokenA, "A seviyesi", 1);
        Guid termA = await CreateTermAsync(client, tokenA, "A dönemi", "2026-09-01", "2027-01-22");
        Guid entryA = await CreateCalendarEntryAsync(client, tokenA, termA, "A tatili", "Holiday", "2026-10-29", "2026-10-29");
        Guid classA = await CreateClassroomAsync(client, tokenA, termA, a.BranchId, levelA, "A-1");
        Guid levelB = await CreateGradeLevelAsync(client, tokenB, "B seviyesi", 1);
        Guid termB = await CreateTermAsync(client, tokenB, "B dönemi", "2026-09-01", "2027-01-22");
        Guid entryB = await CreateCalendarEntryAsync(client, tokenB, termB, "B tatili", "Holiday", "2026-10-29", "2026-10-29");
        Guid classB = await CreateClassroomAsync(client, tokenB, termB, b.BranchId, levelB, "B-1");

        (HttpMethod, string, object?)[] calls =
        [
            (HttpMethod.Put, $"/api/admin/grade-levels/{levelB}", new { name = "Ele geçti", sortOrder = 9 }),
            (HttpMethod.Post, $"/api/admin/grade-levels/{levelB}/deactivate", null),
            (HttpMethod.Post, $"/api/admin/grade-levels/{levelB}/activate", null),
            (HttpMethod.Put, $"/api/admin/terms/{termB}", new { name = "Ele geçti", startDate = "2026-09-01", endDate = "2027-01-22" }),
            (HttpMethod.Get, $"/api/admin/terms/{termB}/calendar-entries", null),
            (HttpMethod.Post, $"/api/admin/terms/{termB}/calendar-entries", new { title = "Sızma", kind = "Event", startDate = "2026-10-01", endDate = "2026-10-01" }),
            (HttpMethod.Put, $"/api/admin/calendar-entries/{entryB}", new { title = "Ele geçti", kind = "Event", startDate = "2026-10-01", endDate = "2026-10-01" }),
            (HttpMethod.Delete, $"/api/admin/calendar-entries/{entryB}", null),
            (HttpMethod.Get, $"/api/admin/classrooms?termId={termB}", null),
            (HttpMethod.Put, $"/api/admin/classrooms/{classB}", new { gradeLevelId = levelB, name = "Ele geçti" }),
            (HttpMethod.Post, $"/api/admin/classrooms/{classB}/deactivate", null),
            (HttpMethod.Post, $"/api/admin/classrooms/{classB}/activate", null),
            (HttpMethod.Get, $"/api/classrooms/{classB}", null),
            (HttpMethod.Get, $"/api/classrooms?termId={termB}", null),
            (HttpMethod.Post, "/api/admin/classrooms", new { termId = termB, branchId = a.BranchId, gradeLevelId = levelA, name = "Sızma" }),
        ];
        foreach ((HttpMethod method, string path, object? body) in calls)
        {
            (await StatusAsync(client, method, path, tokenA, body)).ShouldBe(HttpStatusCode.NotFound, $"{method} {path}");
        }

        var (withBranch, branchProblem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/classrooms", tokenA, new { termId = termA, branchId = b.BranchId, gradeLevelId = levelA, name = "Sızma" });
        withBranch.ShouldBe(HttpStatusCode.BadRequest);
        Detail(branchProblem).ShouldBe("Seçilen şube bulunamadı ya da pasif");
        var (withLevel, levelProblem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/classrooms", tokenA, new { termId = termA, branchId = a.BranchId, gradeLevelId = levelB, name = "Sızma" });
        withLevel.ShouldBe(HttpStatusCode.BadRequest);
        Detail(levelProblem).ShouldBe("Seçilen sınıf seviyesi bulunamadı ya da pasif");

        Ids((await CallAsync(client, HttpMethod.Get, "/api/admin/grade-levels", tokenA)).Body).ShouldBe([levelA]);
        Ids((await CallAsync(client, HttpMethod.Get, "/api/admin/terms", tokenA)).Body).ShouldBe([termA]);
        Ids((await CallAsync(client, HttpMethod.Get, "/api/admin/classrooms", tokenA)).Body.GetProperty("items")).ShouldBe([classA]);
        Ids((await CallAsync(client, HttpMethod.Get, "/api/classrooms", tokenA)).Body).ShouldBe([classA]);
        JsonElement calendar = (await CallAsync(client, HttpMethod.Get, "/api/calendar", tokenA)).Body;
        calendar.GetProperty("term").GetProperty("id").GetGuid().ShouldBe(termA);
        Ids(calendar.GetProperty("entries")).ShouldBe([entryA]);

        (await QueryAsync(factory, c => c.GradeLevels.IgnoreQueryFilters().SingleAsync(g => g.Id == levelB))).Name.ShouldBe("B seviyesi");
        (await QueryAsync(factory, c => c.Terms.IgnoreQueryFilters().SingleAsync(t => t.Id == termB))).Name.ShouldBe("B dönemi");
        (await QueryAsync(factory, c => c.Classrooms.IgnoreQueryFilters().SingleAsync(x => x.Id == classB))).Name.ShouldBe("B-1");
        (await QueryAsync(factory, c => c.CalendarEntries.IgnoreQueryFilters().SingleAsync(e => e.Id == entryB))).DeletedAt.ShouldBeNull();
        (await QueryAsync(factory, c => c.Classrooms.IgnoreQueryFilters().CountAsync(x => x.TenantId == a.TenantId))).ShouldBe(1);
        (await QueryAsync(factory, c => c.CalendarEntries.IgnoreQueryFilters().CountAsync(e => e.TermId == termB))).ShouldBe(1);
    }

    private static Guid[] Ids(JsonElement list) => [.. list.EnumerateArray().Select(x => x.GetProperty("id").GetGuid())];
}
