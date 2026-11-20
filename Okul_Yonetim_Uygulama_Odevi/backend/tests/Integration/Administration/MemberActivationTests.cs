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

/// <summary>Spec 0005 AC-9: pasif kişi bu okula giremez, açık oturumu hemen reddedilir; öbür okul etkilenmez; hiç etkin okulu kalmayan kişi boş okul listesi alır; yeniden etkinleşince eski rolleriyle girer.</summary>
[Trait("Category", "Integration")]
public sealed class MemberActivationTests
{
    private readonly TestDatabase database;

    public MemberActivationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Deactivate_BlocksThisSchoolImmediately_KeepsOtherSchool_ReactivateRestoresRoles()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string person = UniqueEmail("iki-okul");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, person, b.TenantId, Role.Teacher, b.BranchId);
        string founderA = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string founderB = await TokenForSchoolAsync(client, b.FounderEmail, b.TenantId);
        string personInA = await TokenForSchoolAsync(client, person, a.TenantId);
        string personInB = await TokenForSchoolAsync(client, person, b.TenantId);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/deactivate", founderA)).ShouldBe(HttpStatusCode.OK);

        (await StatusAsync(client, HttpMethod.Get, "/api/me", personInA)).ShouldBe(HttpStatusCode.Unauthorized);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", personInB)).ShouldBe(HttpStatusCode.OK);
        JsonElement auth = await LoginAsync(client, person);
        auth.GetProperty("tenants").EnumerateArray().Select(t => t.GetProperty("id").GetGuid()).ShouldBe([b.TenantId]);
        (await StatusAsync(client, HttpMethod.Post, "/api/auth/select-tenant", AccessToken(auth), new { tenantId = a.TenantId })).ShouldNotBe(HttpStatusCode.OK);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/deactivate", founderB)).ShouldBe(HttpStatusCode.OK);
        (await LoginAsync(client, person)).GetProperty("tenants").GetArrayLength().ShouldBe(0);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/activate", founderA)).ShouldBe(HttpStatusCode.OK);
        string back = await TokenForSchoolAsync(client, person, a.TenantId);
        var (_, branches, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", back);
        branches.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([a.BranchId]);
    }

    [Fact]
    public async Task Refresh_ForSchoolWherePersonWasDeactivated_IsRejected()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("ogretmen");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        JsonElement auth = await LoginAsync(client, person);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);

        using HttpResponseMessage refreshed = await PostJsonAsync(client, "/api/auth/refresh", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        refreshed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
