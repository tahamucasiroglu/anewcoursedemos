using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Hub.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Hub.Concrete;

/// <summary>
/// Ajanın SignalR hub bağlantısı. Sunucu komutlarını DI'daki <see cref="IAgentClient"/> uygulamasına
/// (<c>AgentCommandHandler</c>) iletir; işleyici geç çözülür (iş çalıştırıcı ↔ raporlayıcı döngüsü olmasın diye).
/// </summary>
public sealed class AgentHubConnection : AgentHubConnectionBase
{
    private readonly IServiceProvider _services;

    /// <summary>Oluşturur.</summary>
    /// <param name="auth">Oturum servisi.</param>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="hello">Hello üretici.</param>
    /// <param name="options">Seçenekler.</param>
    /// <param name="services">Servis sağlayıcı.</param>
    /// <param name="logger">Logger.</param>
    public AgentHubConnection(
        IAgentAuthService auth,
        IAgentSettingsStore settings,
        IAgentHelloBuilder hello,
        AgentOptions options,
        IServiceProvider services,
        ILogger<AgentHubConnection> logger)
        : base(auth, settings, hello, options, logger)
    {
        _services = services;
    }

    /// <inheritdoc />
    protected override IAgentClient ResolveClient() => _services.GetRequiredService<IAgentClient>();
}
