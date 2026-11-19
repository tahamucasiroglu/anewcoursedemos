namespace OkulYonetim.Application.Features.Identity;

/// <summary>
/// Giriş / yenileme / okul seçimi sonucu: erişim anahtarı ve bitişi, yenileme anahtarı (API onu web'e yalnızca çerezle,
/// mobile gövdede verir), okul seçimi gerekip gerekmediği ve kişinin okulları.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    bool RequiresTenantSelection,
    IReadOnlyList<TenantSummary> Tenants);
