using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>Spec 0005 AC-3, AC-4: şube ekleme/düzenleme (ad kuralları, aynı ad), pasifleştirme (atamada seçilemez, kişiden düşer, son etkin şube korunur).</summary>
[Trait("Category", "Integration")]
public sealed class BranchAdminTests
{
    private readonly TestDatabase database;

    public BranchAdminTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_Update_ValidatesNameAndRejectsDuplicateInSameSchoolOnly()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string tokenA = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string tokenB = await TokenForSchoolAsync(client, b.FounderEmail, b.TenantId);

        (await StatusAsync(client, HttpMethod.Post, "/api/admin/branches", tokenA, new { name = "  " })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/branches", tokenA, new { name = new string('x', 101) })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/branches", tokenA, new { name = "Kadıköy", phone = "123" })).ShouldBe(HttpStatusCode.BadRequest);
        var (created, body, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/branches", tokenA, new { name = " Kadıköy ", address = "Moda Cd. 1", phone = "0216 555 44 33" });
        created.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("name").GetString().ShouldBe("Kadıköy");
        body.GetProperty("phone").GetString().ShouldBe("+902165554433");
        Guid kadikoy = body.GetProperty("id").GetGuid();

        var (dup, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/branches", tokenA, new { name = "KADIKÖY" });
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu adla bir şube zaten var");
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/branches/{a.BranchId}", tokenA, new { name = "kadıköy" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/branches", tokenB, new { name = "Kadıköy" })).ShouldBe(HttpStatusCode.OK);

        var (updated, renamed, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/branches/{kadikoy}", tokenA, new { name = "Kadıköy Moda", address = (string?)null, phone = (string?)null });
        updated.ShouldBe(HttpStatusCode.OK);
        renamed.GetProperty("name").GetString().ShouldBe("Kadıköy Moda");
        var (_, list, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", tokenA);
        list.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ShouldContain("Kadıköy Moda");
        string teacher = UniqueEmail("ogretmen");
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/members", tokenA, NewMember(teacher, roles: Teacher(kadikoy)))).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivate_HidesFromAssignedPerson_BlocksNewAssignment_KeepsLastActive()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid second = await CreateBranchAsync(client, token, "İkinci Şube");
        string teacher = UniqueEmail("ogretmen");
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, second);
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);
        string teacherToken = await TokenForSchoolAsync(client, teacher, a.TenantId);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{second}/deactivate", token)).ShouldBe(HttpStatusCode.OK);

        var (_, visible, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", teacherToken);
        visible.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([a.BranchId]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/branches/{second}", teacherToken)).ShouldBe(HttpStatusCode.NotFound);
        var (bad, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(UniqueEmail(), roles: Teacher(second)));
        bad.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Seçilen şube bulunamadı ya da pasif");
        var (_, adminList, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/branches", token);
        adminList.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == second).GetProperty("isActive").GetBoolean().ShouldBeFalse();

        var (last, lastProblem, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/branches/{a.BranchId}/deactivate", token);
        last.ShouldBe(HttpStatusCode.BadRequest);
        Detail(lastProblem).ShouldBe("Okulun son etkin şubesi pasifleştirilemez");

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{second}/activate", token)).ShouldBe(HttpStatusCode.OK);
        var (_, again, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", teacherToken);
        again.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldContain(second);
    }
}
