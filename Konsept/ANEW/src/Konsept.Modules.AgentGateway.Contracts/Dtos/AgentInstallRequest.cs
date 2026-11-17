namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Kullanıcının onayladığı bileşenleri kur (spec 0027 Req 5, 8): yalnızca kimlikler gider; ajan adres ve SHA-256'yı kendi gömülü
/// listesinden alır, listede olmayan kimliği kurmaz.
/// </summary>
/// <param name="Components">Bileşen kimlikleri.</param>
public sealed record AgentInstallRequest(IReadOnlyList<string> Components);
