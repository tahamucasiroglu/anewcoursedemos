namespace TahaKonsept.Modules.Identity.Application.Models;

/// <summary>
/// Üretilen erişim token'ı ve bitiş anı.
/// </summary>
/// <param name="Token">İmzalı JWT.</param>
/// <param name="ExpiresAtUtc">Bitiş anı (UTC).</param>
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);
