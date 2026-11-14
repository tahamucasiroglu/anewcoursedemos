namespace TahaKonsept.Modules.Production.Normalization.Models;

/// <summary>Tek bir normalizasyon değişikliği (ör. <c>1453</c> → <c>bin dört yüz elli üç</c>, kural <c>number</c>).</summary>
/// <param name="Original">Özgün parça.</param>
/// <param name="Replacement">Okunuş karşılığı.</param>
/// <param name="Rule">Uygulanan kural anahtarı (lexicon, number, date, roman, ordinal, abbreviation, symbol).</param>
public sealed record NormalizationChange(string Original, string Replacement, string Rule);
