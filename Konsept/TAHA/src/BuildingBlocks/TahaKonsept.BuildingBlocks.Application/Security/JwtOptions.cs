namespace TahaKonsept.BuildingBlocks.Application.Security;

/// <summary>
/// JWT ayarları (<c>Jwt</c> yapılandırma bölümü): issuer, audience, süreler ve opsiyonel imzalama anahtarı.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Yapılandırma bölüm adı.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token yayıncısı.</summary>
    public string Issuer { get; set; } = "TahaKonsept";

    /// <summary>Token hedef kitlesi.</summary>
    public string Audience { get; set; } = "TahaKonsept";

    /// <summary>Erişim token süresi (dakika).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Yenileme token süresi (gün).</summary>
    public int RefreshTokenDays { get; set; } = 30;

    /// <summary>İmzalama anahtarı (base64 ya da en az 32 karakter düz metin). Boşsa <c>App_Data/keys/jwt.key</c> üretilir.</summary>
    public string? SigningKey { get; set; }
}
