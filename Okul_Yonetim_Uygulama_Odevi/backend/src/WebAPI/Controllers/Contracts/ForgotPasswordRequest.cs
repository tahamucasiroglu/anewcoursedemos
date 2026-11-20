namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Şifremi unuttum isteği.</summary>
public sealed record ForgotPasswordRequest(string? Email);
