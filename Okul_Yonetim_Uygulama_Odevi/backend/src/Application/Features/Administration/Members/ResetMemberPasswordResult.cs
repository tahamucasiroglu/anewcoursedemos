namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Sıfırlama sonucu: bir kez gösterilecek yeni geçici şifre.</summary>
public sealed record ResetMemberPasswordResult(string TemporaryPassword);
