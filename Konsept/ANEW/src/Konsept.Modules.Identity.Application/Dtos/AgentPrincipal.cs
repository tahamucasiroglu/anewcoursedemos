namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Doğrulanmış ajan oturumu: sahip ve cihaz (spec 0012 Req 13).</summary>
/// <param name="UserId">Sahip.</param>
/// <param name="DeviceId">Cihaz.</param>
public sealed record AgentPrincipal(Guid UserId, Guid DeviceId);
