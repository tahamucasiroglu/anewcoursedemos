namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Aktiviteler isteği (sorgu metni; spec 0010 Req 7–8).</summary>
/// <param name="Page">Sayfa (1'den; geçersizse 1).</param>
/// <param name="User">Ad/e-posta araması (en az 2 karakterle uygulanır).</param>
/// <param name="From">Başlangıç günü (<c>yyyy-MM-dd</c>, dahil).</param>
/// <param name="To">Bitiş günü (<c>yyyy-MM-dd</c>, dahil).</param>
public sealed record AdminActivitiesRequest(int Page = 1, string? User = null, string? From = null, string? To = null);
