using OkulYonetim.Application.Features.Identity;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Giriş / yenileme / okul seçimi yanıtı: erişim anahtarı ve bitişi; yenileme anahtarı yalnızca mobil (gövde) isteğinde
/// dolu, web'de boş (çerezde); okul seçimi gerekiyor mu; kişinin okulları.
/// </summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    bool RequiresTenantSelection,
    IReadOnlyList<TenantSummary> Tenants)
{
    public static AuthTokensResponse From(AuthResult result, bool includeRefreshToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new(result.AccessToken, result.AccessTokenExpiresAt, includeRefreshToken ? result.RefreshToken : null, result.RequiresTenantSelection, result.Tenants);
    }
}
