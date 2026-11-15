using FluentValidation;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Application.Validators;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Auth;

/// <summary>
/// Kayıt: kullanıcı yokken de açılır ve normal kullanıcı olur (ilk kayıt yönetici OLMAZ); eşzamanlı kayıtların hiçbiri yönetici olmaz;
/// aynı kullanıcı adı ikinci kez alınamaz; doğrulama kuralları; kayıt sonrası giriş çalışır.
/// </summary>
public sealed class RegisterTests
{
    /// <summary>Boş veritabanında kayıt çalışır, token döner ve kullanıcı yönetici değildir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task First_registration_creates_normal_user_with_tokens()
    {
        await using var host = new IdentityTestHost();

        var status = await host.RunAsync<IAuthService, Result<RegistrationStatusResponse>>(s => s.GetRegistrationStatusAsync());
        status.Value.RegistrationOpen.ShouldBeTrue();

        var result = await host.RegisterAsync("ilkuye");

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        var users = await host.RunAsync<IUserDirectory, IReadOnlyList<UserSummaryDto>>(d => d.ListAsync());
        users.Single().IsAdmin.ShouldBeFalse();
        users.Single().LastLoginAtUtc.ShouldNotBeNull();
    }

    /// <summary>Kayıtlı kullanıcının izinlerinde yönetim yoktur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Registered_user_has_no_admin_permission()
    {
        await using var host = new IdentityTestHost();
        await host.RegisterAsync("uye1");
        host.CurrentUser.UserId = (await host.RunAsync<IUserDirectory, IReadOnlyList<UserSummaryDto>>(d => d.ListAsync())).Single().Id;

        var me = await host.RunAsync<IUserProfileService, Result<CurrentUserResponse>>(s => s.GetMeAsync());

        me.Value.IsAdmin.ShouldBeFalse();
        me.Value.Permissions.ShouldNotContain(PermissionNames.AdminAccess);
    }

    /// <summary>Aynı anda yapılan kayıtların hiçbiri yönetici olmaz; hepsi açılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concurrent_registrations_never_create_an_admin()
    {
        await using var host = new IdentityTestHost();

        var results = await Task.WhenAll(Enumerable.Range(1, 6).Select(i => host.RegisterAsync("yaris" + i)));

        results.ShouldAllBe(r => r.IsSuccess);
        var users = await host.RunAsync<IUserDirectory, IReadOnlyList<UserSummaryDto>>(d => d.ListAsync());
        users.Count.ShouldBe(6);
        users.ShouldAllBe(u => !u.IsAdmin);
    }

    /// <summary>Aynı kullanıcı adıyla ikinci kayıt Türkçe doğrulama hatasıyla reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Duplicate_user_name_is_rejected()
    {
        await using var host = new IdentityTestHost();
        (await host.RegisterAsync("ayni")).IsSuccess.ShouldBeTrue();

        var second = await host.RegisterAsync("ayni");

        second.IsFailure.ShouldBeTrue();
        second.Error.Type.ShouldBe(ErrorType.Validation);
        (await host.RunAsync<IUserDirectory, int>(d => d.CountAsync())).ShouldBe(1);
    }

    /// <summary>Kısa parola Identity tarafından da Türkçe mesajla reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Short_password_returns_turkish_validation_error()
    {
        await using var host = new IdentityTestHost();

        var result = await host.RegisterAsync("taha", "kisa");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("en az 8 karakter");
    }

    /// <summary>Kayıt olan kullanıcı daha sonra giriş yapabilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Registered_user_can_login_later()
    {
        await using var host = new IdentityTestHost();
        await host.RegisterAsync("sonra");

        var login = await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.LoginAsync(new LoginRequest { UserName = "sonra", Password = IdentityTestHost.Password }));

        login.IsSuccess.ShouldBeTrue();
    }

    /// <summary>Doğrulayıcı: kısa kullanıcı adı ve kısa parola reddedilir; geçerli istek (e-posta biçimli ad dahil) geçer.</summary>
    /// <param name="userName">Kullanıcı adı.</param>
    /// <param name="password">Parola.</param>
    /// <param name="valid">Geçerli mi.</param>
    [Theory]
    [InlineData("ab", "Parola123", false)]
    [InlineData("   ", "Parola123", false)]
    [InlineData("taha", "kisa", false)]
    [InlineData("taha", "Parola123", true)]
    [InlineData("taha@example.com", "Parola123", true)]
    public void Validator_applies_same_rules_as_policy(string userName, string password, bool valid)
    {
        IValidator<RegisterRequest> validator = new RegisterRequestValidator();

        validator.Validate(new RegisterRequest { UserName = userName, Password = password }).IsValid.ShouldBe(valid);
    }

    /// <summary>Kayıt kapalıysa hata kodu <c>identity.registration_closed</c> (403) olur.</summary>
    [Fact]
    public void Registration_closed_error_is_forbidden()
    {
        IdentityErrors.RegistrationClosed.Type.ShouldBe(ErrorType.Forbidden);
        IdentityErrors.RegistrationClosed.Code.ShouldBe("identity.registration_closed");
    }
}
