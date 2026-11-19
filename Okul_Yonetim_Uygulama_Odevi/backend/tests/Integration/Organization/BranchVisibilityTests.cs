using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Organization;

/// <summary>Şube görünürlüğü (spec 0004 AC-10, BR-1): kurucu tüm şubeler; öğretmen yalnızca atandığı şube.</summary>
[Trait("Category", "Integration")]
public sealed class BranchVisibilityTests
{
    private readonly TestDatabase database;

    public BranchVisibilityTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Founder_SeesAllBranches_TeacherOnlyAssignedOne()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        Guid second = await AddBranchAsync(factory, school.TenantId, "Avcılar");
        string teacher = UniqueEmail("ogretmen");
        await AddMemberAsync(factory, teacher, school.TenantId, Role.Teacher, second);

        string founderToken = AccessToken(await LoginAsync(client, school.FounderEmail));
        (await BranchIdsAsync(client, founderToken)).ShouldBe([school.BranchId, second], ignoreOrder: true);

        string teacherToken = AccessToken(await LoginAsync(client, teacher));
        (await BranchIdsAsync(client, teacherToken)).ShouldBe([second]);
        using HttpResponseMessage hidden = await SendAuthorizedAsync(client, HttpMethod.Get, $"/api/branches/{school.BranchId}", teacherToken);
        hidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using HttpResponseMessage me = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", teacherToken);
        JsonElement body = await ReadJsonAsync(me);
        body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Teacher"]);
    }

    [Fact]
    public async Task MemberWithRoleButNoBranchVisible_GetsEmptyBranchList()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        SchoolSeed other = await CreateSchoolAsync(factory);
        string guardian = UniqueEmail("veli");
        // Kenar durum: atama başka okulun şubesine işaret ediyor (bozuk veri) — kişi bu okulda hiçbir şube görmemeli.
        await AddMemberAsync(factory, guardian, school.TenantId, Role.Guardian, other.BranchId);

        string token = AccessToken(await LoginAsync(client, guardian));

        (await BranchIdsAsync(client, token)).ShouldBeEmpty();
        using HttpResponseMessage me = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", token);
        (await ReadJsonAsync(me)).GetProperty("branches").GetArrayLength().ShouldBe(0);
    }

    private static async Task<Guid[]> BranchIdsAsync(HttpClient client, string token)
    {
        using HttpResponseMessage response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/branches", token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return [.. (await ReadJsonAsync(response)).EnumerateArray().Select(b => b.GetProperty("id").GetGuid())];
    }
}
