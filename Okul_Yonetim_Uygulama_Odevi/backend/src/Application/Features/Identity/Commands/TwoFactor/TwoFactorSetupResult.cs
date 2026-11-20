namespace OkulYonetim.Application.Features.Identity.Commands.TwoFactor;

/// <summary>Kurulum bilgisi: elle yazılabilir anahtar ve kare kodun adresi (yalnızca kurulum ekranında, bir kez).</summary>
public sealed record TwoFactorSetupResult(string Secret, string OtpAuthUri);
