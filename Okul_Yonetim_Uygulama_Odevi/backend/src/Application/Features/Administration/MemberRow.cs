namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Kişi listesi satırı (yalnızca seçili okul): ad, e-posta, telefon, durum ve bu okuldaki roller. Rolü kalmayan ya da
/// pasifleştirilen kişi pasiftir. Başka okulların hiçbir bilgisi taşınmaz.
/// </summary>
public sealed record MemberRow(Guid UserId, string FullName, string Email, string? Phone, bool IsActive, IReadOnlyList<MemberRoleView> Roles);
