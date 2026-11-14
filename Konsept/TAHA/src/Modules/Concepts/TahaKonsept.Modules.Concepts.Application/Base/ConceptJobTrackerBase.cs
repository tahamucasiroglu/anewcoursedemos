using System.Collections.Concurrent;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptJobTracker"/> ortak davranışı: iş kimliği → <see cref="ConceptJobState"/>; DTO değişimi durum kilidi altında.
/// Güncel iş: çalışan → en eski sıradaki → en son biten. Kullanıcı başına en çok <see cref="KeepFinished"/> biten iş tutulur.
/// </summary>
public abstract class ConceptJobTrackerBase : IConceptJobTracker
{
    private readonly ConcurrentDictionary<Guid, ConceptJobState> _jobs = new();

    /// <summary>Oluşturur.</summary>
    /// <param name="clock">Saat.</param>
    protected ConceptJobTrackerBase(IClock clock)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>Kullanıcı başına saklanan biten iş sayısı.</summary>
    protected virtual int KeepFinished => 5;

    /// <summary>İşi kaydeder.</summary>
    /// <param name="job">İş.</param>
    /// <returns>İlk durum.</returns>
    public ConceptJobDto Register(ConceptJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var state = new ConceptJobState(job, new ConceptJobDto
        {
            Id = job.Id,
            Kind = job.Kind,
            Status = ConceptJobStatus.Queued,
            Total = job.Count,
            CreatedAt = job.CreatedAtUtc,
        });
        _jobs[job.Id] = state;
        Prune(job.UserId);
        return GetCurrent(job.UserId);
    }

    /// <summary>Kullanıcının güncel işi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Durum.</returns>
    public ConceptJobDto GetCurrent(Guid userId)
    {
        var mine = _jobs.Values.Where(s => s.Job.UserId == userId).ToList();
        var running = mine.FirstOrDefault(s => Read(s).Status == ConceptJobStatus.Running);
        var queued = mine.Where(s => Read(s).Status == ConceptJobStatus.Queued).OrderBy(s => s.Job.CreatedAtUtc).ToList();
        var current = running
            ?? queued.FirstOrDefault()
            ?? mine.Where(s => s.IsFinished).OrderByDescending(s => Read(s).FinishedAt ?? s.Job.CreatedAtUtc).FirstOrDefault();
        if (current is null)
        {
            return new ConceptJobDto { Status = ConceptJobStatus.Idle };
        }

        var waiting = queued.Count(s => !ReferenceEquals(s, current));
        return Read(current) with { QueuedJobs = waiting };
    }

    /// <summary>Durumu günceller.</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="update">Güncelleme.</param>
    /// <returns>Yeni durum.</returns>
    public ConceptJobDto? Update(Guid jobId, Func<ConceptJobDto, ConceptJobDto> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!_jobs.TryGetValue(jobId, out var state))
        {
            return null;
        }

        lock (state.Gate)
        {
            state.Dto = update(state.Dto);
            return state.Dto;
        }
    }

    /// <summary>İptal belirteci.</summary>
    /// <param name="jobId">İş.</param>
    /// <returns>Belirteç.</returns>
    public CancellationToken GetCancellationToken(Guid jobId) =>
        _jobs.TryGetValue(jobId, out var state) ? state.Cancellation.Token : new CancellationToken(true);

    /// <summary>İptal edilmiş mi.</summary>
    /// <param name="jobId">İş.</param>
    /// <returns>İptal mi.</returns>
    public bool IsCancelled(Guid jobId) => !_jobs.TryGetValue(jobId, out var state) || state.Cancellation.IsCancellationRequested;

    /// <summary>Kullanıcının işlerini iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Güncel durum.</returns>
    public ConceptJobDto CancelAll(Guid userId)
    {
        var now = Clock.UtcNow;
        foreach (var state in _jobs.Values.Where(s => s.Job.UserId == userId && !s.IsFinished).ToList())
        {
            state.Cancellation.Cancel();
            lock (state.Gate)
            {
                if (state.Dto.Status == ConceptJobStatus.Queued)
                {
                    state.Dto = state.Dto with { Status = ConceptJobStatus.Cancelled, FinishedAt = now, Message = "İptal edildi." };
                }
            }
        }

        return GetCurrent(userId);
    }

    private static ConceptJobDto Read(ConceptJobState state)
    {
        lock (state.Gate)
        {
            return state.Dto;
        }
    }

    private void Prune(Guid userId)
    {
        var finished = _jobs.Values
            .Where(s => s.Job.UserId == userId && s.IsFinished)
            .OrderByDescending(s => Read(s).FinishedAt ?? s.Job.CreatedAtUtc)
            .Skip(KeepFinished)
            .ToList();
        foreach (var state in finished)
        {
            if (_jobs.TryRemove(state.Job.Id, out var removed))
            {
                removed.Dispose();
            }
        }
    }
}
