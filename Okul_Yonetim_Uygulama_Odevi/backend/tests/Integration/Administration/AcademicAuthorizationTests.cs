using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Spec 0008 AC-1: seviye, dönem, sınıf ve takvim yönetimi yalnızca kurucuya; öğretmen/veli/muhasebe her işlemde 403 ve
/// hiçbir şey değişmez; girişsiz 401. Okul takvimi okulda rolü olan her role açık; okul seçilmemiş oturum alamaz (403).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicAuthorizationTests
{
    private readonly TestDatabase database;

    public AcademicAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData(Role.Teacher)]
    [InlineData(Role.Guardian)]
    [InlineData(Role.Accounting)]
    public async Task NonFounder_GetsForbiddenOnEveryAdminCall_NothingChanges_ButSeesCalendar(Role role)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        string founder = await TokenForSchoolAsync(client, school.FounderEmail, school.TenantId);
        Guid level = await CreateGradeLevelAsync(client, founder, "Hazırlık", 1);
        Guid term = await CreateTermAsync(client, founder, "Güz", "2026-09-01", "2027-01-22");
        Guid classroom = await CreateClassroomAsync(client, founder, term, school.BranchId, level, "H-A");
        Guid entry = await CreateCalendarEntryAsync(client, founder, term, "Tatil", "Holiday", "2026-10-29", "2026-10-29");
        string email = UniqueEmail("rol");
        await AddMemberAsync(factory, email, school.TenantId, role, school.BranchId);
        string token = await TokenForSchoolAsync(client, email, school.TenantId);

        foreach ((HttpMethod method, string path, object? body) in Calls(level, term, classroom, entry, school.BranchId))
        {
            (await StatusAsync(client, method, path, token, body)).ShouldBe(HttpStatusCode.Forbidden, $"{method} {path}");
        }

        (await QueryAsync(factory, c => c.GradeLevels.IgnoreQueryFilters().Where(g => g.TenantId == school.TenantId).Select(g => g.Name + g.IsActive).ToListAsync())).ShouldBe(["Hazırlıktrue"]);
        (await QueryAsync(factory, c => c.Terms.IgnoreQueryFilters().Where(t => t.TenantId == school.TenantId).Select(t => t.Name).ToListAsync())).ShouldBe(["Güz"]);
        (await QueryAsync(factory, c => c.Classrooms.IgnoreQueryFilters().Where(x => x.TenantId == school.TenantId).Select(x => x.Name + x.IsActive).ToListAsync())).ShouldBe(["H-Atrue"]);
        (await QueryAsync(factory, c => c.CalendarEntries.IgnoreQueryFilters().Where(e => e.TenantId == school.TenantId).Select(e => e.Title + (e.DeletedAt == null)).ToListAsync())).ShouldBe(["Tatiltrue"]);

        (await StatusAsync(client, HttpMethod.Get, "/api/calendar", token)).ShouldBe(HttpStatusCode.OK, "okul takvimi her role açık");
    }

    [Fact]
    public async Task Founder_SeesCalendar_Anonymous401_NoSelectedSchool403()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        (await StatusAsync(client, HttpMethod.Get, "/api/calendar", founder)).ShouldBe(HttpStatusCode.OK);

        foreach ((HttpMethod method, string path, object? body) in Calls(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), a.BranchId).Append((HttpMethod.Get, "/api/calendar", null)).Append((HttpMethod.Get, "/api/classrooms", null)))
        {
            using HttpRequestMessage request = new(method, new Uri(path, UriKind.Relative));
            if (body is not null)
            {
                request.Content = System.Net.Http.Json.JsonContent.Create(body);
            }

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{method} {path}");
        }

        string person = UniqueEmail("iki-okul");
        await AddMemberAsync(factory, person, a.TenantId, Role.Guardian, a.BranchId);
        await AddMemberAsync(factory, person, b.TenantId, Role.Guardian, b.BranchId);
        JsonElement auth = await LoginAsync(client, person);
        auth.GetProperty("requiresTenantSelection").GetBoolean().ShouldBeTrue();
        (await StatusAsync(client, HttpMethod.Get, "/api/calendar", AccessToken(auth))).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Get, "/api/classrooms", AccessToken(auth))).ShouldBe(HttpStatusCode.Forbidden);
    }

    private static IEnumerable<(HttpMethod Method, string Path, object? Body)> Calls(Guid level, Guid term, Guid classroom, Guid entry, Guid branch) =>
    [
        (HttpMethod.Get, "/api/admin/grade-levels", null),
        (HttpMethod.Post, "/api/admin/grade-levels", new { name = "Gizli", sortOrder = 2 }),
        (HttpMethod.Put, $"/api/admin/grade-levels/{level}", new { name = "Değişti", sortOrder = 3 }),
        (HttpMethod.Post, $"/api/admin/grade-levels/{level}/deactivate", null),
        (HttpMethod.Post, $"/api/admin/grade-levels/{level}/activate", null),
        (HttpMethod.Get, "/api/admin/terms", null),
        (HttpMethod.Post, "/api/admin/terms", new { name = "Gizli", startDate = "2027-02-08", endDate = "2027-06-18" }),
        (HttpMethod.Put, $"/api/admin/terms/{term}", new { name = "Değişti", startDate = "2026-09-01", endDate = "2027-01-22" }),
        (HttpMethod.Get, $"/api/admin/terms/{term}/calendar-entries", null),
        (HttpMethod.Post, $"/api/admin/terms/{term}/calendar-entries", new { title = "Gizli", kind = "Event", startDate = "2026-10-01", endDate = "2026-10-01" }),
        (HttpMethod.Put, $"/api/admin/calendar-entries/{entry}", new { title = "Değişti", kind = "Exam", startDate = "2026-10-01", endDate = "2026-10-01" }),
        (HttpMethod.Delete, $"/api/admin/calendar-entries/{entry}", null),
        (HttpMethod.Get, "/api/admin/classrooms", null),
        (HttpMethod.Post, "/api/admin/classrooms", new { termId = term, branchId = branch, gradeLevelId = level, name = "Gizli" }),
        (HttpMethod.Put, $"/api/admin/classrooms/{classroom}", new { gradeLevelId = level, name = "Değişti" }),
        (HttpMethod.Post, $"/api/admin/classrooms/{classroom}/deactivate", null),
        (HttpMethod.Post, $"/api/admin/classrooms/{classroom}/activate", null),
    ];
}
