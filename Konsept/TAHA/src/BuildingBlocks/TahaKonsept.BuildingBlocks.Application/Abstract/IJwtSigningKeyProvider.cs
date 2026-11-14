namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// JWT imzalama anahtarı sağlayıcısı. Hem token üreten Identity modülü hem de doğrulayan host aynı anahtarı kullanır.
/// Kaynak: yapılandırma <c>Jwt:SigningKey</c> ya da otomatik üretilip saklanan <c>App_Data/keys/jwt.key</c>.
/// </summary>
public interface IJwtSigningKeyProvider
{
    /// <summary>HMAC-SHA256 imzalama anahtarı (en az 32 bayt).</summary>
    /// <returns>Anahtar baytları.</returns>
    byte[] GetSigningKey();
}
