namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Başlık önerisi yanıtı: öneriler (<see cref="Items"/>; hafızadaki konular elenmiş), elenen tekrar sayısı ve çağrı maliyeti.
/// </summary>
public sealed record TopicSuggestionsResponse
{
    /// <summary>Öneriler.</summary>
    public IReadOnlyList<TopicSuggestionDto> Items { get; init; } = [];

    /// <summary>Hafızadaki konulara benzediği için elenen öneri sayısı.</summary>
    public int FilteredOut { get; init; }

    /// <summary>Çağrı maliyeti ($).</summary>
    public decimal CostUsd { get; init; }
}
