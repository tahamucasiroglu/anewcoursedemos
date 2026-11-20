using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>Spec 0005 AC-5, AC-6: yeni kişi (geçici şifre bir kez, yalnız atanan şube, alan hataları, telefon biçimi); var olan kişi (rol eklenir, şifre değişmez); aynı okulda tekrar reddi.</summary>
[Trait("Category", "Integration")]
public sealed class AddMemberTests
{
    private readonly TestDatabase database;

    public AddMemberTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task NewPerson_GetsOneTimeTemporaryPassword_LogsInAndSeesOnlyAssignedBranch()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid other = await CreateBranchAsync(client, token, "Öbür Şube");
        string email = UniqueEmail("yeni");

        var (status, result, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember("  " + email.ToUpperInvariant() + " ", "Ali Veli", "0532 123 45 67", Teacher(a.BranchId)));

        status.ShouldBe(HttpStatusCode.OK);
        result.GetProperty("existingAccount").GetBoolean().ShouldBeFalse();
        string temporary = result.GetProperty("temporaryPassword").GetString()!;
        temporary.ShouldMatch(TemporaryPasswordPattern);
        Guid userId = result.GetProperty("userId").GetGuid();
        var (_, card, cardRaw) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{userId}", token);
        cardRaw.ShouldNotContain(temporary);
        card.GetProperty("phone").GetString().ShouldBe("+905321234567");
        card.GetProperty("email").GetString().ShouldBe(email);
        var (_, list, listRaw) = await CallAsync(client, HttpMethod.Get, "/api/admin/members", token);
        listRaw.ShouldNotContain(temporary);

        JsonElement auth = await LoginAsync(client, email, temporary);
        auth.GetProperty("requiresTenantSelection").GetBoolean().ShouldBeFalse();
        var (_, branches, _) = await CallAsync(client, HttpMethod.Get, "/api/branches", AccessToken(auth));
        branches.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ShouldBe([a.BranchId]);
        (await StatusAsync(client, HttpMethod.Get, $"/api/branches/{other}", AccessToken(auth))).ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("", "gecerli@ornek.test", null, true)]
    [InlineData("Ad Soyad", "", null, true)]
    [InlineData("Ad Soyad", "gecersiz-eposta", null, true)]
    [InlineData("Ad Soyad", "gecerli@ornek.test", "12345", true)]
    [InlineData("Ad Soyad", "gecerli@ornek.test", null, false)]
    public async Task InvalidFields_Return400_AndCreateNothing(string fullName, string email, string? phone, bool withRole)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string unique = email.Length == 0 || !email.Contains('@', StringComparison.Ordinal) ? email : UniqueEmail("gecersiz");
        object body = withRole
            ? new { fullName, email = unique, phone, roles = new[] { Teacher(a.BranchId) } }
            : new { fullName, email = unique, phone, roles = Array.Empty<object>() };

        (await StatusAsync(client, HttpMethod.Post, "/api/admin/members", token, body)).ShouldBe(HttpStatusCode.BadRequest);

        (await QueryAsync(factory, c => c.Memberships.IgnoreQueryFilters().CountAsync(m => m.TenantId == a.TenantId))).ShouldBe(1);
    }

    /// <summary>Review round 2, L-A: aynı yeni kişi aynı anda iki kez eklenirse biri eklenir, öbürü anlaşılır 400 alır (500 değil).</summary>
    [Fact]
    public async Task SameNewPersonAddedTwiceAtOnce_OneSucceeds_OtherGets400()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        string email = UniqueEmail("cift");

        HttpStatusCode[] results = await Task.WhenAll(
            StatusAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(email, roles: Teacher(a.BranchId))),
            StatusAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(email, roles: Teacher(a.BranchId))));

        results.ShouldBe([HttpStatusCode.OK, HttpStatusCode.BadRequest], ignoreOrder: true);
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Email == email))).ShouldBe(1);
        factory.ReadLog().ShouldNotContain("Beklenmeyen hata");
    }

    [Fact]
    public async Task RoleWithoutBranch_IsRejected()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        var (status, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(UniqueEmail(), roles: Teacher()));

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu rol için en az bir şube seçin");
    }

    [Fact]
    public async Task ExistingPerson_GetsRoleOnly_PasswordUnchanged_SameSchoolDuplicateRejected()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string email = UniqueEmail("var-olan");
        Guid userId = await AddMemberAsync(factory, email, b.TenantId, Role.Teacher, b.BranchId);
        string hashBefore = await QueryAsync(factory, c => c.Users.Where(u => u.Id == userId).Select(u => u.PasswordHash).SingleAsync());
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        var (status, result, raw) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(email, "Başka Ad", null, Teacher(a.BranchId)));

        status.ShouldBe(HttpStatusCode.OK);
        result.GetProperty("existingAccount").GetBoolean().ShouldBeTrue();
        result.GetProperty("temporaryPassword").ValueKind.ShouldBe(JsonValueKind.Null);
        raw.ShouldNotContain(b.TenantId.ToString());
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Email == email))).ShouldBe(1);
        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == userId).Select(u => u.PasswordHash).SingleAsync())).ShouldBe(hashBefore);
        JsonElement auth = await LoginAsync(client, email);
        auth.GetProperty("tenants").GetArrayLength().ShouldBe(2);

        var (dup, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(" " + email.ToUpperInvariant(), roles: Teacher(a.BranchId)));
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu kişi bu okulda zaten kayıtlı");
    }
}
