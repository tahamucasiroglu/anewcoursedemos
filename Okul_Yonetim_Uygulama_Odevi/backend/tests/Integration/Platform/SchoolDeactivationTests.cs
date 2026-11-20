using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Platform.PlatformTestData;

namespace OkulYonetim.Tests.Integration.Platform;

/// <summary>Spec 0007 AC-9: okulu pasifleştir / etkinleştir — kimse giremez, açık oturum ve yenileme reddedilir, veri durur.</summary>
[Trait("Category", "Integration")]
public sealed class SchoolDeactivationTests
{
    private readonly TestDatabase database;

    public SchoolDeactivationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Deactivate_LocksEveryoneOut_OtherSchoolsUnaffected_ActivateRestores()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.77");
        (SchoolSeed admin, string platform, _) = await SignedInPlatformAdminAsync(factory, client, time);
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory, a.FounderEmail);
        string teacher = UniqueEmail("ogretmen");
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);
        JsonElement founderAuth = await LoginAsync(client, a.FounderEmail);
        using HttpResponseMessage selected = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/select-tenant", AccessToken(founderAuth), new { tenantId = a.TenantId });
        JsonElement founderInA = await ReadJsonAsync(selected);
        string teacherToken = AccessToken(await LoginAsync(client, teacher));
        int branches = await QueryAsync(factory, c => c.Branches.IgnoreQueryFilters().CountAsync(x => x.TenantId == a.TenantId));

        var deactivated = await CallAsync(client, HttpMethod.Post, $"{Schools}/{a.TenantId}/deactivate", platform);

        deactivated.Status.ShouldBe(HttpStatusCode.OK, deactivated.Raw);
        deactivated.Body.GetProperty("isActive").GetBoolean().ShouldBeFalse();
        (await StatusAsync(client, HttpMethod.Get, "/api/me", AccessToken(founderInA))).ShouldBe(HttpStatusCode.Unauthorized);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", teacherToken)).ShouldBe(HttpStatusCode.Unauthorized);
        (await PostAsync(client, "/api/auth/refresh", new { refreshToken = founderInA.GetProperty("refreshToken").GetString() })).Status.ShouldBe(HttpStatusCode.Unauthorized);
        JsonElement relogin = await LoginAsync(client, a.FounderEmail);
        relogin.GetProperty("tenants").EnumerateArray().Select(t => t.GetProperty("id").GetGuid()).ShouldBe([b.TenantId]);
        (await CallAsync(client, HttpMethod.Get, "/api/me", AccessToken(relogin))).Body.GetProperty("tenant").GetProperty("id").GetGuid().ShouldBe(b.TenantId, "başka okulu etkilenmez");
        (await StatusAsync(client, HttpMethod.Post, "/api/auth/select-tenant", AccessToken(relogin), new { tenantId = a.TenantId })).ShouldNotBe(HttpStatusCode.OK);
        JsonElement teacherLogin = await LoginAsync(client, teacher);
        teacherLogin.GetProperty("tenants").GetArrayLength().ShouldBe(0);
        (await QueryAsync(factory, c => c.Branches.IgnoreQueryFilters().CountAsync(x => x.TenantId == a.TenantId))).ShouldBe(branches, "veri silinmez");

        var activated = await CallAsync(client, HttpMethod.Post, $"{Schools}/{a.TenantId}/activate", platform);
        activated.Body.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        JsonElement back = (await CallAsync(client, HttpMethod.Get, "/api/me", await TokenForSchoolAsync(client, teacher, a.TenantId))).Body;
        back.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Teacher"]);

        List<AuditEntry> audit = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.TargetId == a.TenantId && (e.Action == AuditAction.TenantDeactivated || e.Action == AuditAction.TenantActivated)).ToListAsync());
        audit.Count.ShouldBe(2);
        audit.ShouldAllBe(e => e.ActorUserId == admin.FounderId && e.IpAddress == "203.0.113.77" && e.TenantId == a.TenantId);
    }

    [Fact]
    public async Task UnknownSchool_Is404()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (_, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);

        (await StatusAsync(client, HttpMethod.Post, $"{Schools}/{Guid.NewGuid()}/deactivate", token)).ShouldBe(HttpStatusCode.NotFound);
    }
}
