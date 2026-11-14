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
/// <see cref="IProductionQueueService"/> ortak davranışı (oturumdaki kullanıcı). Durum değişiklikleri sıra kaydına (<see cref="ProductionQueue"/>)
/// yazılır ve orkestratör uyandırılır; işin kendisini orkestratör değiştirir. İstisna: çalışan öğe çıkarılınca iş kullanıcı kilidi altında iptal edilir
/// (paylaşılmayan çalışma alanında ajana <c>CancelJob</c>, paylaşılanda <c>PauseJob</c> — kaynak sürümün dosyaları silinmesin). Kilidi beklemeden önce
/// <see cref="IJobCancellationRegistry"/> ile orkestratörün o iş için sürmekte olan sunucu zinciri (LLM/TTS/görsel çağrıları) kesilir; böylece istek
/// uzun zinciri beklemez ve kalan ücretli çağrılar yapılmaz.
/// </summary>
public abstract class ProductionQueueServiceBase : IProductionQueueService
{
    private readonly IProductionQueueRepository _queues;
    private readonly IQueueItemRepository _items;
    private readonly IJobRepository _jobs;
    private readonly IConceptSource _concepts;
    private readonly IAgentGateway _agent;
    private readonly IArtifactStore _artifacts;
    private readonly IUserJobLock _locks;
    private readonly IJobCancellationRegistry _cancellations;
    private readonly IVideoVersionAllocator _versions;
    private readonly IProductionSignal _signal;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    /// <summary>Oluşturur.</summary>
    /// <param name="queues">Sıra durumu deposu.</param>
    /// <param name="items">Sıra öğeleri deposu.</param>
    /// <param name="jobs">İş deposu.</param>
    /// <param name="concepts">Konsept portu.</param>
    /// <param name="agent">Ajan geçidi.</param>
    /// <param name="artifacts">Artefakt deposu.</param>
    /// <param name="locks">Kullanıcı kilidi.</param>
    /// <param name="cancellations">İş iptal kaydı.</param>
    /// <param name="versions">Sürüm numarası ayırıcı.</param>
    /// <param name="signal">Orkestratör sinyali.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="clock">Saat.</param>
    protected ProductionQueueServiceBase(
        IProductionQueueRepository queues,
        IQueueItemRepository items,
        IJobRepository jobs,
        IConceptSource concepts,
        IAgentGateway agent,
        IArtifactStore artifacts,
        IUserJobLock locks,
        IJobCancellationRegistry cancellations,
        IVideoVersionAllocator versions,
        IProductionSignal signal,
        ICurrentUser currentUser,
        IClock clock)
    {
        _queues = queues ?? throw new ArgumentNullException(nameof(queues));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _concepts = concepts ?? throw new ArgumentNullException(nameof(concepts));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        _locks = locks ?? throw new ArgumentNullException(nameof(locks));
        _cancellations = cancellations ?? throw new ArgumentNullException(nameof(cancellations));
        _versions = versions ?? throw new ArgumentNullException(nameof(versions));
        _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Sıranın tamamı.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> GetStateAsync(CancellationToken cancellationToken) =>
        await BuildStateAsync(_currentUser.GetRequiredUserId(), cancellationToken);

    /// <summary>Konseptleri sıraya ekler (zaten bekleyen/çalışan konsept tekrar eklenmez).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> AddAsync(AddToQueueRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = _currentUser.GetRequiredUserId();
        var existing = await _items.ListForUserAsync(userId, cancellationToken);
        var pending = existing.Where(i => i.Status is QueueItemStatus.Waiting || i.IsActive).Select(i => i.ConceptId).ToHashSet();
        var order = existing.Count == 0 ? 0 : existing.Max(i => i.Order);
        var settingsJson = request.Settings is null ? null : ProductionJson.Write(ProductionSettingsDefaults.Normalize(request.Settings));
        var added = 0;
        foreach (var conceptId in request.ConceptIds.Distinct())
        {
            if (pending.Contains(conceptId))
            {
                continue;
            }

            var concept = await _concepts.GetAsync(userId, conceptId, cancellationToken);
            if (concept is null)
            {
                continue;
            }

            var version = await _versions.NextAsync(userId, conceptId, cancellationToken);
            await _items.AddAsync(QueueItem.Create(userId, conceptId, concept.Title, ++order, settingsJson, version: version), cancellationToken);
            added++;
        }

        if (added == 0)
        {
            return ProductionErrors.NothingToQueue;
        }

        await EnsureQueueAsync(userId, cancellationToken);
        await _items.UnitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(userId, cancellationToken);
    }

    /// <summary>Öğeyi yukarı/aşağı taşır (komşusuyla yer değiştirir).</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> MoveAsync(Guid id, MoveQueueItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = _currentUser.GetRequiredUserId();
        var items = (await _items.ListForUserAsync(userId, cancellationToken)).ToList();
        var index = items.FindIndex(i => i.Id == id);
        if (index < 0)
        {
            return ProductionErrors.QueueItemNotFound;
        }

        var target = index + Math.Sign(request.Direction);
        if (request.Direction == 0 || target < 0 || target >= items.Count)
        {
            return ProductionErrors.CannotMove;
        }

        (items[index], items[target]) = (items[target], items[index]);
        for (var i = 0; i < items.Count; i++)
        {
            items[i].SetOrder(i + 1);
        }

