using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Oturumdaki kullanıcının bilgileri ve tercihleri (<c>api/identity/me</c>, <c>api/identity/me/preferences</c>).
/// </summary>
public interface IUserProfileService : IApplicationService
{
    /// <summary>Kullanıcı bilgisi + izinler + tercihler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kullanıcı.</returns>
    Task<Result<CurrentUserResponse>> GetMeAsync(CancellationToken ct = default);

    /// <summary>Tercihleri okur.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Tercihler.</returns>
    Task<Result<UserPreferencesDto>> GetPreferencesAsync(CancellationToken ct = default);

    /// <summary>Tercihleri kaydeder.</summary>
    /// <param name="preferences">Yeni tercihler.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kaydedilen tercihler.</returns>
    Task<Result<UserPreferencesDto>> UpdatePreferencesAsync(UserPreferencesDto preferences, CancellationToken ct = default);
}
