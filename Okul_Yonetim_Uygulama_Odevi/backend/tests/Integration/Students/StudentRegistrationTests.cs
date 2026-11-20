using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-3: geçerli bilgilerle öğrenci oluşur ve listede görünür; her geçersiz alan (ad, doğum tarihi, okul numarası,
/// şube, sınıf, sağlık notu, alerji) hata verir ve hiçbir şey oluşmaz; aynı okulda aynı okul numarası (harf farkıyla bile)
/// reddedilir, başka okulda serbesttir. Bugün 2026-11-20 (sabit saat).
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentRegistrationTests
{
    private readonly TestDatabase database;

    public StudentRegistrationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_ValidStudentWithClassroom_AppearsInListAndCard()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid level = await CreateGradeLevelAsync(client, founder, "3. sınıf", 3);
        Guid term = await CreateTermAsync(client, founder, "2026-2027", "2026-09-01", "2027-06-15");
        Guid classroom = await CreateClassroomAsync(client, founder, term, a.BranchId, level, "3-A");

        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "  Zeynep Şahin ", "2018-05-04", " 2024A7 ", classroom, "Astım", "Fıstık"));

        JsonElement card = await CardAsync(client, founder, id);
        card.GetProperty("fullName").GetString().ShouldBe("Zeynep Şahin");
        card.GetProperty("birthDate").GetString().ShouldBe("2018-05-04");
        card.GetProperty("schoolNumber").GetString().ShouldBe("2024A7");
        card.GetProperty("branchName").GetString().ShouldBe("Merkez");
        card.GetProperty("classroomName").GetString().ShouldBe("3-A");
        card.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        card.GetProperty("health").GetProperty("healthNote").GetString().ShouldBe("Astım");
        card.GetProperty("health").GetProperty("allergy").GetString().ShouldBe("Fıstık");
        JsonElement row = (await ListAsync(client, founder)).GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        row.GetProperty("id").GetGuid().ShouldBe(id);
        row.GetProperty("classroomName").GetString().ShouldBe("3-A");
    }

    [Theory]
    [InlineData("fullName", "", "Ad soyad zorunludur")]
    [InlineData("fullName", "LONG_NAME", "En fazla 100 karakter olabilir")]
    [InlineData("birthDate", null, "Doğum tarihi zorunludur")]
    [InlineData("birthDate", "2026-11-21", "Doğum tarihi bugünden ileri olamaz")]
    [InlineData("birthDate", "1926-11-19", "Doğum tarihi 100 yıldan eski olamaz")]
    [InlineData("schoolNumber", "123456789012345678901", "Okul numarası en fazla 20 karakter olabilir")]
    [InlineData("schoolNumber", "12-A", "Okul numarası yalnızca harf ve rakam içerebilir")]
    [InlineData("schoolNumber", "Ş12", "Okul numarası yalnızca harf ve rakam içerebilir")] // yalnız ASCII (A-4; 0009 VERIFY V-1)
    [InlineData("schoolNumber", "ı5", "Okul numarası yalnızca harf ve rakam içerebilir")]
    [InlineData("healthNote", "LONG_NOTE", "Sağlık notu en fazla 1000 karakter olabilir")]
    [InlineData("allergy", "LONG_ALLERGY", "Alerji en fazla 500 karakter olabilir")]
    public async Task Create_InvalidField_Returns400WithFieldMessage_AndCreatesNothing(string field, string? value, string message)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Dictionary<string, object?> body = NewStudent(a.BranchId);
        body[field] = value switch
        {
            "LONG_NAME" => new string('a', 101),
            "LONG_NOTE" => new string('n', 1001),
            "LONG_ALLERGY" => new string('n', 501),
            _ => value,
        };

        var (status, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/students", founder, body);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain(message);
        (await StudentRowsAsync(factory, a.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_BoundaryValues_AreAccepted()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);

        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, new string('a', 100), "2026-11-20", "ABCDEFGHIJ1234567890", null, new string('n', 1000), new string('n', 500)));
        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Yüz Yaşında", "1926-11-20"));

        (await StudentRowsAsync(factory, a.TenantId)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Create_UnavailableBranchOrClassroom_Returns400_AndCreatesNothing()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        (SchoolSeed b, string founderB) = await SchoolAsync(factory, client);
        Guid passive = await CreateBranchAsync(client, founder, "Kapanan");
        Guid second = await CreateBranchAsync(client, founder, "İkinci");
        Guid level = await CreateGradeLevelAsync(client, founder, "4. sınıf", 4);
        Guid term = await CreateTermAsync(client, founder, "2026-2027", "2026-09-01", "2027-06-15");
        Guid passiveClass = await CreateClassroomAsync(client, founder, term, a.BranchId, level, "4-P");
        Guid secondClass = await CreateClassroomAsync(client, founder, term, second, level, "4-A");
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/classrooms/{passiveClass}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{passive}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);
        Guid levelB = await CreateGradeLevelAsync(client, founderB, "4. sınıf", 4);
        Guid termB = await CreateTermAsync(client, founderB, "2026-2027", "2026-09-01", "2027-06-15");
        Guid classB = await CreateClassroomAsync(client, founderB, termB, b.BranchId, levelB, "4-B");

        (object Body, string Message)[] cases =
        [
            (NewStudent(passive), "Seçilen şube bulunamadı ya da pasif"),
            (NewStudent(b.BranchId), "Seçilen şube bulunamadı ya da pasif"),
            (NewStudent(Guid.Empty), "Şube seçin"),
            (NewStudent(a.BranchId, classroomId: passiveClass), "Seçilen sınıf bulunamadı, pasif ya da bu şubenin değil"),
            (NewStudent(a.BranchId, classroomId: secondClass), "Seçilen sınıf bulunamadı, pasif ya da bu şubenin değil"),
            (NewStudent(a.BranchId, classroomId: classB), "Seçilen sınıf bulunamadı, pasif ya da bu şubenin değil"),
        ];
        foreach ((object body, string message) in cases)
        {
            var (status, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/students", founder, body);
            status.ShouldBe(HttpStatusCode.BadRequest);
            Messages(problem).ShouldContain(message);
        }

        (await StudentRowsAsync(factory, a.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateSchoolNumberIgnoringCase_RejectedInSameSchool_FreeInOtherSchool()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        (SchoolSeed b, string founderB) = await SchoolAsync(factory, client);
        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "İlk", schoolNumber: "abc12"));

        var (status, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/students", founder, NewStudent(a.BranchId, "İkinci", schoolNumber: "ABC12"));

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu okul numarası başka bir öğrencide");
        (await StudentRowsAsync(factory, a.TenantId)).ShouldHaveSingleItem();
        await CreateStudentAsync(client, founderB, NewStudent(b.BranchId, "Başka okulda", schoolNumber: "ABC12"));
        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Numarasız 1"));
        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Numarasız 2", schoolNumber: "  "));
        (await StudentRowsAsync(factory, a.TenantId)).Count.ShouldBe(3, "boş numara tekillik dışında");
    }
}
