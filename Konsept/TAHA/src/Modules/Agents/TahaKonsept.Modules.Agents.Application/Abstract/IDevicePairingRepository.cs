using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Cihaz eşleme isteği deposu. Önemli metotlar: <see cref="FindByDeviceCodeHashAsync"/> (ajan yoklaması),
/// <see cref="FindOpenByUserCodeAsync"/> (web onayı), <see cref="IsUserCodeInUseAsync"/> (kod çakışması kontrolü).
/// </summary>
public interface IDevicePairingRepository : IRepository<DevicePairing>
{
    /// <summary>Cihaz kodu hash'ine göre getirir (takipli).</summary>
    /// <param name="deviceCodeHash">Hash.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstek ya da <c>null</c>.</returns>
    Task<DevicePairing?> FindByDeviceCodeHashAsync(string deviceCodeHash, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcı koduna göre en yeni bekleyen (ya da onaylanmış) isteği getirir (takipli).</summary>
    /// <param name="userCode">Normalize edilmiş kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstek ya da <c>null</c>.</returns>
    Task<DevicePairing?> FindOpenByUserCodeAsync(string userCode, CancellationToken cancellationToken = default);

    /// <summary>Bu kod, süresi dolmamış açık bir istekte kullanılıyor mu.</summary>
    /// <param name="userCode">Normalize edilmiş kod.</param>
    /// <param name="now">Şu an.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanılıyorsa <c>true</c>.</returns>
    Task<bool> IsUserCodeInUseAsync(string userCode, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Süresi belirli bir andan önce dolmuş istekleri silinmek üzere işaretler (temizlik; kalıcılaşması için <c>SaveChangesAsync</c>).</summary>
    /// <param name="before">Bu andan önce süresi dolanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen sayısı.</returns>
    Task<int> DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default);
}
