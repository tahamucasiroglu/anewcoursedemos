namespace Konsept.Agent.Domain.Entities;

/// <summary>Sunucudan alınan eşleştirme kodu (spec 0012 Req 1). Sır yalnızca bellekte tutulur, loglanmaz.</summary>
/// <param name="Code">Ekranda gösterilen kod (<c>ABCD-1234</c>).</param>
/// <param name="Secret">Durum sorgusu için onay sırrı.</param>
/// <param name="ExpiresAt">Son geçerlilik (UTC).</param>
public sealed record PairingCodeInfo(string Code, string Secret, DateTimeOffset ExpiresAt);
