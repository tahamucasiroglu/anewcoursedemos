using Microsoft.IdentityModel.Tokens;

namespace Konsept.Modules.Identity.Infrastructure.Security;

/// <summary>
/// JWT ayarları: imza anahtarı <c>Auth:SigningKey</c>'den (≥ 32 bayt); Development'ta yoksa süreç başına rastgele üretilir
/// (<see cref="KeyGenerated"/>). Anahtar değeri hiçbir zaman loglanmaz.
/// </summary>
/// <param name="Key">İmza anahtarı (HMAC-SHA256).</param>
/// <param name="KeyGenerated">Anahtar süreç başına rastgele mi üretildi.</param>
public sealed record JwtSettings(SymmetricSecurityKey Key, bool KeyGenerated)
{
    /// <summary>Token'ı veren.</summary>
    public const string Issuer = "konsept-api";

    /// <summary>Token'ın hedefi.</summary>
    public const string Audience = "konsept-web";

    /// <summary>Yapılandırma anahtarı.</summary>
    public const string SigningKeyConfigurationKey = "Auth:SigningKey";

    /// <summary>İmza anahtarının en az bayt uzunluğu.</summary>
    public const int MinKeyBytes = 32;

    /// <summary>Erişim token'ının ömrü (docs/security.md: 15 dk).</summary>
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
}
