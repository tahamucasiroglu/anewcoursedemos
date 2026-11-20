using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Spec 0008 AC-3: seviye ekleme/düzenleme (ad ≤ 50, sıra 1–99), sıra numarasına göre liste, aynı okulda aynı ad (Türkçe
/// harf kuralıyla) reddi, başka okulda serbest; pasif seviye yeni sınıfta seçilemez, mevcut sınıf değişmez, yeniden
/// etkinleştirmede ad çakışması denetlenir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GradeLevelAdminTests
{
    private readonly TestDatabase database;

    public GradeLevelAdminTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_Update_ValidatesFields_OrdersBySortOrder_RejectsDuplicateInSameSchoolOnly()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string tokenB = await TokenForSchoolAsync(client, b.FounderEmail, b.TenantId);
        const string path = "/api/admin/grade-levels";

        (await StatusAsync(client, HttpMethod.Post, path, token, new { name = " ", sortOrder = 1 })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, path, token, new { name = new string('x', 51), sortOrder = 1 })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, path, token, new { name = "Sıfır", sortOrder = 0 })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, path, token, new { name = "Yüz", sortOrder = 100 })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, path, token, new { name = "Sırasız" })).ShouldBe(HttpStatusCode.BadRequest);
        (await QueryAsync(factory, c => c.GradeLevels.IgnoreQueryFilters().CountAsync(g => g.TenantId == a.TenantId))).ShouldBe(0);

        var (created, body, _) = await CallAsync(client, HttpMethod.Post, path, token, new { name = " Hazırlık ", sortOrder = 1 });
        created.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("name").GetString().ShouldBe("Hazırlık");
        body.GetProperty("sortOrder").GetInt32().ShouldBe(1);
        body.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        (await StatusAsync(client, HttpMethod.Post, path, token, new { name = new string('x', 50), sortOrder = 99 })).ShouldBe(HttpStatusCode.OK);

        var (dup, problem, _) = await CallAsync(client, HttpMethod.Post, path, token, new { name = "HAZIRLIK", sortOrder = 2 });
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu adla bir seviye zaten var");
        (await StatusAsync(client, HttpMethod.Post, path, tokenB, new { name = "Hazırlık", sortOrder = 1 })).ShouldBe(HttpStatusCode.OK, "başka okulda aynı ad serbest");

        Guid fifth = await CreateGradeLevelAsync(client, token, "5. sınıf", 5);
        await CreateGradeLevelAsync(client, token, "4 yaş grubu", 1);
        var (_, list, _) = await CallAsync(client, HttpMethod.Get, path, token);
        Names(list).ShouldBe(["4 yaş grubu", "Hazırlık", "5. sınıf", new string('x', 50)]);

        var (updated, renamed, _) = await CallAsync(client, HttpMethod.Put, $"{path}/{fifth}", token, new { name = "5. Sınıf", sortOrder = 2 });
        updated.ShouldBe(HttpStatusCode.OK);
        renamed.GetProperty("name").GetString().ShouldBe("5. Sınıf");
        var (_, after, _) = await CallAsync(client, HttpMethod.Get, path, token);
        Names(after).ShouldBe(["4 yaş grubu", "Hazırlık", "5. Sınıf", new string('x', 50)]);
        var (clash, clashProblem, _) = await CallAsync(client, HttpMethod.Put, $"{path}/{fifth}", token, new { name = "hazırlık", sortOrder = 2 });
        clash.ShouldBe(HttpStatusCode.BadRequest);
        Detail(clashProblem).ShouldBe("Bu adla bir seviye zaten var");
        (await StatusAsync(client, HttpMethod.Put, $"{path}/{fifth}", token, new { name = "5. Sınıf", sortOrder = 100 })).ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deactivate_BlocksNewClassroom_KeepsExistingClassroom_ReactivationRechecksName()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid level = await CreateGradeLevelAsync(client, token, "Hazırlık", 1);
        Guid term = await CreateTermAsync(client, token, "2026-2027 Güz", "2026-09-01", "2027-01-22");
        Guid existing = await CreateClassroomAsync(client, token, term, a.BranchId, level, "H-A");

        var (deactivated, body, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/grade-levels/{level}/deactivate", token);
        deactivated.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("isActive").GetBoolean().ShouldBeFalse();

        var (blocked, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/classrooms", token, new { termId = term, branchId = a.BranchId, gradeLevelId = level, name = "H-B" });
        blocked.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Seçilen sınıf seviyesi bulunamadı ya da pasif");
        var (_, classes, _) = await CallAsync(client, HttpMethod.Get, $"/api/admin/classrooms?termId={term}", token);
        JsonElement kept = classes.GetProperty("items").EnumerateArray().Single();
        kept.GetProperty("id").GetGuid().ShouldBe(existing);
        kept.GetProperty("gradeLevelId").GetGuid().ShouldBe(level);
        kept.GetProperty("isActive").GetBoolean().ShouldBeTrue("seviyeyi kullanan mevcut sınıf değişmez");

        Guid twin = await CreateGradeLevelAsync(client, token, "HAZIRLIK", 1);
        var (again, againProblem, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/grade-levels/{level}/activate", token);
        again.ShouldBe(HttpStatusCode.BadRequest);
        Detail(againProblem).ShouldBe("Bu adla bir seviye zaten var");
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/grade-levels/{twin}", token, new { name = "Hazırlık 2", sortOrder = 1 })).ShouldBe(HttpStatusCode.OK);
        var (activated, active, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/grade-levels/{level}/activate", token);
        activated.ShouldBe(HttpStatusCode.OK);
        active.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/classrooms", token, new { termId = term, branchId = a.BranchId, gradeLevelId = level, name = "H-B" })).ShouldBe(HttpStatusCode.OK);
    }

    private static string?[] Names(JsonElement list) => [.. list.EnumerateArray().Select(x => x.GetProperty("name").GetString())];
}
