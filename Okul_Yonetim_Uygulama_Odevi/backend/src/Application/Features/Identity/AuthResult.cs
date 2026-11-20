namespace OkulYonetim.Application.Features.Identity;

/// <summary>
/// Giriş / yenileme / okul seçimi sonucu: erişim anahtarı ve bitişi, yenileme anahtarı (API onu web'e yalnızca çerezle,
/// mobile gövdede verir), okul seçimi gerekip gerekmediği ve kişinin okulları. Platform yöneticisinde (spec 0007) oturum
/// yerine iki adımlı doğrulama beklemesi döner: erişim anahtarı boş, <c>TwoFactorChallenge</c> dolu.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    bool RequiresTenantSelection,
    IReadOnlyList<TenantSummary> Tenants,
    string? TwoFactorChallenge = null,
    bool TwoFactorSetupRequired = false);
