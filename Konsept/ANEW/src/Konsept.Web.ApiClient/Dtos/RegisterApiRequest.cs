namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Api'ye kayıt isteği (şifre yalnızca istekte; loglanmaz).</summary>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
/// <param name="Password">Şifre.</param>
public sealed record RegisterApiRequest(string? Name, string? Email, string? Password);
