namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Refresh token üretimi ve özetleme: <see cref="Generate"/> kriptografik rastgele ham token (base64url),
/// <see cref="Hash"/> DB'de saklanan SHA-256 özeti (hex).
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Yeni ham refresh token üretir.</summary>
    /// <returns>Ham token.</returns>
    string Generate();

    /// <summary>Ham token'ın özetini hesaplar.</summary>
    /// <param name="refreshToken">Ham token.</param>
    /// <returns>Özet (hex, küçük harf).</returns>
    string Hash(string refreshToken);
}
