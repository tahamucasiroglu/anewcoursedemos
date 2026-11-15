using System.Globalization;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IRegistrationSettingsService"/> tabanı: depo, saat ve oturum kullanıcısını tutar; yönetici kontrolü
/// (<see cref="RequireAdmin"/>) ve <c>bool</c> ↔ metin dönüşümü (<see cref="ToText"/>, <see cref="ParseBool"/>) burada.
/// </summary>
public abstract class RegistrationSettingsServiceBase : IRegistrationSettingsService
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <param name="clock">Saat.</param>
    protected RegistrationSettingsServiceBase(IIdentitySettingRepository settings, ICurrentUser currentUser, IClock clock)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Ayar deposu.</summary>
    protected IIdentitySettingRepository Settings { get; }

    /// <summary>Oturum kullanıcısı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <inheritdoc />
    public abstract Task<bool> IsRegistrationOpenAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<RegistrationSettingsDto>> GetAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<RegistrationSettingsDto>> UpdateAsync(RegistrationSettingsDto settings, CancellationToken ct = default);

    /// <summary>Oturum yoksa NotAuthenticated, yönetici değilse AdminRequired; yöneticiyse <c>null</c>.</summary>
    /// <returns>Hata ya da <c>null</c>.</returns>
    protected Error? RequireAdmin()
    {
        if (!CurrentUser.IsAuthenticated)
        {
            return IdentityErrors.NotAuthenticated;
        }

        return CurrentUser.HasPermission(PermissionNames.AdminAccess) ? null : IdentityErrors.AdminRequired;
    }

    /// <summary><c>bool</c> değerini saklama metnine çevirir.</summary>
    /// <param name="value">Değer.</param>
    /// <returns><c>true</c> / <c>false</c>.</returns>
    protected static string ToText(bool value) => value ? "true" : "false";

    /// <summary>Saklama metnini okur; boş/geçersizse varsayılan.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="fallback">Varsayılan.</param>
    /// <returns>Değer.</returns>
    protected static bool ParseBool(string? text, bool fallback) =>
        bool.TryParse(text?.Trim().ToLower(CultureInfo.InvariantCulture), out var value) ? value : fallback;
}
