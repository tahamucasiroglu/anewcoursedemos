using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretilen videolar servisi (<c>api/production/videos...</c>): liste, ayrıntı (oynatma adresi ajandan), sil (ajandaki dosya da), klasörde aç,
/// sistem oynatıcısında aç, yeni sürüm (değişmeyen aşamalar yeniden kullanılır), yeniden üret (sıfırdan).
/// </summary>
public interface IVideoService
{
    /// <summary>Videolar (yeniden eskiye).</summary>
    /// <param name="take">Azami adet.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<IReadOnlyList<VideoSummaryDto>>> ListAsync(int take, CancellationToken cancellationToken);

    /// <summary>Video ayrıntısı.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<VideoDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Videoyu siler.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Klasörde açtırır.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> OpenFolderAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Sistem oynatıcısında açtırır.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> OpenInPlayerAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Yeni sürümü sıraya alır.</summary>
    /// <param name="id">Kaynak video.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueItemDto>> NewVersionAsync(Guid id, NewVersionRequest request, CancellationToken cancellationToken);

    /// <summary>Yeniden üretimi sıraya alır.</summary>
    /// <param name="id">Kaynak video.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueItemDto>> RegenerateAsync(Guid id, RegenerateRequest request, CancellationToken cancellationToken);
}
