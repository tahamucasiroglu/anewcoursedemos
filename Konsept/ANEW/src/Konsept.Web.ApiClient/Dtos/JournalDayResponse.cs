namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// Günlük geçmişinde bir günün bir sayfası (spec 0021 Req 5–6).
/// </summary>
/// <param name="Date">Gün (<c>yyyy-MM-dd</c>, Europe/Istanbul).</param>
/// <param name="IsToday">Bugün mü.</param>
/// <param name="IsFirstDay">Gezilebilecek ilk gün mü.</param>
/// <param name="Items">Kayıtlar (en yenisi üstte).</param>
/// <param name="HasMore">Devamı var mı.</param>
public sealed record JournalDayResponse(string Date, bool IsToday, bool IsFirstDay, IReadOnlyList<SystemEventResponse> Items, bool HasMore);
