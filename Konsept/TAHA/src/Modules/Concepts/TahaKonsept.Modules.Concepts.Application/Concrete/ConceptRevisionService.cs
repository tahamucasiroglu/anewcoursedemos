using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan düzeltme döngüsü servisi; davranış <see cref="ConceptRevisionServiceBase"/>'tedir.
/// </summary>
/// <param name="repository">Konsept deposu.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="budget">Bütçe bekçisi.</param>
/// <param name="contexts">Bağlam kurucu.</param>
/// <param name="generator">Konsept üreticisi.</param>
/// <param name="wordCheck">Kelime denetimi.</param>
/// <param name="candidates">Aday deposu.</param>
/// <param name="memory">Konu hafızası.</param>
/// <param name="clock">Saat.</param>
/// <param name="activity">İşlem günlüğü.</param>
public sealed class ConceptRevisionService(
    IConceptRepository repository,
    ICurrentUser currentUser,
    IBudgetGuard budget,
    IConceptContextProvider contexts,
    IConceptGenerator generator,
    IWordRepetitionChecker wordCheck,
    IRevisionCandidateStore candidates,
    ITopicMemory memory,
    IClock clock,
    IActivityLogger activity)
    : ConceptRevisionServiceBase(repository, currentUser, budget, contexts, generator, wordCheck, candidates, memory, clock, activity);
