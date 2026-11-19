using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Organization;

/// <summary>Okul duvarı (spec 0004 AC-9, S1): A okulunun kurucusu B'nin hiçbir kaydını göremez — kimliği bilse bile.</summary>
[Trait("Category", "Integration")]
public sealed class TenantIsolationTests
{
    private readonly TestDatabase database;

    public TenantIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FounderOfA_RequestingBranchOfB_Gets404AndListOnlyHasA()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        Guid bSecond = await AddBranchAsync(factory, b.TenantId, "B İkinci");
        string token = AccessToken(await LoginAsync(client, a.FounderEmail));

        foreach (Guid foreign in new[] { b.BranchId, bSecond })
        {
            using HttpResponseMessage response = await SendAuthorizedAsync(client, HttpMethod.Get, $"/api/branches/{foreign}", token);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ReadJsonAsync(response)).GetProperty("detail").GetString().ShouldBe("Şube bulunamadı");
        }

        using HttpResponseMessage list = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/branches", token);
        JsonElement branches = await ReadJsonAsync(list);
        branches.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([a.BranchId]);
        using HttpResponseMessage own = await SendAuthorizedAsync(client, HttpMethod.Get, $"/api/branches/{a.BranchId}", token);
        own.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_OfFounderA_ContainsNoTenantOrBranchOfB()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string token = AccessToken(await LoginAsync(client, a.FounderEmail));

        using HttpResponseMessage me = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", token);
        string raw = await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        raw.ShouldNotContain(b.TenantId.ToString());
        raw.ShouldNotContain(b.BranchId.ToString());
        raw.ShouldNotContain(b.SchoolName);
    }
}
