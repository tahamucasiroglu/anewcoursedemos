using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Validators;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Users;

/// <summary>
/// Kullanıcı tercihleri: varsayılan tema sistem; kaydedilen tema okunur; geçersiz tema doğrulamada reddedilir.
/// </summary>
public sealed class PreferencesTests
{
    /// <summary>Tema kaydedilir ve geri okunur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Theme_preference_round_trips()
    {
        await using var host = new IdentityTestHost();
        var (_, userId) = await host.SetupAdminAsync();
        host.CurrentUser.UserId = userId;

        var initial = await host.RunAsync<IUserProfileService, Result<UserPreferencesDto>>(s => s.GetPreferencesAsync());
        initial.Value.Theme.ShouldBe(ThemePreference.System);

        await host.RunAsync<IUserProfileService, Result<UserPreferencesDto>>(s => s.UpdatePreferencesAsync(new UserPreferencesDto { Theme = ThemePreference.Dark }));

        var stored = await host.RunAsync<IUserProfileService, Result<UserPreferencesDto>>(s => s.GetPreferencesAsync());
        stored.Value.Theme.ShouldBe(ThemePreference.Dark);
    }

    /// <summary>Tanımsız tema değeri doğrulamada reddedilir.</summary>
    [Fact]
    public void Undefined_theme_fails_validation()
    {
        var result = new UserPreferencesDtoValidator().Validate(new UserPreferencesDto { Theme = (ThemePreference)42 });

        result.IsValid.ShouldBeFalse();
    }

    /// <summary>Kayıt doğrulayıcısı kısa kullanıcı adı ve parolayı reddeder.</summary>
    [Fact]
    public void Register_validator_rejects_short_values()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest { UserName = "ab", Password = "1234567" });

        result.Errors.Select(e => e.PropertyName).ShouldBe([nameof(RegisterRequest.UserName), nameof(RegisterRequest.Password)], ignoreOrder: true);
    }
}
