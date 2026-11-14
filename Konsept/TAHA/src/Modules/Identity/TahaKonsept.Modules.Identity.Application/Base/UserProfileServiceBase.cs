using Microsoft.AspNetCore.Identity;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IUserProfileService"/> tabanı: oturumdaki kullanıcıyı yükleyen <see cref="LoadCurrentUserAsync"/> yardımcısı
/// (oturum yok → Unauthorized, kullanıcı silinmiş → NotFound).
/// </summary>
public abstract class UserProfileServiceBase : IUserProfileService
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    protected UserProfileServiceBase(UserManager<AppUser> users, ICurrentUser currentUser)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Kullanıcı yöneticisi.</summary>
    protected UserManager<AppUser> Users { get; }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <inheritdoc />
    public abstract Task<Result<CurrentUserResponse>> GetMeAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<UserPreferencesDto>> GetPreferencesAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<UserPreferencesDto>> UpdatePreferencesAsync(UserPreferencesDto preferences, CancellationToken ct = default);

    /// <summary>Oturumdaki kullanıcıyı yükler.</summary>
    /// <returns>Kullanıcı ya da hata.</returns>
    protected async Task<Result<AppUser>> LoadCurrentUserAsync()
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return IdentityErrors.NotAuthenticated;
        }

        var user = await Users.FindByIdAsync(userId.ToString());
        return user is null ? IdentityErrors.UserNotFound : user;
    }
}
