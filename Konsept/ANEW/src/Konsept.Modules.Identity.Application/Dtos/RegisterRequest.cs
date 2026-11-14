namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Kayıt isteği (spec 0002 Req 1–3). Şifre bir kez sorulur.</summary>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
/// <param name="Password">Şifre (düz metin yalnızca istekte; hiçbir yere yazılmaz).</param>
public sealed record RegisterRequest(string? Name, string? Email, string? Password);
