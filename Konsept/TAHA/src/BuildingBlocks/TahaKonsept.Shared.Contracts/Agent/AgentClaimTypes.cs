namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajan token'larında kullanılan claim adları ve değerleri. Token üreten taraf (Identity ya da Agents modülündeki yedek üretici)
/// ajan token'ına <see cref="AgentDeviceId"/> claim'ini ve <c>client</c>=<see cref="AgentClientValue"/> değerini koyar;
/// hub ajanı bu claim ile tanır.
/// </summary>
public static class AgentClaimTypes
{
    /// <summary>Eşlenmiş cihaz kimliği claim adı (Guid).</summary>
    public const string AgentDeviceId = "agent_device_id";

    /// <summary><c>client</c> claim'inin ajan değeri.</summary>
    public const string AgentClientValue = "agent";
}
