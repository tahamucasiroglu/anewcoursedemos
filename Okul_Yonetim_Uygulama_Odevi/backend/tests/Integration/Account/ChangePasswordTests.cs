using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Account.AccountTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Account;

/// <summary>Spec 0006 AC-3, AC-4: şifremi değiştir — yeni şifre geçerli, diğer oturumlar kapanır, bu oturum sürer; hatalar ve kilit.</summary>
[Trait("Category", "Integration")]
public sealed class ChangePasswordTests
{
    private readonly TestDatabase database;

    public ChangePasswordTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Change_NewPasswordWorks_OtherSessionsClosed_ThisSessionContinues()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("degistir");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        JsonElement other = await LoginAsync(client, person);
        JsonElement mine = await LoginAsync(client, person);

        (await StatusAsync(client, HttpMethod.Post, "/api/account/change-password", AccessToken(mine), new { currentPassword = Password, newPassword = NewPassword, newPasswordRepeat = NewPassword }))
            .ShouldBe(HttpStatusCode.NoContent);

        (await StatusAsync(client, HttpMethod.Get, "/api/me", AccessToken(mine))).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", AccessToken(other))).ShouldBe(HttpStatusCode.Unauthorized);
        using (HttpResponseMessage old = await PostLoginAsync(client, person, Password))
        {
            old.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        (await LoginAsync(client, person, NewPassword)).GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task WrongCurrent_CountsTowardLockout_ValidationErrorsChangeNothing()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("kilit");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        string token = AccessToken(await LoginAsync(client, person));
        string hashBefore = await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.PasswordHash).SingleAsync());

        foreach (object invalid in new object[]
        {
            new { currentPassword = Password, newPassword = "kisa", newPasswordRepeat = "kisa" },
            new { currentPassword = Password, newPassword = NewPassword, newPasswordRepeat = NewPassword + "x" },
            new { currentPassword = Password, newPassword = Password, newPasswordRepeat = Password },
            new { currentPassword = "", newPassword = NewPassword, newPasswordRepeat = NewPassword },
        })
        {
            (await StatusAsync(client, HttpMethod.Post, "/api/account/change-password", token, invalid)).ShouldBe(HttpStatusCode.BadRequest);
        }

        var (wrong, problem, _) = await CallAsync(client, HttpMethod.Post, "/api/account/change-password", token, new { currentPassword = "Yanlis-Sifre-1", newPassword = NewPassword, newPasswordRepeat = NewPassword });
        wrong.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Mevcut şifre hatalı");
        using (HttpResponseMessage loginFail = await PostLoginAsync(client, person, "Yanlis-Sifre-2"))
        {
            loginFail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var (third, lockedProblem, _) = await CallAsync(client, HttpMethod.Post, "/api/account/change-password", token, new { currentPassword = "Yanlis-Sifre-3", newPassword = NewPassword, newPasswordRepeat = NewPassword });
        third.ShouldBe(HttpStatusCode.BadRequest);
        Detail(lockedProblem).ShouldBe("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.");
        var (stillLocked, stillProblem, _) = await CallAsync(client, HttpMethod.Post, "/api/account/change-password", token, new { currentPassword = Password, newPassword = NewPassword, newPasswordRepeat = NewPassword });
        stillLocked.ShouldBe(HttpStatusCode.BadRequest);
        Detail(stillProblem).ShouldBe("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.");
        using (HttpResponseMessage lockedLogin = await PostLoginAsync(client, person, Password))
        {
            lockedLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.PasswordHash).SingleAsync())).ShouldBe(hashBefore);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", token)).ShouldBe(HttpStatusCode.OK, "yanlış mevcut şifre oturumu kapatmaz");
    }
}
