namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Bir aktivite: yalnızca kim, ne zaman, ne tür işlem (BR-1; olay metni yok).</summary>
/// <param name="OccurredAt">Zaman (UTC).</param>
/// <param name="UserName">Kullanıcı adı.</param>
/// <param name="UserEmail">Kullanıcı e-postası.</param>
/// <param name="Activity">Aktivite türü (Türkçe etiket).</param>
public sealed record AdminActivityDto(DateTimeOffset OccurredAt, string UserName, string UserEmail, string Activity);
