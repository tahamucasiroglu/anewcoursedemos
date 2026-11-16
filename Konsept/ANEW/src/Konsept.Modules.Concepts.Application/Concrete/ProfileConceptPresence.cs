using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Konsepti olan profiller (Concepts.Contracts); davranış <see cref="ProfileConceptPresenceBase"/>'tedir.</summary>
/// <param name="concepts">Konsept deposu.</param>
public sealed class ProfileConceptPresence(IConceptRepository concepts) : ProfileConceptPresenceBase(concepts);
