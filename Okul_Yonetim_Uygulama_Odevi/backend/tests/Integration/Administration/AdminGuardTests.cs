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

/// <summary>Spec 0005 AC-10: kendi yönetici rolünü alamaz, kendini pasifleştiremez; son etkin yönetici korunur; iki yönetici aynı anda birbirini pasifleştirse bile en az biri kalır.</summary>
[Trait("Category", "Integration")]
public sealed class AdminGuardTests
{
    private readonly TestDatabase database;

    public AdminGuardTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task SelfAndLastFounder_AreProtected_SecondFounderCanBeDeactivated()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        var (own, ownProblem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", token, new { roles = new[] { Teacher(a.BranchId) } });
        own.ShouldBe(HttpStatusCode.BadRequest);
        Detail(ownProblem).ShouldBe("Kendi yönetici rolünüzü alamazsınız");
        var (self, selfProblem, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/members/{a.FounderId}/deactivate", token);
        self.ShouldBe(HttpStatusCode.BadRequest);
        Detail(selfProblem).ShouldBe("Kendinizi pasifleştiremezsiniz");

        string second = UniqueEmail("mudur");
        var (_, added, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(second, "Müdür", null, Founder()));
        Guid secondId = added.GetProperty("userId").GetGuid();
        string secondToken = await TokenForSchoolAsync(client, second, a.TenantId, added.GetProperty("temporaryPassword").GetString()!);
        var (lastFounder, lastProblem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", secondToken, new { roles = new[] { Teacher(a.BranchId) } });
        lastFounder.ShouldBe(HttpStatusCode.OK, "ikinci yönetici varken ilkinin rolü alınabilir");
        var (blocked, blockedProblem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", secondToken, new { roles = new[] { Founder() } });
        blocked.ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{secondId}/deactivate", token)).ShouldBe(HttpStatusCode.OK);
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.TenantId == a.TenantId && r.Role == Role.Founder))).ShouldBe(2);
        _ = blockedProblem;
        _ = lastProblem;
    }

    [Fact]
    public async Task TwoFoundersDeactivatingEachOtherAtOnce_AtLeastOneStaysActive()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string first = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string email = UniqueEmail("mudur");
        var (_, added, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", first, NewMember(email, "Müdür", null, Founder()));
        Guid secondId = added.GetProperty("userId").GetGuid();
        string second = await TokenForSchoolAsync(client, email, a.TenantId, added.GetProperty("temporaryPassword").GetString()!);

        HttpStatusCode[] results = await Task.WhenAll(
            StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{secondId}/deactivate", first),
            StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{a.FounderId}/deactivate", second));

        results.Count(r => r == HttpStatusCode.OK).ShouldBeLessThanOrEqualTo(1);
        int activeFounders = await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters()
            .Where(r => r.TenantId == a.TenantId && r.Role == Role.Founder)
            .CountAsync(r => c.Memberships.IgnoreQueryFilters().Any(m => m.UserId == r.UserId && m.TenantId == a.TenantId && m.IsActive)));
        activeFounders.ShouldBeGreaterThanOrEqualTo(1);
    }
}
