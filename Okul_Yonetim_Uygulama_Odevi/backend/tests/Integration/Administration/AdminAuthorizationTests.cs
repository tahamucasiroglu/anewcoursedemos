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

/// <summary>Spec 0005 AC-1: yönetim yalnızca seçili okulun kurucusuna; öğretmen/veli/muhasebe 403 ve değişiklik yok; girişsiz 401.</summary>
[Trait("Category", "Integration")]
public sealed class AdminAuthorizationTests
{
    private readonly TestDatabase database;

    public AdminAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData(Role.Teacher)]
    [InlineData(Role.Guardian)]
    [InlineData(Role.Accounting)]
    public async Task NonFounder_GetsForbidden_AndNothingChanges(Role role)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        string email = UniqueEmail("rol");
        await AddMemberAsync(factory, email, school.TenantId, role, school.BranchId);
        string token = await TokenForSchoolAsync(client, email, school.TenantId);
        int branchesBefore = await QueryAsync(factory, c => c.Branches.IgnoreQueryFilters().CountAsync(b => b.TenantId == school.TenantId));

        (await StatusAsync(client, HttpMethod.Get, "/api/admin/members", token)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Get, "/api/admin/branches", token)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/branches", token, new { name = "Gizli Şube" })).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(UniqueEmail(), roles: Teacher(school.BranchId)))).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{school.FounderId}/reset-password", token)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{school.FounderId}/deactivate", token)).ShouldBe(HttpStatusCode.Forbidden);

        (await QueryAsync(factory, c => c.Branches.IgnoreQueryFilters().CountAsync(b => b.TenantId == school.TenantId))).ShouldBe(branchesBefore);
        (await QueryAsync(factory, c => c.Memberships.IgnoreQueryFilters().CountAsync(m => m.TenantId == school.TenantId && m.IsActive))).ShouldBe(2);
    }

    [Fact]
    public async Task Anonymous_IsRejectedWith401()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage list = await client.GetAsync(new Uri("/api/admin/members", UriKind.Relative), TestContext.Current.CancellationToken);
        using HttpResponseMessage create = await PostJsonAsync(client, "/api/admin/branches", new { name = "X" });

        list.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        create.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
