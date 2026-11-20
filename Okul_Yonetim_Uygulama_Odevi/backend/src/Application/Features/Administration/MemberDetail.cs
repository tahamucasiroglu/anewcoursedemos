namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Kişi kartı: liste satırı + istek sahibinin kendisi mi; kurucu bu kişinin şifresini sıfırlayabilir mi ve ad/telefonunu
/// düzenleyebilir mi (ikisi de yalnızca kişinin tek okulu bu okulsa — spec 0005 gereksinim 6 ve 10; hangi başka okul
/// olduğu taşınmaz).
/// </summary>
public sealed record MemberDetail(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<MemberRoleView> Roles,
    bool IsSelf,
    bool CanResetPassword,
    bool CanEditProfile);
