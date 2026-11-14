using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretim sırası servisi (<c>api/production/queue...</c>): durum, ekle, taşı (↑↓), çıkar (çalışıyorsa iptal), sırayla üret, durdur (ajana kontrol noktasında
/// duraklat), devam (biten aşamalar atlanır), öğe durumu, bitenleri temizle.
/// </summary>
public interface IProductionQueueService
{
    /// <summary>Sıranın tamamı.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> GetStateAsync(CancellationToken cancellationToken);

    /// <summary>Konseptleri sıraya ekler.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> AddAsync(AddToQueueRequest request, CancellationToken cancellationToken);

    /// <summary>Öğeyi yukarı/aşağı taşır.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> MoveAsync(Guid id, MoveQueueItemRequest request, CancellationToken cancellationToken);

    /// <summary>Öğeyi sıradan çıkarır (çalışıyorsa işi iptal eder).</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Sırayla üretimi başlatır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> StartAsync(CancellationToken cancellationToken);

    /// <summary>Durdurur.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> PauseAsync(CancellationToken cancellationToken);

    /// <summary>Kaldığı yerden sürdürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> ResumeAsync(CancellationToken cancellationToken);

    /// <summary>Tek öğenin durumu.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueItemDto>> GetItemAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Biten/iptal/hatalı öğeleri sıradan kaldırır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<QueueStateDto>> ClearFinishedAsync(CancellationToken cancellationToken);
}
