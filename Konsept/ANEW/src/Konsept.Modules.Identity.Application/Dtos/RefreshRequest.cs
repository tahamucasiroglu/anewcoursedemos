namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Refresh token'la yeni token çifti isteği (dönen refresh token, docs/security.md).</summary>
/// <param name="RefreshToken">Geçerli refresh token.</param>
public sealed record RefreshRequest(string? RefreshToken);
