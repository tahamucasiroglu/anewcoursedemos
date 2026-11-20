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
