using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Concepts.Contracts <see cref="IProfileConceptPresence"/>: konsepti olan profiller (silinmişler dahil; spec 0014 Req 26) ve üretimi
/// süren profiller — iş sürerken profil silinirse yetim konsept yazılırdı (review r1 M-3).
/// </summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="jobs">İş kaydı.</param>
public abstract class ProfileConceptPresenceBase(IConceptRepository concepts, IConceptJobRegistry jobs) : IProfileConceptPresence
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ProfilesWithConceptsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var withConcepts = await concepts.ProfileIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        var result = new HashSet<Guid>(withConcepts);
        result.UnionWith(jobs.RunningProfiles(userId));
        return result;
    }
}
