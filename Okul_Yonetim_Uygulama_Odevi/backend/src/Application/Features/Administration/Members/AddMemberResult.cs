namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>
/// Ekleme sonucu: kişi kimliği; e-posta sistemde zaten kayıtlıysa <c>ExistingAccount</c> (şifre üretilmez, değişmez);
/// değilse bir kez gösterilecek geçici şifre. Bu yanıt dışında geçici şifre hiçbir yerde yer almaz.
/// </summary>
public sealed record AddMemberResult(Guid UserId, bool ExistingAccount, string? TemporaryPassword);
