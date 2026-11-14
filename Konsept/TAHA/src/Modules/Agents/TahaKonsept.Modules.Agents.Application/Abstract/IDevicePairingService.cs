using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Cihaz eşleme akışı (RFC 8628 benzeri). Ajan: <see cref="StartAsync"/> (anonim) → <see cref="PollTokenAsync"/> (anonim, aralıklı);
/// kullanıcı web'de: <see cref="ApproveAsync"/> / <see cref="DenyAsync"/> (oturum gerekli). Yoklama hataları
/// <c>DeviceTokenErrorCodes</c> kodlarıyla 400 döner; onaylıysa <see cref="TokenResponse"/> bir kez verilir.
/// </summary>
public interface IDevicePairingService
{
    /// <summary>Eşleme isteği açar.</summary>
    /// <param name="request">Bilgisayar adı ve sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz kodu, kullanıcı kodu, doğrulama adresi, aralık, süre.</returns>
    Task<Result<DeviceStartResponse>> StartAsync(DeviceStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ajan token yoklaması.</summary>
    /// <param name="request">Cihaz kodu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token ya da bekleme/yavaşla/süre doldu/reddedildi hatası.</returns>
    Task<Result<TokenResponse>> PollTokenAsync(DeviceTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oturumdaki kullanıcı kodu onaylar.</summary>
    /// <param name="request">Kullanıcı kodu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Onaylanan bilgisayar bilgisi.</returns>
    Task<Result<DeviceApproveResponse>> ApproveAsync(DeviceApproveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oturumdaki kullanıcı kodu reddeder.</summary>
    /// <param name="request">Kullanıcı kodu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DenyAsync(DeviceApproveRequest request, CancellationToken cancellationToken = default);
}
