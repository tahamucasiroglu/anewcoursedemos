using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-5: kurucu bilgileri ve sınıfı (aynı şubenin sınıfı ya da boş) değiştirir; şube değiştirme ve başka şubenin
/// sınıfı reddedilir; "ayrıldı" öğrenci silinmez, kurucu listesinde "ayrıldı" görünür, velinin listesinden düşer ve kartı
/// veliye 404 olur; "geri al" her şeyi eski hâline döndürür.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentUpdateTests
{
    private readonly TestDatabase database;

    public StudentUpdateTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Update_DetailsAndClassroom_SameBranchOnly_BranchCannotChange()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid second = await CreateBranchAsync(client, founder, "İkinci");
        Guid level = await CreateGradeLevelAsync(client, founder, "6. sınıf", 6);
        Guid term = await CreateTermAsync(client, founder, "2026-2027", "2026-09-01", "2027-06-15");
        Guid classA = await CreateClassroomAsync(client, founder, term, a.BranchId, level, "6-A");
        Guid classB = await CreateClassroomAsync(client, founder, term, a.BranchId, level, "6-B");
        Guid otherBranchClass = await CreateClassroomAsync(client, founder, term, second, level, "6-A");
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Eski Ad", "2014-01-01", "55", classA, "not"));

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}", founder, NewStudent(a.BranchId, "Yeni Ad", "2014-02-02", "56", classB))).ShouldBe(HttpStatusCode.OK);
        JsonElement card = await CardAsync(client, founder, id);
        card.GetProperty("fullName").GetString().ShouldBe("Yeni Ad");
        card.GetProperty("birthDate").GetString().ShouldBe("2014-02-02");
        card.GetProperty("schoolNumber").GetString().ShouldBe("56");
        card.GetProperty("classroomName").GetString().ShouldBe("6-B");
        card.GetProperty("health").GetProperty("healthNote").GetString().ShouldBe("not", "bilgi düzenlemesi sağlığa dokunmaz");

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}", founder, NewStudent(a.BranchId, "Yeni Ad", "2014-02-02", "56", null))).ShouldBe(HttpStatusCode.OK);
        (await CardAsync(client, founder, id)).GetProperty("classroomId").ValueKind.ShouldBe(JsonValueKind.Null);

        var (moved, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}", founder, NewStudent(second, "Yeni Ad", "2014-02-02", "56", null));
        moved.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Kayıttan sonra şube değiştirilemez");
        var (wrongClass, problem2, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}", founder, NewStudent(a.BranchId, "Başka", "2014-02-02", "56", otherBranchClass));
        wrongClass.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem2).ShouldBe("Seçilen sınıf bulunamadı, pasif ya da bu şubenin değil");
        JsonElement after = await CardAsync(client, founder, id);
        after.GetProperty("branchId").GetGuid().ShouldBe(a.BranchId);
        after.GetProperty("fullName").GetString().ShouldBe("Yeni Ad");
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{Guid.CreateVersion7()}", founder, NewStudent(a.BranchId))).ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_SchoolNumberTakenByOther_Rejected_OwnNumberKept()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Bir", schoolNumber: "A1"));
        Guid two = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "İki", schoolNumber: "A2"));

        var (status, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{two}", founder, NewStudent(a.BranchId, "İki", schoolNumber: "a1"));
        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu okul numarası başka bir öğrencide");
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{two}", founder, NewStudent(a.BranchId, "İki Yeni", schoolNumber: "a2"))).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkLeft_KeepsStudent_HidesFromGuardian_Reinstate_RestoresEverything()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Gidecek Öğrenci", schoolNumber: "77"));
        (string guardian, Guid guardianId, _, _) = await NewGuardianAsync(client, founder, a.TenantId, id);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);

        (await StudentRowsAsync(factory, a.TenantId)).ShouldHaveSingleItem().IsActive.ShouldBeFalse();
        JsonElement row = (await ListAsync(client, founder)).GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        row.GetProperty("isActive").GetBoolean().ShouldBeFalse();
        (await CallAsync(client, HttpMethod.Get, "/api/my/children", guardian)).Body.GetArrayLength().ShouldBe(0);
        var (hidden, problem, _) = await CallAsync(client, HttpMethod.Get, $"/api/my/children/{id}", guardian);
        hidden.ShouldBe(HttpStatusCode.NotFound);
        Detail(problem).ShouldBe("Bu öğrenci artık görüntülenemiyor.");

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/activate", founder)).ShouldBe(HttpStatusCode.OK);

        JsonElement card = await CardAsync(client, founder, id);
        card.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        card.GetProperty("schoolNumber").GetString().ShouldBe("77");
        card.GetProperty("guardians").EnumerateArray().ShouldHaveSingleItem().GetProperty("userId").GetGuid().ShouldBe(guardianId);
        Ids((await CallAsync(client, HttpMethod.Get, "/api/my/children", guardian)).Body).ShouldBe([id]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{id}", guardian)).ShouldBe(HttpStatusCode.OK);
    }
}
