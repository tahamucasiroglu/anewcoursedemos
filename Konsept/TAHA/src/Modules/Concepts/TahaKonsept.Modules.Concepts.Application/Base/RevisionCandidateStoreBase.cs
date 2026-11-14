using System.Collections.Concurrent;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IRevisionCandidateStore"/> ortak davranışı: eşzamanlı sözlük; adaylar <see cref="TimeToLive"/> sonra geçersiz,
/// eklemede süresi dolanlar ve <see cref="MaxCandidates"/> üstündeki en eskiler temizlenir.
/// </summary>
public abstract class RevisionCandidateStoreBase : IRevisionCandidateStore
{
    private readonly ConcurrentDictionary<Guid, RevisionCandidate> _items = new();

    /// <summary>Oluşturur.</summary>
    /// <param name="clock">Saat.</param>
    protected RevisionCandidateStoreBase(IClock clock)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>Aday ömrü.</summary>
    protected virtual TimeSpan TimeToLive => TimeSpan.FromHours(2);

    /// <summary>En fazla aday.</summary>
    protected virtual int MaxCandidates => 200;

    /// <summary>Aday ekler.</summary>
    /// <param name="candidate">Aday.</param>
    public void Add(RevisionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Prune();
        _items[candidate.CandidateId] = candidate;
    }

    /// <summary>Adayı bulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <returns>Aday ya da null.</returns>
    public RevisionCandidate? Find(Guid userId, Guid conceptId, Guid candidateId) =>
        _items.TryGetValue(candidateId, out var c) && c.UserId == userId && c.ConceptId == conceptId && !IsExpired(c) ? c : null;

    /// <summary>Adayı siler.</summary>
    /// <param name="candidateId">Aday.</param>
    /// <returns>Silindi mi.</returns>
    public bool Remove(Guid candidateId) => _items.TryRemove(candidateId, out _);

    /// <summary>Konseptin adaylarını siler.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <returns>Silinenler.</returns>
    public IReadOnlyList<RevisionCandidate> RemoveForConcept(Guid conceptId)
    {
        var removed = new List<RevisionCandidate>();
        foreach (var c in _items.Values.Where(c => c.ConceptId == conceptId).ToList())
        {
            if (_items.TryRemove(c.CandidateId, out var r))
            {
                removed.Add(r);
            }
        }

        return removed;
    }

    private bool IsExpired(RevisionCandidate candidate) => Clock.UtcNow - candidate.CreatedAtUtc > TimeToLive;

    private void Prune()
    {
        foreach (var c in _items.Values.Where(IsExpired).ToList())
        {
            _items.TryRemove(c.CandidateId, out _);
        }

        var overflow = _items.Count - MaxCandidates + 1;
        if (overflow > 0)
        {
            foreach (var c in _items.Values.OrderBy(c => c.CreatedAtUtc).Take(overflow).ToList())
            {
                _items.TryRemove(c.CandidateId, out _);
            }
        }
    }
}
