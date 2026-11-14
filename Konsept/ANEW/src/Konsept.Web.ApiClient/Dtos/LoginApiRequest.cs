namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Api'ye giriş isteği (şifre yalnızca istekte; loglanmaz).</summary>
/// <param name="Email">E-posta.</param>
/// <param name="Password">Şifre.</param>
public sealed record LoginApiRequest(string? Email, string? Password);
