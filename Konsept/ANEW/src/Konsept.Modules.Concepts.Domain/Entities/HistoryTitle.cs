namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Profil geçmişindeki bir başlık (spec 0014 Req 4–5): seçilip üretilen konu mu (yoksa elenen/fikirden mi), videosu silinip "beğenilmedi" mi
/// (spec 0019 Req 6) ve zamanı.
/// </summary>
/// <param name="Title">Başlık.</param>
/// <param name="Chosen">Seçilip üretilen konu ise <c>true</c>.</param>
/// <param name="At">Zaman (UTC; en yeni önce sıralamak için).</param>
/// <param name="Disliked">Videosu Silinenler'de ise <c>true</c> (negatif sinyal).</param>
public sealed record HistoryTitle(string Title, bool Chosen, DateTimeOffset At, bool Disliked = false);
