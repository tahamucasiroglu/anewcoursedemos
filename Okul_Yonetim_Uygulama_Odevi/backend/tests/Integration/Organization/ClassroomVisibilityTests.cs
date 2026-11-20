using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Organization;

/// <summary>
/// Spec 0008 AC-2 (şube duvarı, plan Risks 2): kurucu olmayan kişi yalnızca atandığı etkin şubelerin sınıflarını alır;
/// atanmadığı şubenin ve atandığı ama pasifleşmiş şubenin sınıfı 404'tür. Kurucu bütün şubelerin sınıflarını görür.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClassroomVisibilityTests
{
    private readonly TestDatabase database;

    public ClassroomVisibilityTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Teacher_SeesOnlyAssignedActiveBranchClassrooms_OthersAreNotFound()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)));
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid avcilar = await CreateBranchAsync(client, founder, "Avcılar");
        Guid closing = await CreateBranchAsync(client, founder, "Kapanan Şube");
        Guid level = await CreateGradeLevelAsync(client, founder, "5. sınıf", 5);
        Guid term = await CreateTermAsync(client, founder, "Güz", "2026-09-01", "2027-01-22");
        Guid merkezClass = await CreateClassroomAsync(client, founder, term, a.BranchId, level, "5-A");
        Guid avcilarClass = await CreateClassroomAsync(client, founder, term, avcilar, level, "5-A");
        Guid closingClass = await CreateClassroomAsync(client, founder, term, closing, level, "5-A");
        string teacher = UniqueEmail("ogretmen");
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, closing);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{closing}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);
        string token = await TokenForSchoolAsync(client, teacher, a.TenantId);

        var (status, list, _) = await CallAsync(client, HttpMethod.Get, "/api/classrooms", token);
        status.ShouldBe(HttpStatusCode.OK);
        list.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([merkezClass]);
        var (one, body, _) = await CallAsync(client, HttpMethod.Get, $"/api/classrooms/{merkezClass}", token);
        one.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("branchName").GetString().ShouldBe("Merkez");
        body.GetProperty("gradeLevelName").GetString().ShouldBe("5. sınıf");
        (await StatusAsync(client, HttpMethod.Get, $"/api/classrooms/{avcilarClass}", token)).ShouldBe(HttpStatusCode.NotFound, "atanmadığı şube");
        (await StatusAsync(client, HttpMethod.Get, $"/api/classrooms/{closingClass}", token)).ShouldBe(HttpStatusCode.NotFound, "atandığı ama pasif şube");

        var (_, all, _) = await CallAsync(client, HttpMethod.Get, "/api/classrooms", founder);
        Ids(all).ShouldBe([avcilarClass, closingClass, merkezClass], ignoreOrder: true);
        (await StatusAsync(client, HttpMethod.Get, $"/api/classrooms/{avcilarClass}", founder)).ShouldBe(HttpStatusCode.OK);
    }

    private static Guid[] Ids(JsonElement list) => [.. list.EnumerateArray().Select(c => c.GetProperty("id").GetGuid())];
}
