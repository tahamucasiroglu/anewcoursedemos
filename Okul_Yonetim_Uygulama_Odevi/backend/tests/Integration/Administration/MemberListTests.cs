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

/// <summary>Spec 0005 AC-7: liste (sayfa başına 25), ad/e-posta araması, rol/şube/durum süzgeci, boş sonuç, profil düzenleme (e-posta değişmez).</summary>
[Trait("Category", "Integration")]
public sealed class MemberListTests
{
    private readonly TestDatabase database;

    public MemberListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task List_PagesSearchesFilters_AndProfileEditKeepsEmail()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid second = await CreateBranchAsync(client, token, "Avcılar");
        for (int i = 0; i < 25; i++)
        {
            await AddMemberAsync(factory, UniqueEmail($"liste{i:00}"), a.TenantId, Role.Teacher, a.BranchId);
        }

        string veli = UniqueEmail("zeynep-veli");
        Guid veliId = await AddMemberAsync(factory, veli, a.TenantId, Role.Guardian, second);

        var (_, page1, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members", token);
        page1.GetProperty("total").GetInt32().ShouldBe(27);
        page1.GetProperty("items").GetArrayLength().ShouldBe(25);
        var (_, page2, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?page=2", token);
        page2.GetProperty("items").GetArrayLength().ShouldBe(2);

        var (_, byEmail, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?search=ZEYNEP-VELI", token);
        byEmail.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("userId").GetGuid()).ShouldBe([veliId]);
        var (_, byRole, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?role=Guardian", token);
        byRole.GetProperty("total").GetInt32().ShouldBe(1);
        var (_, byBranch, _) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members?branchId={second}", token);
        byBranch.GetProperty("total").GetInt32().ShouldBe(1);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{veliId}/profile", token, new { fullName = "Şule Işık", phone = (string?)null })).ShouldBe(HttpStatusCode.OK);
        var (_, turkish, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?search=%C5%9EULE%20I%C5%9EIK", token);
        turkish.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("userId").GetGuid()).ShouldBe([veliId]);
        var (_, none, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?search=%25", token);
        none.GetProperty("total").GetInt32().ShouldBe(0);
        none.GetProperty("items").GetArrayLength().ShouldBe(0);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{veliId}/deactivate", token)).ShouldBe(HttpStatusCode.OK);
        var (_, inactive, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?status=inactive", token);
        inactive.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("userId").GetGuid()).ShouldBe([veliId]);
        var (_, active, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/members?status=active", token);
        active.GetProperty("total").GetInt32().ShouldBe(26);

        var (edited, card, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{veliId}/profile", token, new { fullName = "Zeynep Kaya", phone = "+90 533 765 43 21", email = "degisti@ornek.test" });
        edited.ShouldBe(HttpStatusCode.OK);
        card.GetProperty("fullName").GetString().ShouldBe("Zeynep Kaya");
        card.GetProperty("phone").GetString().ShouldBe("+905337654321");
        card.GetProperty("email").GetString().ShouldBe(veli);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{veliId}/profile", token, new { fullName = "", phone = (string?)null })).ShouldBe(HttpStatusCode.BadRequest);
    }
}
