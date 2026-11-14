using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan konu önerisi servisi; davranış <see cref="TopicSuggestionServiceBase"/>'tedir.
/// </summary>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="budget">Bütçe bekçisi.</param>
/// <param name="contexts">Bağlam kurucu.</param>
/// <param name="memory">Konu hafızası.</param>
/// <param name="generator">Konu üreticisi.</param>
/// <param name="activity">İşlem günlüğü.</param>
public sealed class TopicSuggestionService(
    ICurrentUser currentUser,
    IBudgetGuard budget,
    IConceptContextProvider contexts,
    ITopicMemory memory,
    ITopicGenerator generator,
    IActivityLogger activity)
    : TopicSuggestionServiceBase(currentUser, budget, contexts, memory, generator, activity);
