namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okul açma sonucu: yeni kurucuya geçici şifre bir kez; var olan kişide şifre yok.</summary>
public sealed record PanelSchoolResult(Guid TenantId, bool ExistingAccount, string? TemporaryPassword);
