using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// Erişim anahtarı ayarları ("Jwt" bölümü). İmza anahtarı repoda değil: geliştirmede user-secrets, Pi'de
/// /etc/okul/okul-api.env (<c>Jwt__SigningKey</c>). En az 32 bayt olmalı; yoksa API başlamaz. Erişim anahtarı 15 dk geçerli.
/// Doğrulama parametreleri (yayıncı, hedef, imza, süre) API ile aynı yerden üretilir.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "okul-api";

    public string Audience { get; set; } = "okul-clients";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public SymmetricSecurityKey CreateSigningKey()
    {
        byte[] key = Encoding.UTF8.GetBytes(this.SigningKey ?? string.Empty);
        if (key.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey ayarı eksik ya da 32 bayttan kısa. Geliştirmede: dotnet user-secrets set Jwt:SigningKey <rastgele>; Pi'de: setup.sh service.");
        }

        return new SymmetricSecurityKey(key);
    }

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = this.Issuer,
        ValidateAudience = true,
        ValidAudience = this.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = this.CreateSigningKey(),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
    };
}
