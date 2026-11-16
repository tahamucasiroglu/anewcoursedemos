namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Günlük geçmişinde bir günün bir sayfası (spec 0021 Req 5–6): kayıtlar en yenisi üstte, en fazla 50.
/// </summary>
/// <param name="Date">Gösterilen gün (Europe/Istanbul, <c>yyyy-MM-dd</c>).</param>
/// <param name="IsToday">Gün bugün mü (sonraki gün düğmesi pasif).</param>
/// <param name="IsFirstDay">Gün gezilebilecek ilk gün mü (önceki gün düğmesi pasif).</param>
/// <param name="Items">Kayıtlar.</param>
/// <param name="HasMore">Bu sayfadan sonra kayıt var mı ("Daha fazla").</param>
public sealed record JournalDayDto(string Date, bool IsToday, bool IsFirstDay, IReadOnlyList<SystemEventDto> Items, bool HasMore);
