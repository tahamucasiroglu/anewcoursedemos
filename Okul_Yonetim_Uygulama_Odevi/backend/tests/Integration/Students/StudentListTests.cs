using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-4: liste ad sırasıyla (Türkçe harf kuralı) ad-soyad, okul numarası, şube, sınıf, veli sayısı ve durumla
/// gelir; ad / okul numarası araması, şube / sınıf / durum süzgeci; 25'ten fazlasında sayfalama; boş sonuç; yanıtta sağlık
/// bilgisi yok.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentListTests
{
    private readonly TestDatabase database;

    public StudentListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task List_SortsTurkish_ShowsColumns_FiltersAndSearches_WithoutHealth()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, founder, "Avcılar");
        Guid level = await CreateGradeLevelAsync(client, founder, "5. sınıf", 5);
        Guid term = await CreateTermAsync(client, founder, "2026-2027", "2026-09-01", "2027-06-15");
        Guid classroom = await CreateClassroomAsync(client, founder, term, a.BranchId, level, "5-A");
        Guid zeynep = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Zeynep Ak", schoolNumber: "300", classroomId: classroom, healthNote: "GİZLİ-SAĞLIK", allergy: "GİZLİ-ALERJİ"));
        Guid sule = await CreateStudentAsync(client, founder, NewStudent(avcilar, "Şule Er", schoolNumber: "200"));
        Guid ilker = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "İlker Can", schoolNumber: "X9"));
        Guid ismail = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Irmak Su"));
        Guid cem = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Çağla Öz"));
        await LinkAsync(client, founder, zeynep, Guardian(IdentityTestData.UniqueEmail("v1")));
        await LinkAsync(client, founder, zeynep, Guardian(IdentityTestData.UniqueEmail("v2"), relationship: "Father"));
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{cem}/deactivate", founder)).ShouldBe(System.Net.HttpStatusCode.OK);

        var (_, page, raw) = await CallAsync(client, HttpMethod.Get, "/api/admin/students", founder);

        Ids(page.GetProperty("items")).ShouldBe([cem, ismail, ilker, sule, zeynep], "Türkçe sıra: Ç < I < İ < Ş < Z");
        page.GetProperty("total").GetInt32().ShouldBe(5);
        raw.ShouldNotContain("GİZLİ");
        raw.ShouldNotContain("health", Case.Insensitive);
        raw.ShouldNotContain("allergy", Case.Insensitive);
        JsonElement z = page.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == zeynep);
        z.GetProperty("fullName").GetString().ShouldBe("Zeynep Ak");
        z.GetProperty("schoolNumber").GetString().ShouldBe("300");
        z.GetProperty("branchName").GetString().ShouldBe("Merkez");
        z.GetProperty("classroomName").GetString().ShouldBe("5-A");
        z.GetProperty("guardianCount").GetInt32().ShouldBe(2);
        z.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        page.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == cem).GetProperty("isActive").GetBoolean().ShouldBeFalse();

        Ids((await ListAsync(client, founder, "?search=şule")).GetProperty("items")).ShouldBe([sule]);
        Ids((await ListAsync(client, founder, "?search=ilker")).GetProperty("items")).ShouldBe([ilker], "i/İ eşleşmesi");
        Ids((await ListAsync(client, founder, "?search=x9")).GetProperty("items")).ShouldBe([ilker], "okul numarası");
        Ids((await ListAsync(client, founder, $"?branchId={avcilar}")).GetProperty("items")).ShouldBe([sule]);
        Ids((await ListAsync(client, founder, $"?classroomId={classroom}")).GetProperty("items")).ShouldBe([zeynep]);
        Ids((await ListAsync(client, founder, "?status=left")).GetProperty("items")).ShouldBe([cem]);
        Ids((await ListAsync(client, founder, "?status=active")).GetProperty("items")).ShouldBe([ismail, ilker, sule, zeynep]);
        JsonElement none = await ListAsync(client, founder, "?search=bulunmayan");
        none.GetProperty("items").GetArrayLength().ShouldBe(0);
        none.GetProperty("total").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task List_MoreThan25_IsPaged()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        for (int i = 1; i <= 27; i++)
        {
            await CreateStudentAsync(client, founder, NewStudent(a.BranchId, $"Öğrenci {i:D2}"));
        }

        JsonElement first = await ListAsync(client, founder);
        JsonElement second = await ListAsync(client, founder, "?page=2");

        first.GetProperty("items").GetArrayLength().ShouldBe(25);
        first.GetProperty("total").GetInt32().ShouldBe(27);
        first.GetProperty("pageSize").GetInt32().ShouldBe(25);
        second.GetProperty("page").GetInt32().ShouldBe(2);
        second.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("fullName").GetString()).ShouldBe(["Öğrenci 26", "Öğrenci 27"]);
    }
}
