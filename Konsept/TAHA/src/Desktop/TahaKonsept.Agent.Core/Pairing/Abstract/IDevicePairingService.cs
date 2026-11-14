using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Agent.Core.Pairing.Abstract;

/// <summary>
/// RFC 8628 benzeri cihaz eşleme: <see cref="StartAsync"/> kullanıcı kodunu alır (UI gösterir), <see cref="WaitForApprovalAsync"/>
/// <c>api/agents/device/token</c>'ı aralıkla yoklar; onaylanınca token'lar <c>IAgentAuthService</c>'e kaydedilir.
/// <see cref="PairAsync"/> ikisini birleştirir. Durum <see cref="Status"/>/<see cref="StatusChanged"/> ile izlenir.
/// </summary>
public interface IDevicePairingService
{
    /// <summary>Anlık durum.</summary>
    PairingStatus Status { get; }

    /// <summary>Durum değişince tetiklenir.</summary>
    event EventHandler<PairingStatus>? StatusChanged;

    /// <summary>Eşlemeyi başlatır ve kullanıcı kodunu döner.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başlatma yanıtı ya da hata.</returns>
    Task<Result<DeviceStartResponse>> StartAsync(CancellationToken ct);

    /// <summary>Kullanıcı onaylayana, reddedene ya da süre dolana kadar yoklar.</summary>
    /// <param name="start">Başlatma yanıtı.</param>
    /// <param name="ct">İptal belirteci (kullanıcı vazgeçerse).</param>
    /// <returns>Eşlendiyse başarı; aksi halde hata (kod: <c>DeviceTokenErrorCodes</c>).</returns>
    Task<Result> WaitForApprovalAsync(DeviceStartResponse start, CancellationToken ct);

    /// <summary>Başlat + bekle.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> PairAsync(CancellationToken ct);

    /// <summary>Eşlemeyi kaldırır (token'lar silinir).</summary>
    void Unpair();
}
