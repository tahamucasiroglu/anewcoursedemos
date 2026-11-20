using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;
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
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", secondToken, new { roles = new[] { Teacher(a.BranchId) } }))
            .ShouldBe(HttpStatusCode.OK, "ikinci yönetici varken ilkinin yönetici rolü alınabilir");
        (await StatusAsync(client, HttpMethod.Get, "/api/admin/members", token)).ShouldBe(HttpStatusCode.Forbidden, "rolü alınan artık yönetemez");
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", secondToken, new { roles = new[] { Founder() } }))
            .ShouldBe(HttpStatusCode.OK, "yönetici rolü geri verilebilir");
        var (lastOne, lastProblem, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/members/{secondId}/deactivate", token);
        lastOne.ShouldBe(HttpStatusCode.OK, "iki etkin yönetici varken biri pasifleştirilebilir");
        var (blocked, blockedProblem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", token, new { roles = new[] { Teacher(a.BranchId) } });
        blocked.ShouldBe(HttpStatusCode.BadRequest);
        Detail(blockedProblem).ShouldBe("Kendi yönetici rolünüzü alamazsınız");
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.TenantId == a.TenantId && r.Role == Role.Founder))).ShouldBe(2);
        _ = lastProblem;
    }

    [Fact]
    public async Task TwoFoundersRemovingEachOthersFounderRoleAtOnce_AtLeastOneStays()
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
            StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{secondId}/roles", first, new { roles = new[] { Teacher(a.BranchId) } }),
            StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{a.FounderId}/roles", second, new { roles = new[] { Teacher(a.BranchId) } }));

        results.Count(r => r == HttpStatusCode.OK).ShouldBe(1, "sıradaki ikinci istek, rolü alınmış kişiden geldiği için reddedilir");
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.TenantId == a.TenantId && r.Role == Role.Founder))).ShouldBe(1);
    }

    /// <summary>
    /// Review L-1: istek yetki denetiminden geçtikten sonra, sırasını beklerken kişinin yönetici rolü alınırsa işlem
    /// yapılmaz (sıra içinde yeniden denetim). Okulun sırası test tarafından tutulur; rol o sırada alınır.
    /// </summary>
    [Fact]
    public async Task RequestWaitingForLock_FromFounderDemotedMeanwhile_IsRejected()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;
        SchoolSeed a = await CreateSchoolAsync(factory);
        string first = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string email = UniqueEmail("mudur");
        var (_, added, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", first, NewMember(email, "Müdür", null, Founder()));
        Guid secondId = added.GetProperty("userId").GetGuid();
        string second = await TokenForSchoolAsync(client, email, a.TenantId, added.GetProperty("temporaryPassword").GetString()!);

        using IServiceScope holder = factory.Services.CreateScope();
        IUnitOfWork unitOfWork = holder.ServiceProvider.GetRequiredService<IUnitOfWork>();
        OkulDbContext context = holder.ServiceProvider.GetRequiredService<OkulDbContext>();
        IExclusiveScope held = await unitOfWork.BeginExclusiveAsync("tenant-admin:" + a.TenantId.ToString("N"), ct);
        Task<HttpStatusCode> waiting = StatusAsync(client, HttpMethod.Post, "/api/admin/branches", second, new { name = "Bekleyen Şube" });
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        waiting.IsCompleted.ShouldBeFalse("istek okulun sırasını beklemeli");
        context.RoleAssignments.RemoveRange(await context.RoleAssignments.IgnoreQueryFilters().Where(r => r.UserId == secondId && r.TenantId == a.TenantId).ToListAsync(ct));
        await context.SaveChangesAsync(ct);
        await held.CommitAsync(ct);
        await held.DisposeAsync();

        (await waiting).ShouldBe(HttpStatusCode.Forbidden);
        (await QueryAsync(factory, c => c.Branches.IgnoreQueryFilters().CountAsync(b => b.TenantId == a.TenantId && b.Name == "Bekleyen Şube"))).ShouldBe(0);
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
