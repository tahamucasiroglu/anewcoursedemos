using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan sürüm numarası ayırıcı (scoped).
/// </summary>
/// <param name="videos">Video deposu.</param>
/// <param name="items">Sıra öğeleri deposu.</param>
public sealed class VideoVersionAllocator(IVideoRepository videos, IQueueItemRepository items) : VideoVersionAllocatorBase(videos, items);
