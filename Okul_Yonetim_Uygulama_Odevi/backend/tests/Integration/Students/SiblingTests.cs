using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-9 (BR-2, C-4): aynı veliye bağlı iki öğrenci, farklı şubelerde bile, birbirinin kartında "Kardeşler" altında
/// görünür; bağ kaldırılınca kardeşlik de kalkar; veli tek hesapla iki çocuğunu (farklı şubelerde) görür ve ikisinin kartını
/// açar. Velinin kartında kardeş ve diğer veli bilgisi yoktur.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SiblingTests
{
    private readonly TestDatabase database;

    public SiblingTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task SharedGuardian_MakesSiblingsAcrossBranches_UnlinkRemovesSiblinghood()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, founder, "Avcılar");
        Guid older = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Ablası Ada"));
        Guid younger = await CreateStudentAsync(client, founder, NewStudent(avcilar, "Kardeşi Can"));
        Guid stranger = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Yabancı"));
        (string token, Guid guardianId, string email, _) = await NewGuardianAsync(client, founder, a.TenantId, older);
        JsonElement second = await LinkAsync(client, founder, younger, Guardian(email));
        second.GetProperty("existingAccount").GetBoolean().ShouldBeTrue();
        await LinkAsync(client, founder, stranger, Guardian(IdentityTestData.UniqueEmail("baska")));

        JsonElement olderSibling = (await CardAsync(client, founder, older)).GetProperty("siblings").EnumerateArray().ShouldHaveSingleItem();
        olderSibling.GetProperty("id").GetGuid().ShouldBe(younger);
        olderSibling.GetProperty("fullName").GetString().ShouldBe("Kardeşi Can");
        olderSibling.GetProperty("branchName").GetString().ShouldBe("Avcılar");
        olderSibling.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        Ids((await CardAsync(client, founder, younger)).GetProperty("siblings")).ShouldBe([older]);
        (await CardAsync(client, founder, stranger)).GetProperty("siblings").GetArrayLength().ShouldBe(0);

        Ids((await CallAsync(client, HttpMethod.Get, "/api/my/children", token)).Body).ShouldBe([older, younger], ignoreOrder: true);
        var (s1, card1, raw1) = await CallAsync(client, HttpMethod.Get, $"/api/my/children/{older}", token);
        s1.ShouldBe(HttpStatusCode.OK);
        card1.GetProperty("branchName").GetString().ShouldBe("Merkez");
        raw1.ShouldNotContain("siblings", Case.Insensitive);
        raw1.ShouldNotContain("guardians", Case.Insensitive);
        raw1.ShouldNotContain(email);
        var (s2, card2, _) = await CallAsync(client, HttpMethod.Get, $"/api/my/children/{younger}", token);
        s2.ShouldBe(HttpStatusCode.OK);
        card2.GetProperty("branchName").GetString().ShouldBe("Avcılar");

        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/students/{younger}/guardians/{guardianId}", founder)).ShouldBe(HttpStatusCode.OK);

        (await CardAsync(client, founder, older)).GetProperty("siblings").GetArrayLength().ShouldBe(0);
        (await CardAsync(client, founder, younger)).GetProperty("siblings").GetArrayLength().ShouldBe(0);
    }
}
