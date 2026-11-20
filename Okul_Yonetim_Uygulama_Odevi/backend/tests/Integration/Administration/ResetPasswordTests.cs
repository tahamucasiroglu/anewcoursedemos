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

/// <summary>Spec 0005 AC-11: kurucunun sıfırlaması yeni geçici şifreyi bir kez verir, eski şifre ve bütün oturumlar geçersiz; başka okulda da kayıtlı kişide yapılamaz.</summary>
[Trait("Category", "Integration")]
public sealed class ResetPasswordTests
{
    private readonly TestDatabase database;

    public ResetPasswordTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Reset_GivesNewTemporaryPassword_InvalidatesOldPasswordAndAllSessions()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("ogretmen");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        JsonElement session1 = await LoginAsync(client, person);
        JsonElement session2 = await LoginAsync(client, person);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        var (_, before, _) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{personId}", founder);
        before.GetProperty("canResetPassword").GetBoolean().ShouldBeTrue();

        var (status, result, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/reset-password", founder);

        status.ShouldBe(HttpStatusCode.OK);
        string temporary = result.GetProperty("temporaryPassword").GetString()!;
        temporary.ShouldMatch(TemporaryPasswordPattern);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", AccessToken(session1))).ShouldBe(HttpStatusCode.Unauthorized);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", AccessToken(session2))).ShouldBe(HttpStatusCode.Unauthorized);
        using (HttpResponseMessage old = await PostLoginAsync(client, person, Password))
        {
            old.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        (await LoginAsync(client, person, temporary)).GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
        var (_, card, cardRaw) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{personId}", founder);
        cardRaw.ShouldNotContain(temporary);
        card.GetProperty("isActive").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Reset_PersonInAnotherSchool_IsRejectedWithExplanation()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string person = UniqueEmail("iki-okul");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, person, b.TenantId, Role.Guardian, b.BranchId);
        string hashBefore = await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.PasswordHash).SingleAsync());
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        var (_, card, _) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{personId}", founder);
        card.GetProperty("canResetPassword").GetBoolean().ShouldBeFalse();

        var (status, problem, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/reset-password", founder);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu kişi başka bir okulda da kayıtlı; şifresini kendisi 'Şifremi unuttum' ile yenilemeli.");
        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.PasswordHash).SingleAsync())).ShouldBe(hashBefore);
    }
}
