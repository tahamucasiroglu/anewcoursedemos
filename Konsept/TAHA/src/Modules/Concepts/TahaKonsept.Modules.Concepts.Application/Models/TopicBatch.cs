using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Konu üreticisinin sonucu: elenmiş öneriler, elenen sayısı, maliyet.
/// </summary>
/// <param name="Items">Öneriler.</param>
/// <param name="FilteredOut">Hafıza/tekrar nedeniyle elenen.</param>
/// <param name="CostUsd">Maliyet.</param>
public sealed record TopicBatch(IReadOnlyList<TopicSuggestionDto> Items, int FilteredOut, decimal CostUsd);
