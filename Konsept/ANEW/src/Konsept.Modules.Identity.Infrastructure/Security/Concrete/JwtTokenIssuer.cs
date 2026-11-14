using Konsept.Modules.Identity.Infrastructure.Security.Base;
using Konsept.Shared.Infrastructure.Http.Base;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Konsept.Modules.Identity.Infrastructure.Security.Concrete;

/// <summary>HMAC-SHA256 imzalı JWT üretir (<c>sub</c>, <c>sid</c>, <c>name</c>; veren/hedef sabit).</summary>
/// <param name="settings">JWT ayarları.</param>
public sealed class JwtTokenIssuer(JwtSettings settings) : TokenIssuerBase
{
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    /// <inheritdoc />
    protected override string Sign(Guid userId, string name, Guid sessionId, DateTimeOffset issuedAt, DateTimeOffset expiresAt) =>
        _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtSettings.Issuer,
            Audience = JwtSettings.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [CurrentUserBase.SubjectClaim] = userId.ToString(),
                [CurrentUserBase.SessionClaim] = sessionId.ToString(),
                ["name"] = name,
            },
            SigningCredentials = new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256),
        });
}
