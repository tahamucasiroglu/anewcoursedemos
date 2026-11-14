using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Hub.Abstract;

/// <summary>Hub'a gönderilecek <see cref="AgentHello"/> mesajını üretir (sürüm, makine, LAN IP, video portu, akış anahtarı, yetenekler).</summary>
public interface IAgentHelloBuilder
{
    /// <summary>Tanıtım mesajını üretir.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Mesaj.</returns>
    Task<AgentHello> BuildAsync(CancellationToken ct);
}
