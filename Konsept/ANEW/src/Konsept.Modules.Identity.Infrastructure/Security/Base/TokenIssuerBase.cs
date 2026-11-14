using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;

namespace Konsept.Modules.Identity.Infrastructure.Security.Base;

/// <summary>
/// Erişim token'ı üretiminin ortak kuralı: ömür <see cref="JwtSettings.AccessTokenLifetime"/>, zaman çağırandan
/// (<see cref="TimeProvider"/>) gelir. İmzalama alt sınıftadır.
/// </summary>
public abstract class TokenIssuerBase : ITokenIssuer
{
    /// <inheritdoc />
    public IssuedAccessToken Issue(Guid userId, string name, Guid sessionId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(name);
        var expiresAt = now.ToUniversalTime() + JwtSettings.AccessTokenLifetime;
        return new IssuedAccessToken(Sign(userId, name, sessionId, now.ToUniversalTime(), expiresAt), expiresAt);
    }

    /// <summary>İmzalı token üretir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="name">Ad.</param>
    /// <param name="sessionId">Oturum.</param>
    /// <param name="issuedAt">Üretim zamanı (UTC).</param>
    /// <param name="expiresAt">Bitiş zamanı (UTC).</param>
    /// <returns>Token.</returns>
    protected abstract string Sign(Guid userId, string name, Guid sessionId, DateTimeOffset issuedAt, DateTimeOffset expiresAt);
}
