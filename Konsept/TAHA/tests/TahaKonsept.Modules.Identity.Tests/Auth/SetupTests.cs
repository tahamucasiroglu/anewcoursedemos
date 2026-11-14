using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Auth;

/// <summary>
/// Tek seferlik kurulum: kullanıcı yokken kurulum gerekli; kurulum token döner ve yönetici oluşturur; ikinci kurulum reddedilir.
/// </summary>
public sealed class SetupTests
{
    /// <summary>Boş veritabanında kurulum gerekir; kurulumdan sonra gerekmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Setup_status_flips_after_first_setup()
    {
        await using var host = new IdentityTestHost();

        var before = await host.RunAsync<IAuthService, Result<SetupStatusResponse>>(s => s.GetSetupStatusAsync());
        before.Value.SetupRequired.ShouldBeTrue();

        var (tokens, _) = await host.SetupAdminAsync();
        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        tokens.AccessTokenExpiresAtUtc.ShouldBe(host.Clock.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(1));

        var after = await host.RunAsync<IAuthService, Result<SetupStatusResponse>>(s => s.GetSetupStatusAsync());
        after.Value.SetupRequired.ShouldBeFalse();
    }

    /// <summary>Kurulum yalnızca bir kez yapılabilir (kayıt kapalı).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Second_setup_is_rejected_with_conflict()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();

        var second = await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.SetupAsync(new SetupRequest { UserName = "baska", Password = IdentityTestHost.Password }));

        second.IsFailure.ShouldBeTrue();
        second.Error.ShouldBe(IdentityErrors.SetupCompleted);
        (await host.RunAsync<IUserDirectory, int>(d => d.CountAsync())).ShouldBe(1);
    }

    /// <summary>Kurulumda açılan hesap yöneticidir ve tüm izinlere sahiptir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Setup_user_is_admin_with_all_permissions()
    {
        await using var host = new IdentityTestHost();
        var (_, userId) = await host.SetupAdminAsync();
        host.CurrentUser.UserId = userId;

        var me = await host.RunAsync<IUserProfileService, Result<CurrentUserResponse>>(s => s.GetMeAsync());

        me.IsSuccess.ShouldBeTrue();
        me.Value.UserName.ShouldBe(IdentityTestHost.AdminUserName);
        me.Value.IsAdmin.ShouldBeTrue();
        me.Value.Permissions.ShouldBe(PermissionNames.All, ignoreOrder: true);
    }

    /// <summary>Kısa parola Identity tarafından Türkçe mesajla reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Setup_with_short_password_returns_turkish_validation_error()
    {
        await using var host = new IdentityTestHost();

        var result = await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.SetupAsync(new SetupRequest { UserName = "taha", Password = "kisa" }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("en az 8 karakter");
    }
}
