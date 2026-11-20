using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-2 (S1, plan Risks 4): A okulunun kurucusu B okulunun öğrencisini kimliğini bilerek bile göremez,
/// düzenleyemez, veli bağlayamaz (404) ve listesinde yalnızca A vardır. Veli aynı şubedeki bağlı olmadığı öğrencinin
/// kartını açamaz (404), listesinde yalnızca kendi çocukları vardır; başka okuldaki çocuğu o okul seçilmeden görünmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentIsolationTests
{
    private readonly TestDatabase database;

    public StudentIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FounderOfA_CannotReachStudentOfB_AndListsOnlyA()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founderA) = await SchoolAsync(factory, client);
        (SchoolSeed b, string founderB) = await SchoolAsync(factory, client);
        Guid studentA = await CreateStudentAsync(client, founderA, NewStudent(a.BranchId, "A Öğrencisi"));
        Guid studentB = await CreateStudentAsync(client, founderB, NewStudent(b.BranchId, "B Öğrencisi", healthNote: "B notu"));

        (HttpMethod Method, string Path, object? Body)[] calls =
        [
            (HttpMethod.Get, $"/api/admin/students/{studentB}", null),
            (HttpMethod.Put, $"/api/admin/students/{studentB}", NewStudent(b.BranchId, "Ele geçirildi")),
            (HttpMethod.Put, $"/api/admin/students/{studentB}/health", new { healthNote = "x" }),
            (HttpMethod.Post, $"/api/admin/students/{studentB}/deactivate", null),
            (HttpMethod.Post, $"/api/admin/students/{studentB}/guardians", Guardian(UniqueEmail("v"))),
            (HttpMethod.Put, $"/api/admin/students/{studentB}/pickup-persons", new { persons = Array.Empty<object>() }),
        ];
        foreach ((HttpMethod method, string path, object? body) in calls)
        {
            (await StatusAsync(client, method, path, founderA, body)).ShouldBe(HttpStatusCode.NotFound, $"{method} {path}");
        }

        (await StatusAsync(client, HttpMethod.Post, "/api/admin/students", founderA, NewStudent(b.BranchId, "B şubesine"))).ShouldBe(HttpStatusCode.BadRequest);
        Ids((await ListAsync(client, founderA)).GetProperty("items")).ShouldBe([studentA]);
        JsonElement cardB = await CardAsync(client, founderB, studentB);
        cardB.GetProperty("fullName").GetString().ShouldBe("B Öğrencisi");
        cardB.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        cardB.GetProperty("guardians").GetArrayLength().ShouldBe(0);
        cardB.GetProperty("health").GetProperty("healthNote").GetString().ShouldBe("B notu");
    }

    [Fact]
    public async Task Guardian_SeesOnlyLinkedChildren_SameBranchUnlinkedIsNotFound()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid mine = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Benim Çocuğum"));
        Guid other = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Başkasının Çocuğu", healthNote: "gizli not"));
        (string guardian, _, _, _) = await NewGuardianAsync(client, founder, a.TenantId, mine);

        var (status, list, _) = await CallAsync(client, HttpMethod.Get, "/api/my/children", guardian);
        status.ShouldBe(HttpStatusCode.OK);
        Ids(list).ShouldBe([mine]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{mine}", guardian)).ShouldBe(HttpStatusCode.OK);
        var (otherStatus, _, otherRaw) = await CallAsync(client, HttpMethod.Get, $"/api/my/children/{other}", guardian);
        otherStatus.ShouldBe(HttpStatusCode.NotFound, "aynı şubede ama bağı yok");
        otherRaw.ShouldNotContain("gizli not");
        otherRaw.ShouldNotContain("Başkasının");
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{Guid.CreateVersion7()}", guardian)).ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Guardian_ChildInOtherSchool_VisibleOnlyWhenThatSchoolIsSelected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founderA) = await SchoolAsync(factory, client);
        (SchoolSeed b, string founderB) = await SchoolAsync(factory, client);
        Guid inA = await CreateStudentAsync(client, founderA, NewStudent(a.BranchId, "A Çocuğu"));
        Guid inB = await CreateStudentAsync(client, founderB, NewStudent(b.BranchId, "B Çocuğu"));
        (_, _, string email, string password) = await NewGuardianAsync(client, founderA, a.TenantId, inA);
        JsonElement linked = await LinkAsync(client, founderB, inB, Guardian(email));
        linked.GetProperty("existingAccount").GetBoolean().ShouldBeTrue();

        string inSchoolA = await TokenForSchoolAsync(client, email, a.TenantId, password);
        string inSchoolB = await TokenForSchoolAsync(client, email, b.TenantId, password);

        Ids((await CallAsync(client, HttpMethod.Get, "/api/my/children", inSchoolA)).Body).ShouldBe([inA]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{inB}", inSchoolA)).ShouldBe(HttpStatusCode.NotFound);
        Ids((await CallAsync(client, HttpMethod.Get, "/api/my/children", inSchoolB)).Body).ShouldBe([inB]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{inA}", inSchoolB)).ShouldBe(HttpStatusCode.NotFound);
    }
}
