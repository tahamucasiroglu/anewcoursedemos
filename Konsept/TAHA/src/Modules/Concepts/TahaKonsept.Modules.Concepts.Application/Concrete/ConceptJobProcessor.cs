using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan iş çalıştırıcısı (scoped); davranış <see cref="ConceptJobProcessorBase"/>'tedir.
/// </summary>
/// <param name="contexts">Bağlam kurucu.</param>
/// <param name="generator">Konsept üreticisi.</param>
/// <param name="topics">Konu üreticisi.</param>
/// <param name="wordCheck">Kelime denetimi.</param>
/// <param name="memory">Konu hafızası.</param>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="tracker">İş izleyicisi.</param>
/// <param name="activity">İşlem günlüğü.</param>
/// <param name="clock">Saat.</param>
/// <param name="logger">Logger.</param>
public sealed class ConceptJobProcessor(
    IConceptContextProvider contexts,
    IConceptGenerator generator,
    ITopicGenerator topics,
    IWordRepetitionChecker wordCheck,
    ITopicMemory memory,
    IConceptRepository concepts,
    IConceptJobTracker tracker,
    IActivityLogger activity,
    IClock clock,
    ILogger<ConceptJobProcessor> logger)
    : ConceptJobProcessorBase(new ConceptJobProcessorDependencies(contexts, generator, topics, wordCheck, memory, concepts, tracker, activity, clock, logger));
