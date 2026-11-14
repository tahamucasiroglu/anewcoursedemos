using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IVideoVersionAllocator"/> ortak davranışı: <see cref="IVideoRepository.MaxVersionForConceptAsync"/> ile
/// <see cref="IQueueItemRepository.MaxVersionForConceptAsync"/> değerlerinin büyüğü + 1.
/// </summary>
public abstract class VideoVersionAllocatorBase : IVideoVersionAllocator
{
    private readonly IVideoRepository _videos;
    private readonly IQueueItemRepository _items;

    /// <summary>Oluşturur.</summary>
    /// <param name="videos">Video deposu.</param>
    /// <param name="items">Sıra öğeleri deposu.</param>
    protected VideoVersionAllocatorBase(IVideoRepository videos, IQueueItemRepository items)
    {
        _videos = videos ?? throw new ArgumentNullException(nameof(videos));
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <summary>Sıradaki sürüm.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm.</returns>
    public async Task<int> NextAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        var videos = await _videos.MaxVersionForConceptAsync(userId, conceptId, cancellationToken);
        var pending = await _items.MaxVersionForConceptAsync(userId, conceptId, cancellationToken);
        return Math.Max(videos, pending) + 1;
    }
}
