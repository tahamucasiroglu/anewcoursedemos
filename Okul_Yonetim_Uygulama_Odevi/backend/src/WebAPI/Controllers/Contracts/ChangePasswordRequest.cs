namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Şifre değiştirme isteği.</summary>
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? NewPasswordRepeat);
