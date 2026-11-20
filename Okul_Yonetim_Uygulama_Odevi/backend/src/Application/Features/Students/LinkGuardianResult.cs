namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Veli bağlama sonucu: kişi kimliği; e-posta sistemde zaten kayıtlıysa <c>ExistingAccount</c> (hesap açılmaz, şifre
/// değişmez); değilse bir kez gösterilecek geçici şifre. Bu yanıt dışında geçici şifre hiçbir yerde yer almaz.
/// </summary>
public sealed record LinkGuardianResult(Guid UserId, bool ExistingAccount, string? TemporaryPassword);
