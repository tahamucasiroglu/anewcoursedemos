using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// İş çalıştırıcısının bağımlılık paketi (uzun kurucu yerine tek parametre): bağlam, üreticiler, kelime denetimi, hafıza, depo,
/// izleyici, işlem günlüğü, saat ve logger.
/// </summary>
/// <param name="Contexts">Bağlam kurucu.</param>
/// <param name="Generator">Konsept üreticisi.</param>
/// <param name="Topics">Konu üreticisi.</param>
/// <param name="WordCheck">Kelime denetimi.</param>
/// <param name="Memory">Konu hafızası.</param>
/// <param name="Concepts">Konsept deposu.</param>
/// <param name="Tracker">İş izleyicisi.</param>
/// <param name="Activity">İşlem günlüğü.</param>
/// <param name="Clock">Saat.</param>
/// <param name="Logger">Logger.</param>
public sealed record ConceptJobProcessorDependencies(
    IConceptContextProvider Contexts,
    IConceptGenerator Generator,
    ITopicGenerator Topics,
    IWordRepetitionChecker WordCheck,
    ITopicMemory Memory,
    IConceptRepository Concepts,
    IConceptJobTracker Tracker,
    IActivityLogger Activity,
    IClock Clock,
    ILogger Logger);
