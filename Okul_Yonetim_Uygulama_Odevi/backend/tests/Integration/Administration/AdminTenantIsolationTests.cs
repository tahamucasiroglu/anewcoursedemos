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

/// <summary>Spec 0005 AC-2: A'nın kurucusu B'nin şube ve kişisine hiçbir işlem yapamaz (404); listeler yalnız A; iki okullu kişinin A kartında B rolleri yok.</summary>
[Trait("Category", "Integration")]
public sealed class AdminTenantIsolationTests
{
    private readonly TestDatabase database;

    public AdminTenantIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FounderOfA_CannotTouchSchoolB_AndSeesOnlyA()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string onlyB = UniqueEmail("sadece-b");
        Guid onlyBId = await AddMemberAsync(factory, onlyB, b.TenantId, Role.Teacher, b.BranchId);
        string both = UniqueEmail("iki-okul");
        Guid bothId = await AddMemberAsync(factory, both, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, both, b.TenantId, Role.Accounting, b.BranchId);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        foreach ((HttpMethod method, string path, object? body) in new (HttpMethod, string, object?)[]
        {
            (HttpMethod.Get, $"/api/admin/members/{onlyBId}", null),
            (HttpMethod.Put, $"/api/admin/members/{onlyBId}/profile", new { fullName = "Değişti", phone = (string?)null }),
            (HttpMethod.Put, $"/api/admin/members/{onlyBId}/roles", new { roles = new[] { Teacher(a.BranchId) } }),
            (HttpMethod.Post, $"/api/admin/members/{onlyBId}/deactivate", null),
            (HttpMethod.Post, $"/api/admin/members/{onlyBId}/reset-password", null),
            (HttpMethod.Put, $"/api/admin/branches/{b.BranchId}", new { name = "Ele geçti" }),
            (HttpMethod.Post, $"/api/admin/branches/{b.BranchId}/deactivate", null),
        })
        {
            (await StatusAsync(client, method, path, token, body)).ShouldBe(HttpStatusCode.NotFound, $"{method} {path}");
        }

        (await StatusAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(UniqueEmail(), roles: Teacher(b.BranchId)))).ShouldBe(HttpStatusCode.BadRequest);
        var (_, members, membersRaw) = await CallAsync(client, HttpMethod.Get, "/api/admin/members", token);
        membersRaw.ShouldNotContain(onlyB);
        members.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("userId").GetGuid()).ShouldBe([a.FounderId, bothId], ignoreOrder: true);
        var (_, branches, _) = await CallAsync(client, HttpMethod.Get, "/api/admin/branches", token);
        branches.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([a.BranchId]);
        var (_, card, cardRaw) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{bothId}", token);
        card.GetProperty("roles").EnumerateArray().Select(r => r.GetProperty("role").GetString()).ShouldBe(["Teacher"]);
        cardRaw.ShouldNotContain(b.BranchId.ToString());
        cardRaw.ShouldNotContain(b.TenantId.ToString());
        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == onlyBId).Select(u => u.FullName).SingleAsync())).ShouldBe("Test Kişi");

        // Review M-2: iki okullu kişinin adı ve telefonu ortak; A'nın kurucusu değiştiremez (B'nin gördüğü bilgi değişmez).
        card.GetProperty("canEditProfile").GetBoolean().ShouldBeFalse();
        var (profile, profileProblem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{bothId}/profile", token, new { fullName = "A'nın Verdiği Ad", phone = "0532 000 00 00" });
        profile.ShouldBe(HttpStatusCode.BadRequest);
        Detail(profileProblem).ShouldBe("Bu kişi başka bir okulda da kayıtlı; adını ve telefonunu kendisi değiştirebilir.");
        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == bothId).Select(u => new { u.FullName, u.Phone }).SingleAsync()))
            .ShouldBe(new { FullName = "Test Kişi", Phone = (string?)null });
    }
}
