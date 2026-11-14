using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Refresh token deposu (Identity DbContext üzerinde). Kaydetme <see cref="IIdentityUnitOfWork"/> ile yapılır.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Yeni token ekler.</summary>
    /// <param name="token">Token.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Özete göre token bulur (izlenen).</summary>
    /// <param name="tokenHash">Özet.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token ya da null.</returns>
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Ailenin iptal edilmemiş token'larını getirir (izlenen).</summary>
    /// <param name="familyId">Aile.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token'lar.</returns>
    Task<IReadOnlyList<RefreshToken>> ListUnrevokedByFamilyAsync(Guid familyId, CancellationToken ct = default);

    /// <summary>Kullanıcının iptal edilmemiş tüm token'larını getirir (izlenen; web + ajan).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token'lar.</returns>
    Task<IReadOnlyList<RefreshToken>> ListUnrevokedByUserAsync(Guid userId, CancellationToken ct = default);
}
