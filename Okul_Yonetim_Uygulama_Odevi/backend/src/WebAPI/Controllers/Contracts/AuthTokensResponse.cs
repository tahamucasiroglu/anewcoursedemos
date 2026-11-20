using OkulYonetim.Application.Features.Identity;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Giriş / yenileme / okul seçimi yanıtı: erişim anahtarı ve bitişi; yenileme anahtarı yalnızca mobil (gövde) isteğinde
/// dolu, web'de boş (çerezde); okul seçimi gerekiyor mu; kişinin okulları. Platform yöneticisinde (spec 0007) oturum
/// yerine iki adımlı doğrulama beklemesi (<c>TwoFactorChallenge</c>; kurulum gerekiyorsa <c>TwoFactorSetupRequired</c>).
/// </summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    bool RequiresTenantSelection,
    IReadOnlyList<TenantSummary> Tenants,
    string? TwoFactorChallenge = null,
    bool TwoFactorSetupRequired = false)
{
    public static AuthTokensResponse From(AuthResult result, bool includeRefreshToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new(result.AccessToken, result.AccessTokenExpiresAt, includeRefreshToken ? result.RefreshToken : null, result.RequiresTenantSelection, result.Tenants, result.TwoFactorChallenge, result.TwoFactorSetupRequired);
    }
}
