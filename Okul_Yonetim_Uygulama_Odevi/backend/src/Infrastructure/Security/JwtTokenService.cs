using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// Erişim anahtarı (HS256 JWT): <c>sub</c> = kişi, <c>sid</c> = oturum, <c>tid</c> = seçili kurum (seçilmediyse yok),
/// 15 dk. Yenileme anahtarı 32 bayt kriptografik rastgele (base64url); veritabanına yalnızca SHA-256 özeti yazılır.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    public const string SessionClaim = "sid";
    public const string TenantClaim = "tid";

    private readonly JwtOptions options;
    private readonly SigningCredentials credentials;
    private readonly JsonWebTokenHandler handler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
        this.credentials = new SigningCredentials(this.options.CreateSigningKey(), SecurityAlgorithms.HmacSha256);
    }

    public IssuedAccessToken IssueAccessToken(Guid userId, Guid sessionId, Guid? tenantId, DateTimeOffset now)
    {
        DateTimeOffset expires = now.AddMinutes(this.options.AccessTokenMinutes);
        Dictionary<string, object> claims = new()
        {
            [JwtRegisteredClaimNames.Sub] = userId.ToString(),
            [SessionClaim] = sessionId.ToString(),
        };
        if (tenantId is { } tenant)
        {
            claims[TenantClaim] = tenant.ToString();
        }

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = this.options.Issuer,
            Audience = this.options.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = this.credentials,
        };
        return new IssuedAccessToken(this.handler.CreateToken(descriptor), expires);
    }

    public string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken)));
    }
}
