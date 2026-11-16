using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Base;

namespace Konsept.Modules.AgentGateway.Application.Concrete;

/// <summary>Üretim hattının ajan komutları (AgentGateway.Contracts, spec 0017); davranış <see cref="AgentCommandsBase"/>'tedir.</summary>
/// <param name="registry">Ajan oturum kaydı.</param>
/// <param name="channel">Komut kanalı.</param>
public sealed class AgentCommands(IAgentConnectionRegistry registry, IAgentChannel channel) : AgentCommandsBase(registry, channel)
{
    /// <inheritdoc />
    protected override bool IsConnectionLost(Exception exception) =>
        true; // Ajan işleyicisi kendi hatasını cevapla bildirir; gönderimde fırlayan her hata bağlantı sorunudur.
}
