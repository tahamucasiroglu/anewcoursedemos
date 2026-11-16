using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Concepts.Contracts.Enums;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary><see cref="IConceptQueueBridge"/> (spec 0006): depo üzerinden okuma ve koşullu durum geçişi.</summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="time">Saat.</param>
public abstract class ConceptQueueBridgeBase(IConceptRepository concepts, TimeProvider time) : IConceptQueueBridge
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<QueueableConcept>> FindAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return [];
        }

        var found = await concepts.GetManyAsync(userId, ids, cancellationToken).ConfigureAwait(false);
        return [.. found.Select(c => new QueueableConcept(
            c.Id, c.ProfileId, c.Title, (ConceptQueueStage)(int)c.Status, c.Language, c.Format, c.TargetDurationSeconds))];
    }

    /// <inheritdoc />
    public async Task<bool> ChangeAsync(Guid userId, Guid conceptId, ConceptQueueStage from, ConceptQueueStage to, CancellationToken cancellationToken) =>
        await concepts.ChangeStatusAsync(userId, [conceptId], (ConceptStatus)(int)from, (ConceptStatus)(int)to, time.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false) == 1;

    /// <inheritdoc />
    public async Task<ConceptContent?> GetContentAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        var found = await concepts.GetManyAsync(userId, [conceptId], cancellationToken).ConfigureAwait(false);
        return found.FirstOrDefault(c => c.Id == conceptId) is { } c
            ? new ConceptContent(
                c.Id,
                c.ProfileId,
                c.Title,
                c.Language,
                c.Format,
                [.. c.Scenes.Select(s => new ConceptSceneContent(s.Title, s.Visual, s.Narration, s.DurationSeconds))])
            : null;
    }
}
