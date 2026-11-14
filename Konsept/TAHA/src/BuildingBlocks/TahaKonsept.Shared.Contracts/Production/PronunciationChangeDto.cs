namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Okunuş önizlemesinde tek bir normalizasyon değişikliği (ör. <c>1453</c> → <c>bin dört yüz elli üç</c>, kural <c>number</c>).
/// </summary>
public sealed record PronunciationChangeDto
{
    /// <summary>Özgün parça (yazılış).</summary>
    public string Original { get; init; } = string.Empty;

    /// <summary>Okunuş karşılığı.</summary>
    public string Replacement { get; init; } = string.Empty;

    /// <summary>Kural anahtarı (lexicon, number, date, roman, ordinal, abbreviation, symbol).</summary>
    public string Rule { get; init; } = string.Empty;
}
