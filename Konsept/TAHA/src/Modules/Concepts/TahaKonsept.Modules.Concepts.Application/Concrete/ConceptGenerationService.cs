using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan toplu üretim başlatıcı; davranış <see cref="ConceptGenerationServiceBase"/>'tedir.
/// </summary>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="budget">Bütçe bekçisi.</param>
/// <param name="profiles">Profil okuyucu.</param>
/// <param name="queue">İş kuyruğu.</param>
/// <param name="tracker">İş izleyicisi.</param>
/// <param name="clock">Saat.</param>
public sealed class ConceptGenerationService(
    ICurrentUser currentUser,
    IBudgetGuard budget,
    IProfileReader profiles,
    IConceptJobQueue queue,
    IConceptJobTracker tracker,
    IClock clock)
    : ConceptGenerationServiceBase(currentUser, budget, profiles, queue, tracker, clock);
