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

/// <summary>Spec 0005 AC-8: rol/şube verme ve alma bir sonraki istekte geçerli; rolsüz kişi pasif ve okulu kullanamaz; yönetici rolü okul düzeyinde.</summary>
[Trait("Category", "Integration")]
public sealed class MemberRoleTests
{
    private readonly TestDatabase database;

    public MemberRoleTests(TestDatabase database)
    {
        this.database = database;
    }

    /// <summary>Review M-1: şube pasifleşince kişinin o şubedeki ataması rol düzenlemesini kilitlemez; kaldırılabilir, yeniden eklenemez.</summary>
    [Fact]
    public async Task InactiveBranchAssignment_KeepsRolesEditable_CanBeRemoved_CannotBeReAdded()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid moda = await CreateBranchAsync(client, token, "Moda");
        string teacher = UniqueEmail("ogretmen");
        Guid teacherId = await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, moda);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{moda}/deactivate", token)).ShouldBe(HttpStatusCode.OK);

        var (kept, card, raw) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{teacherId}/roles", token,
            new { roles = new object[] { Teacher(a.BranchId, moda), new { role = "Accounting", branchIds = new[] { a.BranchId } } } });
        kept.ShouldBe(HttpStatusCode.OK, raw);
        card.GetProperty("roles").GetArrayLength().ShouldBe(2);

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{teacherId}/roles", token, new { roles = new[] { Teacher(a.BranchId) } })).ShouldBe(HttpStatusCode.OK);
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.UserId == teacherId && r.BranchId == moda))).ShouldBe(0);

        var (readd, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{teacherId}/roles", token, new { roles = new[] { Teacher(a.BranchId, moda) } });
        readd.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Seçilen şube bulunamadı ya da pasif");
    }

    [Fact]
    public async Task RoleChanges_TakeEffectOnNextRequest_AndRemovingAllMakesInactive()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid second = await CreateBranchAsync(client, token, "Bakırköy");
        string teacher = UniqueEmail("ogretmen");
        Guid teacherId = await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);
        string teacherToken = await TokenForSchoolAsync(client, teacher, a.TenantId);

        var (set, card, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{teacherId}/roles", token, new { roles = new[] { Teacher(second) } });
        set.ShouldBe(HttpStatusCode.OK);
        card.GetProperty("roles").EnumerateArray().Single().GetProperty("branchIds").EnumerateArray().Select(x => x.GetGuid()).ShouldBe([second]);
        var (_, now, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", teacherToken);
        now.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([second]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/branches/{a.BranchId}", teacherToken)).ShouldBe(HttpStatusCode.NotFound);

        var (promoted, _, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{teacherId}/roles", token, new { roles = new[] { Founder() } });
        promoted.ShouldBe(HttpStatusCode.OK);
        var (_, all, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", teacherToken);
        all.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([a.BranchId, second], ignoreOrder: true);

        var (cleared, inactive, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{teacherId}/roles", token, new { roles = Array.Empty<object>() });
        cleared.ShouldBe(HttpStatusCode.OK);
        inactive.GetProperty("isActive").GetBoolean().ShouldBeFalse();
        (await StatusAsync(client, HttpMethod.Get, "/api/me", teacherToken)).ShouldBe(HttpStatusCode.Unauthorized);
        JsonElement auth = await LoginAsync(client, teacher);
        auth.GetProperty("tenants").GetArrayLength().ShouldBe(0);
    }
}
