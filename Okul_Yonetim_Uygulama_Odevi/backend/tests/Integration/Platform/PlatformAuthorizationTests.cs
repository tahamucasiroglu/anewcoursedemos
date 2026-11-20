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

/// <summary>Spec 0007 AC-1: panel yalnızca platform yöneticisi + kodla açılmış oturuma; diğerleri 403, girişsiz 401.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformAuthorizationTests
{
    private readonly TestDatabase database;

    public PlatformAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Teacher)]
    [InlineData(Role.Guardian)]
    [InlineData(Role.Accounting)]
    public async Task SchoolRoles_GetForbidden_AndNothingChanges(Role? role)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        string email = school.FounderEmail;
        if (role is { } r)
        {
            email = UniqueEmail("rol");
            await AddMemberAsync(factory, email, school.TenantId, r, school.BranchId);
        }

        string token = await TokenForSchoolAsync(client, email, school.TenantId);
        string name = $"Yetkisiz Okul {Guid.NewGuid():N}";

        (await StatusAsync(client, HttpMethod.Get, Schools, token)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, Schools, token, new { schoolName = name, branchName = "Merkez", founderName = "X Y", founderEmail = UniqueEmail() })).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, $"{Schools}/{school.TenantId}/deactivate", token)).ShouldBe(HttpStatusCode.Forbidden);

        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().CountAsync(t => t.Name == name))).ShouldBe(0);
        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().SingleAsync(t => t.Id == school.TenantId))).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Anonymous_Gets401()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage list = await client.GetAsync(new Uri(Schools, UriKind.Relative), TestContext.Current.CancellationToken);
        list.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await PostAsync(client, Schools, new { schoolName = "X" })).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SessionWithoutCode_GetsForbidden()
    {
        // Yetki, açık bir oturum varken (komut dışı yolla) verilmiş olsa bile kodsuz oturum paneli açamaz.
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        string token = AccessToken(await LoginAsync(client, school.FounderEmail));
        await WithContextAsync(factory, async c =>
        {
            User user = await c.Users.SingleAsync(u => u.Id == school.FounderId);
            user.GrantPlatformAdmin();
            await c.SaveChangesAsync();
        });

        (await StatusAsync(client, HttpMethod.Get, Schools, token)).ShouldBe(HttpStatusCode.Forbidden);
        (await CallAsync(client, HttpMethod.Get, "/api/me", token)).Body.GetProperty("isPlatformAdmin").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task VerifiedPlatformAdmin_IsAllowed_AndRevokedAdminIsRejectedOnNextRequest()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);

        (await StatusAsync(client, HttpMethod.Get, Schools, token)).ShouldBe(HttpStatusCode.OK);
        (await CallAsync(client, HttpMethod.Get, "/api/me", token)).Body.GetProperty("isPlatformAdmin").GetBoolean().ShouldBeTrue();

        (await CliAsync(factory, "revoke-platform-admin", "--eposta", admin.FounderEmail)).Exit.ShouldBe(0);
        (await StatusAsync(client, HttpMethod.Get, Schools, token)).ShouldNotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokedFlag_WithoutSessionRevocation_StillForbidden()
    {
        // Yetki her istekte veritabanından okunur: oturum açık kalsa bile yetki düşünce panel kapanır.
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        await WithContextAsync(factory, async c =>
        {
            User user = await c.Users.SingleAsync(u => u.Id == admin.FounderId);
            user.RevokePlatformAdmin();
            await c.SaveChangesAsync();
        });

        (await StatusAsync(client, HttpMethod.Get, Schools, token)).ShouldBe(HttpStatusCode.Forbidden);
    }
}
