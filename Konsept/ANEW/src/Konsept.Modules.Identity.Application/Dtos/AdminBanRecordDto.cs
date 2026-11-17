namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Kullanıcı satırındaki ban/ban kaldırma kaydı (spec 0024 Req 8; yalnızca admin görür).</summary>
/// <param name="Action"><c>ban</c> | <c>unban</c>.</param>
/// <param name="AdminName">Yapan admin.</param>
/// <param name="At">An (UTC).</param>
/// <param name="Reason">Sebep (yalnızca ban).</param>
public sealed record AdminBanRecordDto(string Action, string AdminName, DateTimeOffset At, string? Reason);
