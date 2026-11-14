namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Giriş isteği (spec 0002 Req 5).</summary>
/// <param name="Email">E-posta.</param>
/// <param name="Password">Şifre (düz metin yalnızca istekte; hiçbir yere yazılmaz).</param>
public sealed record LoginRequest(string? Email, string? Password);
