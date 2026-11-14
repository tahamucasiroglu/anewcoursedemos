using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Mapping;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IVideoService"/> ortak davranışı (oturumdaki kullanıcı). Oynatma/küçük resim adresleri ajanın yerel sunucusundan
/// (<see cref="IAgentGateway.GetVideoUrl"/>); ajan kapalıysa adres yerine açıklama döner. Yeni sürüm: kaynak videonun konsept anlık görüntüsü +
/// yeni ayarlarla, <c>BaseJobId</c> = kaynak iş olarak sıraya eklenir (değişmeyen aşamalar yeniden kullanılır). Yeniden üret: kaynak iş yok (sıfırdan),
/// isteğe bağlı düzenlenmiş konseptle. Her ikisinde de sıra boştaysa üretim başlatılır.
/// </summary>
public abstract class VideoServiceBase : IVideoService
{
    private readonly IVideoRepository _videos;
    private readonly IQueueItemRepository _items;
    private readonly IProductionQueueRepository _queues;
    private readonly IProductionSettingsService _settings;
    private readonly IVideoVersionAllocator _versions;
    private readonly IAgentGateway _agent;
    private readonly IProductionSignal _signal;
    private readonly IProductionActivity _activity;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="videos">Video deposu.</param>
    /// <param name="items">Sıra öğeleri deposu.</param>
    /// <param name="queues">Sıra durumu deposu.</param>
    /// <param name="settings">Ayar servisi.</param>
    /// <param name="versions">Sürüm numarası ayırıcı.</param>
    /// <param name="agent">Ajan geçidi.</param>
    /// <param name="signal">Orkestratör sinyali.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="logger">Günlük.</param>
    protected VideoServiceBase(
        IVideoRepository videos,
        IQueueItemRepository items,
        IProductionQueueRepository queues,
        IProductionSettingsService settings,
        IVideoVersionAllocator versions,
        IAgentGateway agent,
        IProductionSignal signal,
        IProductionActivity activity,
        ICurrentUser currentUser,
        ILogger logger)
    {
        _videos = videos ?? throw new ArgumentNullException(nameof(videos));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _queues = queues ?? throw new ArgumentNullException(nameof(queues));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _versions = versions ?? throw new ArgumentNullException(nameof(versions));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Videolar.</summary>
    /// <param name="take">Azami adet (1-500).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    public async Task<Result<IReadOnlyList<VideoSummaryDto>>> ListAsync(int take, CancellationToken cancellationToken)
    {
        var videos = await _videos.ListForUserAsync(_currentUser.GetRequiredUserId(), take is > 0 and <= 500 ? take : 100, cancellationToken);
        return Result.Success<IReadOnlyList<VideoSummaryDto>>(videos.Select(v => v.ToSummaryDto()).ToList());
    }

    /// <summary>Video ayrıntısı.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    public async Task<Result<VideoDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var video = await _videos.GetForUserAsync(id, userId, cancellationToken);
        if (video is null)
        {
            return ProductionErrors.VideoNotFound;
        }

        var online = _agent.IsAgentOnline(userId);
        var url = online ? _agent.GetVideoUrl(userId, video.AgentVideoId) : null;
        var thumb = online ? _agent.GetThumbnailUrl(userId, video.AgentVideoId) : null;
        return video.ToDetailDto() with
        {
            AgentOnline = online,
            PlaybackUrl = url is { IsSuccess: true } ? url.Value : null,
            ThumbnailUrl = thumb is { IsSuccess: true } ? thumb.Value : null,
            PlaybackMessage = !online
                ? "Masaüstü programı kapalı; video bilgisayarınızda duruyor, program açılınca oynatılabilir."
                : url is { IsFailure: true } ? url.Error.Message : null,
        };
    }

    /// <summary>Videoyu siler (ajandaki dosyayı da sildirir; ajan kapalıysa kayıt yine silinir).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var video = await _videos.GetForUserAsync(id, userId, cancellationToken);
        if (video is null)
        {
            return Result.Failure(ProductionErrors.VideoNotFound);
        }

