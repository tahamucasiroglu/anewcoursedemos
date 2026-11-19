using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>İki okullu kişi: okul seçimi ve değiştirme (spec 0004 AC-4, AC-9'un "iki okullu kişi" kısmı).</summary>
[Trait("Category", "Integration")]
public sealed class SelectTenantTests
{
    private readonly TestDatabase database;

    public SelectTenantTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Login_TwoSchools_RequiresSelection_ThenSwitchShowsOnlyChosenSchool()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed first = await CreateSchoolAsync(factory, schoolName: "A Okulu " + Guid.NewGuid().ToString("N"));
        SchoolSeed second = await CreateSchoolAsync(factory, email: first.FounderEmail, schoolName: "B Okulu " + Guid.NewGuid().ToString("N"));

        JsonElement auth = await LoginAsync(client, first.FounderEmail);
        auth.GetProperty("requiresTenantSelection").GetBoolean().ShouldBeTrue();
        auth.GetProperty("tenants").GetArrayLength().ShouldBe(2);
        string unselected = AccessToken(auth);
        using (HttpResponseMessage branches = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/branches", unselected))
        {
            branches.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        string tokenA = AccessToken(await SelectAsync(client, unselected, first.TenantId, HttpStatusCode.OK));
        JsonElement meA = await MeAsync(client, tokenA);
        meA.GetProperty("tenant").GetProperty("id").GetGuid().ShouldBe(first.TenantId);
        using (HttpResponseMessage otherBranch = await SendAuthorizedAsync(client, HttpMethod.Get, $"/api/branches/{second.BranchId}", tokenA))
        {
            otherBranch.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        string tokenB = AccessToken(await SelectAsync(client, tokenA, second.TenantId, HttpStatusCode.OK));
        JsonElement meB = await MeAsync(client, tokenB);
        meB.GetProperty("tenant").GetProperty("id").GetGuid().ShouldBe(second.TenantId);
        meB.GetProperty("branches").EnumerateArray().Select(b => b.GetProperty("id").GetGuid()).ShouldBe([second.BranchId]);

        using HttpResponseMessage stale = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", tokenA);
        stale.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SelectTenant_SchoolWithoutRole_Returns404()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed mine = await CreateSchoolAsync(factory);
        SchoolSeed other = await CreateSchoolAsync(factory);
        JsonElement auth = await LoginAsync(client, mine.FounderEmail);

        JsonElement body = await SelectAsync(client, AccessToken(auth), other.TenantId, HttpStatusCode.NotFound);

        body.GetProperty("detail").GetString().ShouldBe("Okul bulunamadı");
    }

    [Fact]
    public async Task Login_TeacherInTwoSchools_ListsBothSchoolsSortedByName()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N");
        SchoolSeed b = await CreateSchoolAsync(factory, schoolName: "Beta " + suffix);
        SchoolSeed a = await CreateSchoolAsync(factory, schoolName: "Alfa " + suffix);
        string teacher = UniqueEmail("ogretmen");
        await AddMemberAsync(factory, teacher, b.TenantId, Role.Teacher, b.BranchId);
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);

        JsonElement auth = await LoginAsync(client, teacher);

        auth.GetProperty("tenants").EnumerateArray().Select(t => t.GetProperty("name").GetString())
            .ShouldBe(["Alfa " + suffix, "Beta " + suffix]);
    }

    private static async Task<JsonElement> SelectAsync(HttpClient client, string token, Guid tenantId, HttpStatusCode expected)
    {
        using HttpResponseMessage response = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/select-tenant", token, new { tenantId });
        response.StatusCode.ShouldBe(expected);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> MeAsync(HttpClient client, string token)
    {
        using HttpResponseMessage response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadJsonAsync(response);
    }
}
