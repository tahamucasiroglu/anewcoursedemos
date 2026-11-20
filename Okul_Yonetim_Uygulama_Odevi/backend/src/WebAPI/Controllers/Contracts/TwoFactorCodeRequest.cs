namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>İki adımlı doğrulama kodu: bekleme anahtarı, 6 haneli kod; mobilde yenileme anahtarı gövdede istenir.</summary>
public sealed record TwoFactorCodeRequest(string? Challenge, string? Code, bool RefreshTokenInBody = false);
