using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Identity.Infrastructure.Security.Base;

namespace TahaKonsept.Modules.Identity.Infrastructure.Security.Concrete;

/// <summary>
/// HMAC-SHA256 imzalı JWT üretir (<see cref="JsonWebTokenHandler"/>). Anahtar <see cref="IJwtSigningKeyProvider"/>'dan gelir;
/// host'taki JwtBearer doğrulaması aynı anahtarı, issuer ve audience'ı kullanır. Aynı türdeki birden çok claim JSON dizisi olur.
/// </summary>
public sealed class JwtAccessTokenFactory : AccessTokenFactoryBase
{
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };
    private readonly IJwtSigningKeyProvider _keyProvider;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="options">JWT ayarları.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="keyProvider">İmza anahtarı sağlayıcı.</param>
    public JwtAccessTokenFactory(JwtOptions options, IClock clock, IJwtSigningKeyProvider keyProvider)
        : base(options, clock)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <inheritdoc />
    protected override string Sign(IReadOnlyList<Claim> claims, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Options.Issuer,
            Audience = Options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAtUtc.UtcDateTime,
            NotBefore = issuedAtUtc.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_keyProvider.GetSigningKey()), SecurityAlgorithms.HmacSha256),
        };
        return Handler.CreateToken(descriptor);
    }
}
