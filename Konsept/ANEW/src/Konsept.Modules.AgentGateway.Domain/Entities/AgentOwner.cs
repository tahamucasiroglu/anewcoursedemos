namespace Konsept.Modules.AgentGateway.Domain.Entities;

/// <summary>
/// Bağlantının sahibi (spec 0012 Req 13): ajan oturum belirtecinden doğrulanan kullanıcı ve cihaz. İstemcinin gönderdiği
/// hiçbir alandan gelmez; hub kimliğinden okunur.
/// </summary>
/// <param name="UserId">Sahip.</param>
/// <param name="DeviceId">Cihaz.</param>
public sealed record AgentOwner(Guid UserId, Guid DeviceId);
