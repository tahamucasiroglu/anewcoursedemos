namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Eşleştirme kodunun ait olduğu cihaz (spec 0012 Req 3): makine adı + ajan sürümü.</summary>
/// <param name="Code">Kanonik kod.</param>
/// <param name="MachineName">Makine adı.</param>
/// <param name="AgentVersion">Ajan sürümü.</param>
/// <param name="ExpiresAt">Kodun son geçerliliği (UTC).</param>
public sealed record PairingDeviceResponse(string Code, string MachineName, string AgentVersion, DateTimeOffset ExpiresAt);
