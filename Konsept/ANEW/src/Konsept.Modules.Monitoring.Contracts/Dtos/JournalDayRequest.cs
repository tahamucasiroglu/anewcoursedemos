namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Günlük geçmişi isteği (sorgu metni).
/// </summary>
/// <param name="Date">İstenen gün (<c>yyyy-MM-dd</c>); boş, geçersiz ya da ileri tarih bugündür.</param>
/// <param name="Filter">Seviye filtresi: <c>all</c>, <c>warning</c>, <c>error</c>.</param>
/// <param name="Skip">Atlanacak kayıt sayısı ("Daha fazla"); negatifse 0.</param>
public sealed record JournalDayRequest(string? Date = null, string? Filter = null, int Skip = 0);
