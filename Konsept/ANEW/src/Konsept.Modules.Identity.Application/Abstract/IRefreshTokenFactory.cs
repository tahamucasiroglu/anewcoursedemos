namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Refresh token üretimi ve hash'i (veritabanında yalnızca hash durur).</summary>
public interface IRefreshTokenFactory
{
    /// <summary>Yeni rastgele refresh token üretir (<c>knr_</c> + 32 bayt base64url).</summary>
    /// <returns>Token.</returns>
    string Create();

    /// <summary>Token'ın saklanan hash'i (SHA-256, hex).</summary>
    /// <param name="token">Token.</param>
    /// <returns>Hash.</returns>
    string Hash(string token);
}
