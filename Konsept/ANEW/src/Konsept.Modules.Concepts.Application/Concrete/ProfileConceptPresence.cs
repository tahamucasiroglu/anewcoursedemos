using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Konsepti olan ya da üretimi süren profiller (Concepts.Contracts); davranış <see cref="ProfileConceptPresenceBase"/>'tedir.</summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="jobs">İş kaydı.</param>
public sealed class ProfileConceptPresence(IConceptRepository concepts, IConceptJobRegistry jobs) : ProfileConceptPresenceBase(concepts, jobs);
