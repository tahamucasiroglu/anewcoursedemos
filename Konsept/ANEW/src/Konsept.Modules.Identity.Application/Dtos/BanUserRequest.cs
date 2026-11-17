namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Ban isteği (spec 0024 Req 3): isteğe bağlı sebep, en fazla 500 karakter.</summary>
/// <param name="Reason">Sebep.</param>
public sealed record BanUserRequest(string? Reason);