        var deleted = await _agent.DeleteVideoAsync(userId, video.AgentVideoId, cancellationToken);
        if (deleted.IsFailure)
        {
            _logger.LogWarning("Ajandaki video dosyası silinemedi (video {VideoId}): {Message}", video.Id, deleted.Error.Message);
        }

        _videos.Remove(video);
        await _videos.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _activity.InfoAsync(userId, "Video silindi · " + video.Title + (deleted.IsFailure ? " (yerel dosya masaüstü programı açılınca silinmeli)" : string.Empty), cancellationToken);
        return Result.Success();
    }

    /// <summary>Klasörde açtırır.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> OpenFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var video = await _videos.GetForUserAsync(id, userId, cancellationToken);
        return video is null ? Result.Failure(ProductionErrors.VideoNotFound) : await _agent.OpenFolderAsync(userId, video.AgentVideoId, cancellationToken);
    }

    /// <summary>Sistem oynatıcısında açtırır.</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> OpenInPlayerAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var video = await _videos.GetForUserAsync(id, userId, cancellationToken);
        return video is null ? Result.Failure(ProductionErrors.VideoNotFound) : await _agent.OpenInSystemPlayerAsync(userId, video.AgentVideoId, cancellationToken);
    }

    /// <summary>Yeni sürümü sıraya alır.</summary>
    /// <param name="id">Kaynak video.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sıra öğesi.</returns>
    public async Task<Result<QueueItemDto>> NewVersionAsync(Guid id, NewVersionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await EnqueueAsync(id, request.Settings, documentJson: null, reuse: true, cancellationToken);
    }

    /// <summary>Yeniden üretimi sıraya alır.</summary>
    /// <param name="id">Kaynak video.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sıra öğesi.</returns>
    public async Task<Result<QueueItemDto>> RegenerateAsync(Guid id, RegenerateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await EnqueueAsync(id, request.Settings, request.Document is null ? null : ProductionJson.Write(request.Document), reuse: false, cancellationToken);
    }

    /// <summary>Kaynak videodan sıra öğesi oluşturur, sıra boştaysa başlatır.</summary>
    /// <param name="videoId">Kaynak video.</param>
    /// <param name="settings">Ayarlar (<c>null</c> = güncel).</param>
    /// <param name="documentJson">Konsept geçersiz kılma (yeni sürümde kaynak videonunki).</param>
    /// <param name="reuse">Kaynak işin çıktıları yeniden kullanılsın mı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sıra öğesi.</returns>
    protected async Task<Result<QueueItemDto>> EnqueueAsync(Guid videoId, ProductionSettingsDto? settings, string? documentJson, bool reuse, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var video = await _videos.GetForUserAsync(videoId, userId, cancellationToken);
        if (video is null)
        {
            return ProductionErrors.VideoNotFound;
        }

        var effective = settings is null ? await _settings.GetForUserAsync(userId, cancellationToken) : ProductionSettingsDefaults.Normalize(settings);
        var version = await _versions.NextAsync(userId, video.ConceptId, cancellationToken);
        var order = await _items.MaxOrderAsync(userId, cancellationToken) + 1;
        var item = QueueItem.Create(
            userId,
            video.ConceptId,
            video.Title,
            order,
            ProductionJson.Write(effective),
            reuse ? video.DocumentJson : documentJson,
            reuse ? video.JobId : null,
            video.Id,
            version);
        await _items.AddAsync(item, cancellationToken);

        var queue = await _queues.GetForUserAsync(userId, cancellationToken);
        if (queue is null)
        {
            queue = ProductionQueue.Create(userId);
            await _queues.AddAsync(queue, cancellationToken);
        }

        if (queue.RunState == QueueRunState.Idle)
        {
            queue.Start();
        }

        await _items.UnitOfWork.SaveChangesAsync(cancellationToken);
        _signal.Notify();
        return item.ToDto(null);
    }
}
