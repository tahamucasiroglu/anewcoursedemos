using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>Concepts.Contracts <see cref="IProfileConceptPresence"/>: konsepti olan profiller (silinmişler dahil; spec 0014 Req 26).</summary>
/// <param name="concepts">Konsept deposu.</param>
public abstract class ProfileConceptPresenceBase(IConceptRepository concepts) : IProfileConceptPresence
{
    /// <inheritdoc />
    public Task<IReadOnlySet<Guid>> ProfilesWithConceptsAsync(Guid userId, CancellationToken cancellationToken) =>
        concepts.ProfileIdsAsync(userId, cancellationToken);
}
