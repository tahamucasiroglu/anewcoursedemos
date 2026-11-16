using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Bellek içi iş kaydı (tek Api süreci): kullanıcı + şerit başına son iş tutulur; aynı şeritte süren iş varken yenisi başlamaz
/// (Req 19: iki şerit birbirinden bağımsızdır). Tekil (singleton) kaydedilir.
/// </summary>
public abstract class ConceptJobRegistryBase : IConceptJobRegistry
{
    private readonly Lock _gate = new();
    private readonly Dictionary<(Guid UserId, JobLane Lane), ConceptJob> _jobs = [];

    /// <inheritdoc />
    public bool TryStart(ConceptJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        lock (_gate)
        {
            var key = (job.UserId, job.Lane);
            if (_jobs.TryGetValue(key, out var current) && current.IsRunning)
            {
                return false;
            }

            _jobs[key] = job;
            return true;
        }
    }

    /// <inheritdoc />
    public ConceptJob? Get(Guid userId, JobLane lane)
    {
        lock (_gate)
        {
            return _jobs.GetValueOrDefault((userId, lane));
        }
    }

    /// <inheritdoc />
    public IReadOnlySet<Guid> RunningProfiles(Guid userId)
    {
        lock (_gate)
        {
            return _jobs.Where(p => p.Key.UserId == userId && p.Value.IsRunning).Select(p => p.Value.ProfileId).ToHashSet();
        }
    }
}
