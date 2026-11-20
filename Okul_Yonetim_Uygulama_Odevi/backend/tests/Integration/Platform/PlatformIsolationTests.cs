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

/// <summary>Spec 0007 AC-10: platform yöneticisi rolü olmayan okulun içine giremez; panel yalnızca özet döndürür.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformIsolationTests
{
    private readonly TestDatabase database;

    public PlatformIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task PlatformAdmin_CannotReachInsideOtherSchool()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        SchoolSeed other = await CreateSchoolAsync(factory);
        string teacher = UniqueEmail("ogretmen");
        Guid teacherId = await AddMemberAsync(factory, teacher, other.TenantId, Role.Teacher, other.BranchId);

        (await StatusAsync(client, HttpMethod.Post, "/api/auth/select-tenant", token, new { tenantId = other.TenantId })).ShouldNotBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Get, $"/api/admin/members/{teacherId}", token)).ShouldBe(HttpStatusCode.NotFound);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{other.FounderId}/reset-password", token)).ShouldBe(HttpStatusCode.NotFound);
        var members = await CallAsync(client, HttpMethod.Get, "/api/admin/members", token);
        members.Raw.ShouldNotContain(teacher);
        members.Raw.ShouldNotContain(other.FounderEmail);
        var branches = await CallAsync(client, HttpMethod.Get, "/api/admin/branches", token);
        branches.Raw.ShouldNotContain(other.BranchId.ToString());
        admin.TenantId.ShouldNotBe(other.TenantId);
    }
}
