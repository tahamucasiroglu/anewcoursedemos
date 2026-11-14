using Microsoft.AspNetCore.Identity;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Base;
using TahaKonsept.Modules.Identity.Application.Mapping;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Oturumdaki kullanıcının bilgisi (<see cref="GetMeAsync"/>: kimlik, ad, yöneticilik, izinler, tercihler) ve
/// tema tercihinin okunması/kaydedilmesi.
/// </summary>
public sealed class UserProfileService : UserProfileServiceBase
{
    private readonly IPermissionResolver _permissions;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="permissions">İzin çözücü.</param>
    public UserProfileService(UserManager<AppUser> users, ICurrentUser currentUser, IPermissionResolver permissions)
        : base(users, currentUser)
    {
        _permissions = permissions;
    }

    /// <inheritdoc />
    public override async Task<Result<CurrentUserResponse>> GetMeAsync(CancellationToken ct = default)
    {
        var loaded = await LoadCurrentUserAsync();
        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var user = loaded.Value;
        var access = await _permissions.ResolveAsync(user);
        return new CurrentUserResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            IsAdmin = access.IsAdmin,
            Permissions = access.Permissions,
            Preferences = ToDto(user),
        };
    }

    /// <inheritdoc />
    public override async Task<Result<UserPreferencesDto>> GetPreferencesAsync(CancellationToken ct = default) =>
        (await LoadCurrentUserAsync()).Map(ToDto);

    /// <inheritdoc />
    public override async Task<Result<UserPreferencesDto>> UpdatePreferencesAsync(UserPreferencesDto preferences, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var loaded = await LoadCurrentUserAsync();
        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var user = loaded.Value;
        user.SetTheme(IdentityEnumMapper.ToDomain(preferences.Theme));
        await Users.UpdateAsync(user);
        return ToDto(user);
    }

    private static UserPreferencesDto ToDto(AppUser user) => new() { Theme = IdentityEnumMapper.ToContract(user.Theme) };
}