        await _items.UnitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(userId, cancellationToken);
    }

    /// <summary>Öğeyi sıradan çıkarır; çalışıyorsa önce sürmekte olan zinciri keser, sonra kilit altında işi iptal eder.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        _cancellations.Cancel(userId, id);
        using (await _locks.AcquireAsync(userId, cancellationToken))
        {
            var item = await _items.GetForUserAsync(id, userId, cancellationToken);
            if (item is null)
            {
                return Result.Failure(ProductionErrors.QueueItemNotFound);
            }

            if (item.IsActive && item.JobId is Guid jobId)
            {
                var job = await _jobs.GetWithRunsAsync(jobId, cancellationToken);
                if (job is not null && job.Status is JobStatus.Running or JobStatus.Paused or JobStatus.Queued)
                {
                    if (job.Runs.Any(r => r.Status == StageStatus.Running && r.Location == StageLocation.Agent))
                    {
                        _ = job.SharesWorkspace
                            ? await _agent.PauseJobAsync(userId, job.WorkspaceId, cancellationToken)
                            : await _agent.CancelJobAsync(userId, job.WorkspaceId, cancellationToken);
                    }

                    job.Cancel(_clock.UtcNow);
                    await _artifacts.DeleteJobArtifactsAsync(userId, job.Id, cancellationToken);
                }
            }

            _items.Remove(item);
            await _items.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        _signal.Notify();
        return Result.Success();
    }

    /// <summary>Sırayla üretimi başlatır (duraklatılmışsa sürdürür).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> StartAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var queue = await EnsureQueueAsync(userId, cancellationToken);
        queue.Start();
        await _queues.UnitOfWork.SaveChangesAsync(cancellationToken);
        _signal.Notify();
        return await BuildStateAsync(userId, cancellationToken);
    }

    /// <summary>Durdurur: ajandaki aşamaya hemen duraklatma gider, iş kontrol noktasında bekler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> PauseAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var queue = await EnsureQueueAsync(userId, cancellationToken);
        queue.Pause();
        await _queues.UnitOfWork.SaveChangesAsync(cancellationToken);
        var active = await _items.GetActiveAsync(userId, cancellationToken);
        if (active?.JobId is Guid jobId)
        {
            var runs = await _jobs.ListRunsNoTrackingAsync(jobId, cancellationToken);
            var job = await _jobs.GetForUserNoTrackingAsync(jobId, userId, cancellationToken);
            if (job is not null && runs.Any(r => r.Status == StageStatus.Running && r.Location == StageLocation.Agent))
            {
                await _agent.PauseJobAsync(userId, job.WorkspaceId, cancellationToken);
            }
        }

        _signal.Notify();
        return await BuildStateAsync(userId, cancellationToken);
    }

    /// <summary>Kaldığı yerden sürdürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> ResumeAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var queue = await EnsureQueueAsync(userId, cancellationToken);
        queue.Resume();
        await _queues.UnitOfWork.SaveChangesAsync(cancellationToken);
        _signal.Notify();
        return await BuildStateAsync(userId, cancellationToken);
    }

    /// <summary>Tek öğenin durumu.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öğe.</returns>
    public async Task<Result<QueueItemDto>> GetItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var item = await _items.GetForUserAsync(id, userId, cancellationToken);
        if (item is null)
        {
            return ProductionErrors.QueueItemNotFound;
        }

        var job = item.JobId is Guid jobId ? await _jobs.GetForUserNoTrackingAsync(jobId, userId, cancellationToken) : null;
        return item.ToDto(job);
    }

    /// <summary>Biten/hatalı/iptal öğeleri kaldırır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    public async Task<Result<QueueStateDto>> ClearFinishedAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        foreach (var item in (await _items.ListForUserAsync(userId, cancellationToken)).Where(i => i.Status is QueueItemStatus.Done or QueueItemStatus.Failed or QueueItemStatus.Cancelled))
        {
            _items.Remove(item);
        }

        await _items.UnitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(userId, cancellationToken);
    }

    /// <summary>Kullanıcının sıra kaydını getirir ya da oluşturur (kaydetmez).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sıra.</returns>
    protected async Task<ProductionQueue> EnsureQueueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var queue = await _queues.GetForUserAsync(userId, cancellationToken);
        if (queue is null)
        {
            queue = ProductionQueue.Create(userId);
            await _queues.AddAsync(queue, cancellationToken);
        }

        return queue;
    }

    /// <summary>Sıra durumunu kurar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    protected async Task<Result<QueueStateDto>> BuildStateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var queue = await _queues.GetForUserAsync(userId, cancellationToken);
        var items = await _items.ListForUserAsync(userId, cancellationToken);
        var jobIds = items.Where(i => i.JobId is not null).Select(i => i.JobId!.Value).Distinct().ToList();
        var jobs = jobIds.Count == 0 ? [] : (await _jobs.ListWithRunsNoTrackingAsync(jobIds, cancellationToken)).ToDictionary(j => j.Id);
        var dtos = items.Select(i => i.ToDto(i.JobId is Guid jid && jobs.TryGetValue(jid, out var job) ? job : null)).ToList();
        var online = _agent.IsAgentOnline(userId);
        var waiting = dtos.Any(d => d.WaitingForAgent);
        return new QueueStateDto
        {
            Status = queue?.RunState ?? QueueRunState.Idle,
            Message = waiting ? "Ajan bekleniyor — masaüstü programını açın" : queue?.Message,
            AgentOnline = online,
            BudgetBlocked = queue?.BudgetBlocked ?? false,
            Items = dtos,
        };
    }
}
