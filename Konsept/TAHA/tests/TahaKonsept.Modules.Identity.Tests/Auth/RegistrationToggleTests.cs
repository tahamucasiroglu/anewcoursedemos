using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Auth;

/// <summary>
/// "Yeni kayıtlara izin ver" ayarı: varsayılan açık; yalnız yönetici okuyup değiştirebilir; kapalıyken kayıt reddedilir ve durum
/// uçları kapalı gösterir; tekrar açılınca kayıt çalışır; ayar kalıcıdır (yeni scope'ta da aynı).
/// </summary>
public sealed class RegistrationToggleTests
{
    /// <summary>Ayar satırı yokken kayıt açıktır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Registration_is_open_by_default()
    {
        await using var host = new IdentityTestHost();
        (await host.RunAsync<IRegistrationSettingsService, bool>(s => s.IsRegistrationOpenAsync())).ShouldBeTrue();
    }

    /// <summary>Yönetici kapatınca kayıt reddedilir; açınca yeniden çalışır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Closing_registration_rejects_new_accounts_until_reopened()
    {
        await using var host = new IdentityTestHost();
        AsAdmin(host);

        (await SetOpen(host, false)).Value.RegistrationOpen.ShouldBeFalse();
        (await host.RunAsync<IRegistrationSettingsService, Result<RegistrationSettingsDto>>(s => s.GetAsync())).Value.RegistrationOpen.ShouldBeFalse();
        (await host.RunAsync<IAuthService, Result<RegistrationStatusResponse>>(s => s.GetRegistrationStatusAsync())).Value.RegistrationOpen.ShouldBeFalse();

        var rejected = await host.RegisterAsync("kapali");
        rejected.Error.ShouldBe(IdentityErrors.RegistrationClosed);
        (await host.RunAsync<IUserDirectory, int>(d => d.CountAsync())).ShouldBe(0);

        (await SetOpen(host, true)).Value.RegistrationOpen.ShouldBeTrue();
        (await host.RegisterAsync("acik")).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Yönetici olmayan kullanıcı ayarı okuyamaz/değiştiremez; oturumsuz istek NotAuthenticated.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Non_admin_cannot_read_or_change_setting()
    {
        await using var host = new IdentityTestHost();

        (await SetOpen(host, false)).Error.ShouldBe(IdentityErrors.NotAuthenticated);

        host.CurrentUser.UserId = Guid.NewGuid();
        host.CurrentUser.Permissions = [PermissionNames.ModelsUse];
        (await SetOpen(host, false)).Error.ShouldBe(IdentityErrors.AdminRequired);
        (await host.RunAsync<IRegistrationSettingsService, Result<RegistrationSettingsDto>>(s => s.GetAsync())).Error.ShouldBe(IdentityErrors.AdminRequired);
        (await host.RunAsync<IRegistrationSettingsService, bool>(s => s.IsRegistrationOpenAsync())).ShouldBeTrue();
    }

    /// <summary>Aynı değeri tekrar kaydetmek sorun çıkarmaz (güncelleme yolu).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Saving_twice_updates_existing_row()
    {
        await using var host = new IdentityTestHost();
        AsAdmin(host);

        await SetOpen(host, false);
        await SetOpen(host, false);
        (await SetOpen(host, true)).IsSuccess.ShouldBeTrue();

        (await host.RunAsync<IRegistrationSettingsService, bool>(s => s.IsRegistrationOpenAsync())).ShouldBeTrue();
    }

    private static void AsAdmin(IdentityTestHost host)
    {
        host.CurrentUser.UserId = Guid.NewGuid();
        host.CurrentUser.Permissions = PermissionNames.All.ToArray();
    }

    private static Task<Result<RegistrationSettingsDto>> SetOpen(IdentityTestHost host, bool open) =>
        host.RunAsync<IRegistrationSettingsService, Result<RegistrationSettingsDto>>(
            s => s.UpdateAsync(new RegistrationSettingsDto { RegistrationOpen = open }));
}
