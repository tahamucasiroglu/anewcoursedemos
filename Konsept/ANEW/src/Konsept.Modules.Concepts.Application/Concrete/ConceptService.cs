using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Üretilenler ve onay; davranış <see cref="ConceptServiceBase"/>'tedir.</summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="profiles">Profil bağlamı.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public sealed class ConceptService(IConceptRepository concepts, IProfileGenerationReader profiles, ICurrentUser currentUser, TimeProvider time)
    : ConceptServiceBase(concepts, profiles, currentUser, time);
