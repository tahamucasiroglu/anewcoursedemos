using System.Security.Cryptography;
using System.Text;
using TahaKonsept.Modules.Identity.Application.Abstract;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IRefreshTokenGenerator"/> tabanı: özetleme ortaktır (SHA-256, küçük harf hex); üretim türetilen sınıfta.
/// </summary>
public abstract class RefreshTokenGeneratorBase : IRefreshTokenGenerator
{
    /// <summary>Yeni ham refresh token üretir.</summary>
    /// <returns>Ham token.</returns>
    public abstract string Generate();

    /// <summary>Ham token'ın SHA-256 özetini (hex) hesaplar.</summary>
    /// <param name="refreshToken">Ham token.</param>
    /// <returns>Özet.</returns>
    public string Hash(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
