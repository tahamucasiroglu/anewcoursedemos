using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-11: kurucu teslim alabilecek kişi ekler, düzenler, kaldırır; ad-soyad, yakınlık ve geçerli telefon
/// zorunludur (eksik/geçersiz → alan hatası, hiçbir şey değişmez); 11. kişi reddedilir; liste kurucunun ve bağlı velinin
/// kartında görünür.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PickupPersonTests
{
    private readonly TestDatabase database;

    public PickupPersonTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task PickupPersons_AddEditRemove_VisibleToFounderAndGuardian()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId));
        (string guardian, _, _, _) = await NewGuardianAsync(client, founder, a.TenantId, id);
        (await CardAsync(client, founder, id)).GetProperty("pickupPersons").GetArrayLength().ShouldBe(0);

        (await Put(client, founder, id, Person("Hasan Dede", "Dede", "0532 000 11 22"), Person("Ayla Teyze", "Teyze", "+90 212 555 66 77"))).ShouldBe(HttpStatusCode.OK);
        Names(await CardAsync(client, founder, id)).ShouldBe(["Hasan Dede|Dede|+905320001122", "Ayla Teyze|Teyze|+902125556677"]);

        (await Put(client, founder, id, Person("Hasan Dede", "Büyükbaba", "05320001122"))).ShouldBe(HttpStatusCode.OK);
        JsonElement founderCard = await CardAsync(client, founder, id);
        Names(founderCard).ShouldBe(["Hasan Dede|Büyükbaba|+905320001122"], "düzenlendi ve biri kaldırıldı");
        Names((await CallAsync(client, HttpMethod.Get, $"/api/my/children/{id}", guardian)).Body).ShouldBe(["Hasan Dede|Büyükbaba|+905320001122"]);

        (await Put(client, founder, id)).ShouldBe(HttpStatusCode.OK);
        (await CardAsync(client, founder, id)).GetProperty("pickupPersons").GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [InlineData("", "Dede", "05320001122", "Ad soyad zorunludur")]
    [InlineData("Hasan", "", "05320001122", "Yakınlık zorunludur")]
    [InlineData("Hasan", "Dede", "", "Telefon zorunludur")]
    [InlineData("Hasan", "Dede", "12345", "Geçerli bir telefon numarası girin")]
    [InlineData("LONG", "Dede", "05320001122", "En fazla 100 karakter olabilir")]
    [InlineData("Hasan", "LONG", "05320001122", "Yakınlık en fazla 50 karakter olabilir")]
    public async Task PickupPersons_InvalidField_Returns400_NothingChanges(string fullName, string relationship, string phone, string message)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId));
        (await Put(client, founder, id, Person("Mevcut Kişi", "Amca", "05321234567"))).ShouldBe(HttpStatusCode.OK);
        object bad = Person(fullName == "LONG" ? new string('a', 101) : fullName, relationship == "LONG" ? new string('b', 51) : relationship, phone);

        var (status, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/pickup-persons", founder, new { persons = new[] { Person("Mevcut Kişi", "Amca", "05321234567"), bad } });

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain(message);
        Names(await CardAsync(client, founder, id)).ShouldBe(["Mevcut Kişi|Amca|+905321234567"]);
    }

    [Fact]
    public async Task PickupPersons_TenAccepted_EleventhRejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId));
        object[] ten = [.. Enumerable.Range(1, 10).Select(i => Person($"Kişi {i}", "Komşu", $"053200000{i:D2}"))];

        (await Put(client, founder, id, ten)).ShouldBe(HttpStatusCode.OK);
        var (status, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/pickup-persons", founder, new { persons = ten.Append(Person("Kişi 11", "Komşu", "05320000011")).ToArray() });

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain("En fazla 10 kişi eklenebilir");
        (await CardAsync(client, founder, id)).GetProperty("pickupPersons").GetArrayLength().ShouldBe(10);
    }

    private static object Person(string fullName, string relationship, string phone) => new { fullName, relationship, phone };

    private static Task<HttpStatusCode> Put(HttpClient client, string token, Guid id, params object[] persons) =>
        StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/pickup-persons", token, new { persons });

    private static string[] Names(JsonElement card) =>
        [.. card.GetProperty("pickupPersons").EnumerateArray().Select(p => $"{p.GetProperty("fullName").GetString()}|{p.GetProperty("relationship").GetString()}|{p.GetProperty("phone").GetString()}")];
}
