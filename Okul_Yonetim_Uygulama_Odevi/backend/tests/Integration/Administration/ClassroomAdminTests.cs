using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Spec 0008 AC-7, AC-8: sınıf açma (etkin şube + etkin seviye + ad ≤ 30; aynı dönem+şubede aynı ad reddi, başka şube ve
/// dönemde serbest; pasif şube reddi), düzenleme (ad, seviye; dönem ve şube sabit), pasifleştir/etkinleştir (ad
/// çakışması), dönem bazlı liste (varsayılan güncel dönem), şube/durum süzgeci, şube–seviye–ad sırası, "şube pasif".
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClassroomAdminTests
{
    private const string Path = "/api/admin/classrooms";
    private readonly TestDatabase database;

    public ClassroomAdminTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_RequiresActiveBranchAndLevel_NameUniquePerTermAndBranch_ValidatesName()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)));
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid avcilar = await CreateBranchAsync(client, token, "Avcılar");
        Guid closed = await CreateBranchAsync(client, token, "Kapalı Şube");
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{closed}/deactivate", token)).ShouldBe(HttpStatusCode.OK);
        Guid fifth = await CreateGradeLevelAsync(client, token, "5. sınıf", 5);
        Guid autumn = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");
        Guid spring = await CreateTermAsync(client, token, "Bahar", "2027-02-08", "2027-06-18");

        var (created, body, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = a.BranchId, gradeLevelId = fifth, name = " 5-A " });
        created.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("name").GetString().ShouldBe("5-A");
        body.GetProperty("branchName").GetString().ShouldBe("Merkez");
        body.GetProperty("gradeLevelName").GetString().ShouldBe("5. sınıf");
        body.GetProperty("termId").GetGuid().ShouldBe(autumn);
        var (_, list, _) = await CallAsync(client, HttpMethod.Get, $"{Path}?termId={autumn}", token);
        list.GetProperty("items").EnumerateArray().Select(c => (c.GetProperty("branchName").GetString(), c.GetProperty("gradeLevelName").GetString(), c.GetProperty("name").GetString()))
            .ShouldBe([("Merkez", "5. sınıf", "5-A")]);

        var (dup, problem, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = a.BranchId, gradeLevelId = fifth, name = "5-a" });
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu adla bir sınıf zaten var");
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = avcilar, gradeLevelId = fifth, name = "5-A" })).ShouldBe(HttpStatusCode.OK, "başka şubede aynı ad");
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { termId = spring, branchId = a.BranchId, gradeLevelId = fifth, name = "5-A" })).ShouldBe(HttpStatusCode.OK, "başka dönemde aynı ad");

        var (passive, passiveProblem, _) = await CallAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = closed, gradeLevelId = fifth, name = "5-K" });
        passive.ShouldBe(HttpStatusCode.BadRequest);
        Detail(passiveProblem).ShouldBe("Seçilen şube bulunamadı ya da pasif");
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = a.BranchId, gradeLevelId = fifth, name = " " })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = a.BranchId, gradeLevelId = fifth, name = new string('x', 31) })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = a.BranchId, name = "5-Z" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, Path, token, new { termId = autumn, branchId = a.BranchId, gradeLevelId = fifth, name = new string('y', 30) })).ShouldBe(HttpStatusCode.OK);
        (await QueryAsync(factory, c => c.Classrooms.IgnoreQueryFilters().CountAsync(x => x.TenantId == a.TenantId))).ShouldBe(4);
    }

    [Fact]
    public async Task Update_Deactivate_Activate_List_FiltersOrderAndDefaultTerm()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)));
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid avcilar = await CreateBranchAsync(client, token, "Avcılar");
        Guid closing = await CreateBranchAsync(client, token, "Zeytinburnu");
        Guid fifth = await CreateGradeLevelAsync(client, token, "5. sınıf", 5);
        Guid sixth = await CreateGradeLevelAsync(client, token, "6. sınıf", 6);
        Guid autumn = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");
        Guid spring = await CreateTermAsync(client, token, "Bahar", "2027-02-08", "2027-06-18");
        Guid classroom = await CreateClassroomAsync(client, token, autumn, a.BranchId, fifth, "5-A");

        var (updated, body, _) = await CallAsync(client, HttpMethod.Put, $"{Path}/{classroom}", token, new { termId = spring, branchId = avcilar, gradeLevelId = sixth, name = "6-A" });
        updated.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("name").GetString().ShouldBe("6-A");
        body.GetProperty("gradeLevelId").GetGuid().ShouldBe(sixth);
        body.GetProperty("termId").GetGuid().ShouldBe(autumn, "dönem değişmez");
        body.GetProperty("branchId").GetGuid().ShouldBe(a.BranchId, "şube değişmez");

        (await StatusAsync(client, HttpMethod.Post, $"{Path}/{classroom}/deactivate", token)).ShouldBe(HttpStatusCode.OK);
        Guid replacement = await CreateClassroomAsync(client, token, autumn, a.BranchId, sixth, "6-a");
        var (blocked, problem, _) = await CallAsync(client, HttpMethod.Post, $"{Path}/{classroom}/activate", token);
        blocked.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu adla bir sınıf zaten var");
        var (_, withPassive, _) = await CallAsync(client, HttpMethod.Get, $"{Path}?termId={autumn}", token);
        Item(withPassive, classroom).GetProperty("isActive").GetBoolean().ShouldBeFalse("pasif sınıf listede kalır");
        (await StatusAsync(client, HttpMethod.Post, $"{Path}/{replacement}/deactivate", token)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, $"{Path}/{classroom}/activate", token)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Put, $"{Path}/{replacement}", token, new { gradeLevelId = sixth, name = "6-Y" })).ShouldBe(HttpStatusCode.OK, "pasif sınıf da düzenlenir");

        await CreateClassroomAsync(client, token, autumn, avcilar, sixth, "6-B");
        await CreateClassroomAsync(client, token, autumn, avcilar, fifth, "5-B");
        await CreateClassroomAsync(client, token, autumn, avcilar, fifth, "5-A");
        Guid inClosing = await CreateClassroomAsync(client, token, autumn, closing, fifth, "5-Z");
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{closing}/deactivate", token)).ShouldBe(HttpStatusCode.OK);

        var (_, byDefault, _) = await CallAsync(client, HttpMethod.Get, Path, token);
        byDefault.GetProperty("termId").GetGuid().ShouldBe(autumn, "varsayılan güncel dönem");
        byDefault.GetProperty("items").EnumerateArray().Select(c => $"{c.GetProperty("branchName").GetString()} {c.GetProperty("name").GetString()}")
            .ShouldBe(["Avcılar 5-A", "Avcılar 5-B", "Avcılar 6-B", "Merkez 6-A", "Merkez 6-Y", "Zeytinburnu 5-Z"]);
        Item(byDefault, inClosing).GetProperty("branchIsActive").GetBoolean().ShouldBeFalse("pasif şubenin sınıfı 'şube pasif' etiketiyle");

        var (_, onlyAvcilar, _) = await CallAsync(client, HttpMethod.Get, $"{Path}?termId={autumn}&branchId={avcilar}", token);
        onlyAvcilar.GetProperty("items").EnumerateArray().ShouldAllBe(c => c.GetProperty("branchId").GetGuid() == avcilar);
        onlyAvcilar.GetProperty("items").GetArrayLength().ShouldBe(3);
        var (_, passiveOnly, _) = await CallAsync(client, HttpMethod.Get, $"{Path}?termId={autumn}&active=false", token);
        passiveOnly.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([replacement]);
        var (_, activeOnly, _) = await CallAsync(client, HttpMethod.Get, $"{Path}?termId={autumn}&active=true", token);
        activeOnly.GetProperty("items").GetArrayLength().ShouldBe(5);

        var (_, empty, _) = await CallAsync(client, HttpMethod.Get, $"{Path}?termId={spring}", token);
        empty.GetProperty("termId").GetGuid().ShouldBe(spring);
        empty.GetProperty("items").GetArrayLength().ShouldBe(0, "sınıfı olmayan dönem: 'Bu dönemde sınıf yok'");
    }

    [Fact]
    public async Task List_NoTerms_ReturnsEmptyWithoutTerm()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        var (status, body, _) = await CallAsync(client, HttpMethod.Get, Path, token);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("termId").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    private static JsonElement Item(JsonElement list, Guid id) =>
        list.GetProperty("items").EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == id);
}
