using Konsept.Modules.Production.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>
/// Konseptten Video (spec 0006): profil başına üretim ayarları, üretim sırası ve sıra denetimi. İsteği yapan kullanıcıyla çalışır; başka
/// kullanıcının profili ya da işi 404'tür (BR-1). Her işlem profilin güncel durumunu döndürür.
/// </summary>
public interface IProductionService
{
    /// <summary>Profilin durumu.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> GetAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="request">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> SaveSettingsAsync(Guid profileId, SaveProductionSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Konseptleri sıranın sonuna ekler.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> EnqueueAsync(EnqueueRequest request, CancellationToken cancellationToken);

    /// <summary>İşi bir sıra taşır.</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="request">Yön.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> MoveAsync(Guid jobId, MoveJobRequest request, CancellationToken cancellationToken);

    /// <summary>İşi sıradan çıkarır; konsept <i>Onaylandı</i>'ya döner.</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> RemoveAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>Sırayla üret: en üstteki <i>Sırada</i> iş <i>Üretiliyor</i> olur.</summary>
    /// <param name="request">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> StartAsync(ProfileCommandRequest request, CancellationToken cancellationToken);

    /// <summary>Durdur: profilin çalışan işi <i>Durduruldu</i> olur.</summary>
    /// <param name="request">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> StopAsync(ProfileCommandRequest request, CancellationToken cancellationToken);

    /// <summary>Kaldığı yerden devam et: <i>Durduruldu</i>/<i>Hata</i> iş yeniden <i>Üretiliyor</i> olur.</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ProductionStateDto>> ResumeAsync(Guid jobId, CancellationToken cancellationToken);
}
