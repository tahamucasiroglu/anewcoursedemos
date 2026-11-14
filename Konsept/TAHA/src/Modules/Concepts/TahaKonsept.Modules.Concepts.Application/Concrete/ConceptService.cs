using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan konsept servisi; davranış <see cref="ConceptServiceBase"/>'tedir.
/// </summary>
/// <param name="repository">Konsept deposu.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="clock">Saat.</param>
/// <param name="memory">Konu hafızası.</param>
/// <param name="activity">İşlem günlüğü.</param>
public sealed class ConceptService(IConceptRepository repository, ICurrentUser currentUser, IClock clock, ITopicMemory memory, IActivityLogger activity)
    : ConceptServiceBase(repository, currentUser, clock, memory, activity);
