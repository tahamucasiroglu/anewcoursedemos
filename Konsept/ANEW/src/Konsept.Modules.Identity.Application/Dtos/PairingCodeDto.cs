namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Ajana verilen kod (spec 0012 Req 1). Sır yalnızca ajanda kalır; hiçbir yere loglanmaz.</summary>
/// <param name="Code">Ekranda gösterilen kod (<c>ABCD-1234</c>).</param>
/// <param name="Secret">Durum sorgusu için onay sırrı.</param>
/// <param name="ExpiresAt">Son geçerlilik (UTC).</param>
public sealed record PairingCodeDto(string Code, string Secret, DateTimeOffset ExpiresAt);
