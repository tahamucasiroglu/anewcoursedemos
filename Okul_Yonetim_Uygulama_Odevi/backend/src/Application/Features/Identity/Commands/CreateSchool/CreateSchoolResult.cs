namespace OkulYonetim.Application.Features.Identity.Commands.CreateSchool;

/// <summary>
/// Okul açma sonucu: oluşturuldu mu yoksa kişi bu adla bir okulun zaten kurucusu mu; kurum, şube ve kişi kimlikleri;
/// kişinin yeni açılıp açılmadığı (mevcut kişinin şifresine dokunulmaz).
/// </summary>
public sealed record CreateSchoolResult(bool AlreadyExisted, Guid TenantId, Guid? BranchId, Guid UserId, bool UserCreated);
