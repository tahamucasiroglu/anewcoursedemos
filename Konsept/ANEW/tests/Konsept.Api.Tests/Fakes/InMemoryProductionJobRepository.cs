using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Üretim işlerinin bellek deposu (plan 0006): gerçek deponun koşullarını ve kullanıcı başına tek çalışan iş kısıtını uygular.</summary>
public sealed class InMemoryProductionJobRepository : IProductionJobRepository
{
    private static readonly ProductionJobStatus[] Running = [ProductionJobStatus.Producing, ProductionJobStatus.WaitingForAgent];
    private readonly List<ProductionJob> _rows = [];
    private readonly Lock _gate = new();

    /// <summary>Gets satırların kopyası.</summary>
    public IReadOnlyList<ProductionJob> Rows
    {
        get
        {
            lock (_gate)
            {
                return [.. _rows];
            }
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductionJob>> ListActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ProductionJob>>([.. _rows.Where(j => j.UserId == userId && j.Status != ProductionJobStatus.Completed).OrderBy(j => j.Position)]);
        }
    }

    /// <inheritdoc />
    public Task<ProductionJob?> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.FirstOrDefault(j => j.Id == jobId && j.UserId == userId));
        }
    }

    /// <inheritdoc />
    public Task<long> MaxPositionAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.Where(j => j.UserId == userId && j.ProfileId == profileId).Select(j => j.Position).DefaultIfEmpty(0).Max());
        }
    }

    /// <inheritdoc />
    public Task AddAsync(ProductionJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        lock (_gate)
        {
            if (_rows.Any(j => j.ConceptId == job.ConceptId && j.Status != ProductionJobStatus.Completed))
            {
                throw new InvalidOperationException("Konseptin tamamlanmamış işi var (tekil dizin).");
            }

            _rows.Add(job);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> SwapAsync(
        Guid userId, ProductionJob first, ProductionJob second, IReadOnlyCollection<ProductionJobStatus> movable, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        lock (_gate)
        {
            var a = _rows.FindIndex(j => j.Id == first.Id && j.UserId == userId && j.Position == first.Position && movable.Contains(j.Status));
            var b = _rows.FindIndex(j => j.Id == second.Id && j.UserId == userId && j.Position == second.Position && movable.Contains(j.Status));
            if (a < 0 || b < 0)
            {
                return Task.FromResult(false);
            }

            _rows[a] = _rows[a] with { Position = second.Position };
            _rows[b] = _rows[b] with { Position = first.Position };
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(Guid userId, Guid jobId, IReadOnlyCollection<ProductionJobStatus> allowed, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.RemoveAll(j => j.Id == jobId && j.UserId == userId && allowed.Contains(j.Status)) == 1);
        }
    }

    /// <inheritdoc />
    public Task<bool> StartAsync(
        Guid userId,
        Guid jobId,
        IReadOnlyCollection<ProductionJobStatus> from,
        FixedJobSettings fixedSettings,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = _rows.FindIndex(j => j.Id == jobId && j.UserId == userId && from.Contains(j.Status));
            if (index < 0 || _rows.Any(j => j.UserId == userId && Running.Contains(j.Status)))
            {
                return Task.FromResult(false);
            }

            _rows[index] = _rows[index] with { Status = ProductionJobStatus.Producing, FixedSettings = fixedSettings, UpdatedAt = at };
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<int> StopAsync(Guid userId, Guid? profileId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var count = 0;
            for (var i = 0; i < _rows.Count; i++)
            {
                var j = _rows[i];
                if (j.UserId == userId && Running.Contains(j.Status) && (profileId is null || j.ProfileId == profileId))
                {
                    _rows[i] = j with { Status = ProductionJobStatus.Stopped, UpdatedAt = at };
                    count++;
                }
            }

            return Task.FromResult(count);
        }
    }

    /// <inheritdoc />
    public Task SetProgressAsync(Guid userId, Guid jobId, JobProgress progress, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = _rows.FindIndex(j => j.Id == jobId && j.UserId == userId);
            if (index >= 0)
            {
                _rows[index] = _rows[index] with { Progress = progress, UpdatedAt = at };
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<bool> TransitionAsync(
        Guid userId, Guid jobId, IReadOnlyCollection<ProductionJobStatus> from, ProductionJobStatus to, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = _rows.FindIndex(j => j.Id == jobId && j.UserId == userId && from.Contains(j.Status));
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            if (Running.Contains(to) && _rows.Any(j => j.UserId == userId && j.Id != jobId && Running.Contains(j.Status)))
            {
                return Task.FromResult(false);
            }

            _rows[index] = _rows[index] with { Status = to, UpdatedAt = at };
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductionJob>> ListRunningAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ProductionJob>>([.. _rows.Where(j => Running.Contains(j.Status))]);
        }
    }

    /// <summary>Test kurulumu: işin durumunu doğrudan değiştirir (0017'nin geçişleri: Ajan bekleniyor, Hata).</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="status">Durum.</param>
    public void SetStatus(Guid jobId, ProductionJobStatus status)
    {
        lock (_gate)
        {
            var index = _rows.FindIndex(j => j.Id == jobId);
            _rows[index] = _rows[index] with { Status = status };
        }
    }
}
