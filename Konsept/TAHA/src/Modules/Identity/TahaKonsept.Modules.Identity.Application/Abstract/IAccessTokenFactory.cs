using TahaKonsept.Modules.Identity.Application.Models;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// İmzalı JWT erişim token'ı üretir (HMAC-SHA256, süre <c>Jwt:AccessTokenMinutes</c>, varsayılan 15 dk).
/// Claim'ler: <c>sub</c>, <c>name</c>, <c>jti</c>, <c>permission</c>*, <c>role</c>*, <c>tfam</c>, <c>client</c>.
/// </summary>
public interface IAccessTokenFactory
{
    /// <summary>Erişim token'ı üretir.</summary>
    /// <param name="descriptor">Token içeriği.</param>
    /// <returns>Token + bitiş anı.</returns>
    AccessTokenResult Create(AccessTokenDescriptor descriptor);
}
