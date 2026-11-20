namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Bağlantıyla yeni şifre.</summary>
public sealed record ResetPasswordRequest(string? Token, string? NewPassword, string? NewPasswordRepeat);
