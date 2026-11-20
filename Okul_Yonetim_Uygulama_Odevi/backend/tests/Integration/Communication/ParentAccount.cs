namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>Testteki veli hesabı: kişi kimliği, e-postası, bu okulu seçmiş oturumu ve geçici şifresi (sahte saat ileri alınınca yeniden giriş için).</summary>
public sealed record ParentAccount(Guid UserId, string Email, string Token, string Password);
