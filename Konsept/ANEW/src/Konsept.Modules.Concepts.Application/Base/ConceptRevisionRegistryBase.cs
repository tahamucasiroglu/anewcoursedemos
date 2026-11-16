using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Bellek içi revizyon kaydı (tek Api süreci): kullanıcı + konsept başına son revizyon tutulur; süren yeniden üretim varken yenisi
/// başlamaz (spec 0015 Req 11, G-3). Tekil (singleton) kaydedilir.
/// </summary>
public abstract class ConceptRevisionRegistryBase : IConceptRevisionRegistry
{
    private readonly Lock _gate = new();
    private readonly Dictionary<(Guid UserId, Guid ConceptId), ConceptRevision> _revisions = [];

    /// <inheritdoc />
    public bool TryStart(ConceptRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        lock (_gate)
        {
            var key = (revision.UserId, revision.ConceptId);
            if (_revisions.TryGetValue(key, out var current) && current.IsRunning)
            {
                return false;
            }

            _revisions[key] = revision;
            return true;
        }
    }

    /// <inheritdoc />
    public ConceptRevision? Get(Guid userId, Guid conceptId)
    {
        lock (_gate)
        {
            return _revisions.GetValueOrDefault((userId, conceptId));
        }
    }

    /// <inheritdoc />
    public bool Remove(Guid userId, Guid conceptId, Guid revisionId)
    {
        lock (_gate)
        {
            var key = (userId, conceptId);
            if (!_revisions.TryGetValue(key, out var current) || current.Id != revisionId || current.IsRunning)
            {
                return false;
            }

            return _revisions.Remove(key);
        }
    }
}
