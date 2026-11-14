using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Refresh token rotasyonu: geçerli token → yeni çift (aynı aile, aynı tür), eski token "Rotated" ile iptal.
/// Kullanılmış/iptal edilmiş token gelirse (tekrar kullanım) aile tamamen iptal edilir ve Unauthorized döner.
/// </summary>
public interface IRefreshTokenRotator
{
    /// <summary>Token'ı döndürür.</summary>
    /// <param name="refreshToken">Ham refresh token.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yeni token çifti.</returns>
    Task<Result<TokenResponse>> RotateAsync(string refreshToken, CancellationToken ct = default);
}
