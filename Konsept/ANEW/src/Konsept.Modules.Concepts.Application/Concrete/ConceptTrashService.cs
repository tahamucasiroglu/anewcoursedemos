using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Silinenler ve önerileri sıfırla; davranış <see cref="ConceptTrashServiceBase"/>'tedir.</summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="suggestions">Öneri deposu.</param>
/// <param name="profiles">Profil bağlamı.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public sealed class ConceptTrashService(
    IConceptRepository concepts,
    ITopicSuggestionRepository suggestions,
    IProfileGenerationReader profiles,
    ICurrentUser currentUser,
    TimeProvider time) : ConceptTrashServiceBase(concepts, suggestions, profiles, currentUser, time);
