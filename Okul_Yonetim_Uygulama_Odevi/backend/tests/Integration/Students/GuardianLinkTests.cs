using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-6 (yeni veli: hesap + geçici şifre bir kez + şubede veli rolü + girişte çocuk görünür + alan hataları),
/// AC-7 (var olan kişi: hesap açılmaz, şifre/ad değişmez; pasif kişi, ikinci bağ ve 5. veli reddedilir) ve AC-8 (bağ
/// kaldırma: sonraki istekte 404; hesap, roller ve diğer bağ aynı; velisiz öğrenci).
/// </summary>
[Trait("Category", "Integration")]
public sealed class GuardianLinkTests
{
    private readonly TestDatabase database;

    public GuardianLinkTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Link_NewEmail_OpensAccountOnce_GrantsGuardianRoleInBranch_GuardianSeesChild()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, founder, "Avcılar");
        Guid id = await CreateStudentAsync(client, founder, NewStudent(avcilar, "Deniz Ay"));
        string email = UniqueEmail("anne");

        JsonElement result = await LinkAsync(client, founder, id, Guardian(email.ToUpperInvariant(), "Anne Ay", "Mother", "0532 111 22 33"));

        result.GetProperty("existingAccount").GetBoolean().ShouldBeFalse();
        string password = result.GetProperty("temporaryPassword").GetString()!;
        password.ShouldMatch(TemporaryPasswordPattern);
        Guid userId = result.GetProperty("userId").GetGuid();
        var (_, card, cardRaw) = await CallAsync(client, HttpMethod.Get, $"/api/admin/students/{id}", founder);
        cardRaw.ShouldNotContain(password);
        JsonElement guardian = card.GetProperty("guardians").EnumerateArray().ShouldHaveSingleItem();
        guardian.GetProperty("userId").GetGuid().ShouldBe(userId);
        guardian.GetProperty("fullName").GetString().ShouldBe("Anne Ay");
        guardian.GetProperty("email").GetString().ShouldBe(email);
        guardian.GetProperty("phone").GetString().ShouldBe("+905321112233");
        guardian.GetProperty("relationship").GetString().ShouldBe("Mother");
        (await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{userId}", founder)).Raw.ShouldNotContain(password);

        List<RoleAssignment> roles = await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().Where(r => r.UserId == userId).ToListAsync());
        roles.ShouldHaveSingleItem();
        roles[0].Role.ShouldBe(Role.Guardian);
        roles[0].BranchId.ShouldBe(avcilar);
        roles[0].TenantId.ShouldBe(a.TenantId);

        string token = await TokenForSchoolAsync(client, email, a.TenantId, password);
        var (_, children, _) = await CallAsync(client, HttpMethod.Get, "/api/my/children", token);
        JsonElement child = children.EnumerateArray().ShouldHaveSingleItem();
        child.GetProperty("id").GetGuid().ShouldBe(id);
        child.GetProperty("fullName").GetString().ShouldBe("Deniz Ay");
        child.GetProperty("branchName").GetString().ShouldBe("Avcılar");
    }

    [Theory]
    [InlineData("", "EMAIL", null, "Mother", "Ad soyad zorunludur")]
    [InlineData("Veli", "", null, "Mother", "E-posta zorunludur")]
    [InlineData("Veli", "gecersiz-eposta", null, "Mother", "Geçerli bir e-posta girin")]
    [InlineData("Veli", "EMAIL", "12345", "Mother", "Geçerli bir telefon numarası girin")]
    [InlineData("Veli", "EMAIL", null, null, "Yakınlık seçin")]
    [InlineData("Veli", "EMAIL", null, "Teyze", "Yakınlık seçin")]
    [InlineData("Veli", "EMAIL", null, "1", "Yakınlık seçin")]
    public async Task Link_InvalidField_Returns400_AndCreatesNothing(string fullName, string email, string? phone, string? relationship, string message)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId));
        string address = email == "EMAIL" ? UniqueEmail("gecersiz") : email;

        var (status, problem, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/guardians", founder, new { fullName, email = address, phone, relationship });

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain(message);
        (await QueryAsync(factory, c => c.Memberships.IgnoreQueryFilters().CountAsync(m => m.TenantId == a.TenantId))).ShouldBe(1);
        (await QueryAsync(factory, c => c.StudentGuardians.IgnoreQueryFilters().CountAsync(g => g.StudentId == id))).ShouldBe(0);
    }

    [Fact]
    public async Task Link_ExistingAccounts_NoNewAccount_PasswordAndNameUnchanged_SeesChildWithOwnPassword()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        (SchoolSeed b, _) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Ece Gül"));
        string elsewhere = UniqueEmail("baskaokul");
        Guid elsewhereId = await AddMemberAsync(factory, elsewhere, b.TenantId, Role.Teacher, b.BranchId);
        string teacher = UniqueEmail("ogretmen");
        Guid teacherId = await AddMemberAsync(factory, teacher, a.TenantId, Role.Teacher, a.BranchId);
        foreach ((string email, Guid userId) in new[] { (elsewhere, elsewhereId), (teacher, teacherId) })
        {
            var (status, result, raw) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/guardians", founder, Guardian(email, "Farklı Ad", "Father"));
            status.ShouldBe(HttpStatusCode.OK, raw);
            result.GetProperty("existingAccount").GetBoolean().ShouldBeTrue();
            result.GetProperty("temporaryPassword").ValueKind.ShouldBe(JsonValueKind.Null);
            result.GetProperty("userId").GetGuid().ShouldBe(userId);
            User user = await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == userId));
            user.FullName.ShouldBe("Test Kişi", "ad değişmez");

            string token = await TokenForSchoolAsync(client, email, a.TenantId, Password);
            Ids((await CallAsync(client, HttpMethod.Get, "/api/my/children", token)).Body).ShouldBe([id]);
        }

        string[] emails = [EmailAddress.Normalize(elsewhere), EmailAddress.Normalize(teacher)];
        (await QueryAsync(factory, c => c.Users.CountAsync(u => emails.Contains(u.Email)))).ShouldBe(2, "yeni hesap açılmadı");
        List<RoleAssignment> teacherRoles = await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().Where(r => r.UserId == teacherId).ToListAsync());
        teacherRoles.Select(r => r.Role).ShouldBe([Role.Teacher, Role.Guardian], ignoreOrder: true);
        (await QueryAsync(factory, c => c.Memberships.IgnoreQueryFilters().CountAsync(m => m.UserId == elsewhereId))).ShouldBe(2);
    }

    [Fact]
    public async Task Link_InactiveMember_DuplicateAndFifthGuardian_AreRejected_NothingChanges()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId));
        string passive = UniqueEmail("pasif");
        Guid passiveId = await AddMemberAsync(factory, passive, a.TenantId, Role.Teacher, a.BranchId);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{passiveId}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);

        var (s1, p1, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/guardians", founder, Guardian(passive));
        s1.ShouldBe(HttpStatusCode.BadRequest);
        Detail(p1).ShouldBe("Bu kişi bu okulda pasif; önce Yönetim'den etkinleştirin.");
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.UserId == passiveId))).ShouldBe(1);

        string first = UniqueEmail("ilk");
        await LinkAsync(client, founder, id, Guardian(first));
        var (s2, p2, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/guardians", founder, Guardian(first.ToUpperInvariant(), relationship: "Father"));
        s2.ShouldBe(HttpStatusCode.BadRequest);
        Detail(p2).ShouldBe("Bu veli bu öğrenciye zaten bağlı");

        for (int i = 0; i < 3; i++)
        {
            await LinkAsync(client, founder, id, Guardian(UniqueEmail("veli")));
        }

        string fifth = UniqueEmail("besinci");
        var (s3, p3, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/guardians", founder, Guardian(fifth));
        s3.ShouldBe(HttpStatusCode.BadRequest);
        Detail(p3).ShouldBe("Bir öğrencinin en fazla 4 velisi olabilir");
        string fifthNormalized = EmailAddress.Normalize(fifth);
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Email == fifthNormalized))).ShouldBe(0, "5. veli için hesap açılmadı");
        (await CardAsync(client, founder, id)).GetProperty("guardians").GetArrayLength().ShouldBe(4);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{Guid.CreateVersion7()}/guardians", founder, Guardian(UniqueEmail("x")))).ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unlink_GuardianLosesAccessNextRequest_AccountRolesAndOtherLinkUnchanged()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid first = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Birinci Çocuk"));
        Guid second = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "İkinci Çocuk"));
        (string token, Guid guardianId, string email, _) = await NewGuardianAsync(client, founder, a.TenantId, first);
        await LinkAsync(client, founder, second, Guardian(email));
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{first}", token)).ShouldBe(HttpStatusCode.OK);

        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/students/{first}/guardians/{guardianId}", founder)).ShouldBe(HttpStatusCode.OK);

        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{first}", token)).ShouldBe(HttpStatusCode.NotFound, "aynı oturumla bir sonraki istek");
        Ids((await CallAsync(client, HttpMethod.Get, "/api/my/children", token)).Body).ShouldBe([second]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{second}", token)).ShouldBe(HttpStatusCode.OK);
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.UserId == guardianId && r.Role == Role.Guardian))).ShouldBe(1);
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Id == guardianId))).ShouldBe(1);
        JsonElement card = await CardAsync(client, founder, first);
        card.GetProperty("guardians").GetArrayLength().ShouldBe(0, "velisiz öğrenci olabilir");
        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/students/{first}/guardians/{guardianId}", founder)).ShouldBe(HttpStatusCode.NotFound);
    }
}
