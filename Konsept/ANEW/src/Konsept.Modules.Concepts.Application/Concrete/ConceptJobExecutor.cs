using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Üretim işinin yürütücüsü; davranış <see cref="ConceptJobExecutorBase"/>'tedir.</summary>
/// <param name="caller">Model çağrısı.</param>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="suggestions">Öneri deposu.</param>
/// <param name="rules">Kurallar.</param>
/// <param name="events">Günlük olay yazıcısı.</param>
/// <param name="disliked">Silinenler'deki videoların başlıkları.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public sealed class ConceptJobExecutor(
    IModelCaller caller,
    IConceptRepository concepts,
    ITopicSuggestionRepository suggestions,
    IConceptRuleSource rules,
    ISystemEventWriter events,
    IDislikedVideoTitles disliked,
    TimeProvider time,
    ILogger<ConceptJobExecutor> logger) : ConceptJobExecutorBase(caller, concepts, suggestions, rules, events, disliked, time, logger);
