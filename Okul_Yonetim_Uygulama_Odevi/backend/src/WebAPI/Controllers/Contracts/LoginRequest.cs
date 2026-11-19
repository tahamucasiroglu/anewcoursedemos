namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Giriş isteği: e-posta, şifre ve yenileme anahtarının nasıl verileceği — mobil <c>refreshTokenInBody: true</c> ile
/// gövdede alır; web (varsayılan) yalnızca HttpOnly çerezle alır.
/// </summary>
public sealed record LoginRequest(string Email, string Password, bool RefreshTokenInBody = false);
