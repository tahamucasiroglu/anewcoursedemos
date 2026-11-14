namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Üretilmiş erişim token'ı.</summary>
/// <param name="Token">İmzalı JWT.</param>
/// <param name="ExpiresAt">Bitiş zamanı (UTC).</param>
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
