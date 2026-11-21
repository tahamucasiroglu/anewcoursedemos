using System.Net;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-1 (yetki; S1/S6): öğretmen, veli ve muhasebe panelin üç kartında, üç raporunda ve üç dışa aktarmasında 403 alır ve dışa
/// aktarma audit'i oluşmaz; girişsiz istek 401; kurucu her uca erişir; kurucu rolü alınan kişinin bir sonraki isteği reddedilir (rol her
/// istekte veritabanından).
/// </summary>
[Trait("Category", "Integration")]
public sealed class DashboardAuthorizationTests
{
    private readonly TestDatabase database;

    public DashboardAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TeacherGuardianAccountant_Every403_Anonymous401_FounderOk()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);

        foreach ((string who, string token) in new[] { ("öğretmen", w.School.Class.Teacher), ("veli", w.School.AliMother.Token), ("muhasebe", w.Accountant) })
        {
            foreach (string path in AllPaths)
            {
                (await StatusAsync(client, HttpMethod.Get, path, token)).ShouldBe(HttpStatusCode.Forbidden, $"{who} {path}");
            }
        }

        foreach (string path in AllPaths)
        {
            using HttpResponseMessage anonymous = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }

        (await ExportAuditAsync(factory, w.TenantId)).ShouldBeEmpty();
        foreach (string path in AllPaths.Where(p => !p.Contains("/export", StringComparison.Ordinal)))
        {
            (await StatusAsync(client, HttpMethod.Get, path, w.Founder)).ShouldBe(HttpStatusCode.OK, path);
        }

        foreach (string kind in new[] { "attendance", "payments", "balances" })
        {
            (await ExportAsync(client, w.Founder, kind, "startDate=2026-11-01&endDate=2026-11-20")).Status.ShouldBe(HttpStatusCode.OK, kind);
        }
    }

    [Fact]
    public async Task FounderRoleRevoked_NextRequestRejected()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        string email = UniqueEmail("kurucu2");
        Guid second = await AddMemberAsync(factory, email, w.TenantId, Role.Founder, null);
        string token = await TokenForSchoolAsync(client, email, w.TenantId);
        (await OverdueCardAsync(client, token)).Status.ShouldBe(HttpStatusCode.OK);

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{second}/roles", w.Founder, new { roles = new[] { new { role = "Teacher", branchIds = new[] { w.BranchId } } } }))
            .ShouldBe(HttpStatusCode.OK);

        (await OverdueCardAsync(client, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PaymentsReportAsync(client, token, "startDate=2026-11-01&endDate=2026-11-20")).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await ExportAsync(client, token, "balances")).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await ExportAuditAsync(factory, w.TenantId)).ShouldBeEmpty();
    }
}
