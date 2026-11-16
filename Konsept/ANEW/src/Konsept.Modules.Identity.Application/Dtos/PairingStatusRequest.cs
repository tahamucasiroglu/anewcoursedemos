namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Ajanın kod durumu sorgusu (spec 0012 Req 10).</summary>
/// <param name="Secret">Onay sırrı.</param>
public sealed record PairingStatusRequest(string? Secret);
