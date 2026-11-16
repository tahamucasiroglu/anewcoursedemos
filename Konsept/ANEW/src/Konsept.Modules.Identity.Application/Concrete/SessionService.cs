using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Oturum servisi (refresh, doğrulama, çıkış, geçerli kullanıcı, tüm oturumları iptal). Ajan cihaz iptali verilmişse "tüm
/// oturumları iptal et" eşleşmiş ajanı da düşürür (spec 0012 Req 12); DI her zaman verir.
/// </summary>
public sealed class SessionService : SessionServiceBase
{
    private readonly IAgentDeviceRevoker? _agentDevices;

    /// <summary>Initializes a new instance of the <see cref="SessionService"/> class.</summary>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="currentUser">İsteği yapan kullanıcı.</param>
    /// <param name="logger">Teknik log.</param>
    public SessionService(AuthServiceDependencies dependencies, ICurrentUser currentUser, ILogger<SessionService> logger)
        : base(dependencies, currentUser, logger)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SessionService"/> class.</summary>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="currentUser">İsteği yapan kullanıcı.</param>
    /// <param name="logger">Teknik log.</param>
    /// <param name="agentDevices">Ajan cihaz iptali.</param>
    public SessionService(AuthServiceDependencies dependencies, ICurrentUser currentUser, ILogger<SessionService> logger, IAgentDeviceRevoker agentDevices)
        : base(dependencies, currentUser, logger)
    {
        _agentDevices = agentDevices ?? throw new ArgumentNullException(nameof(agentDevices));
    }

    /// <inheritdoc />
    protected override Task OnAllSessionsRevokedAsync(Guid userId, CancellationToken cancellationToken) =>
        _agentDevices?.RevokeAllAsync(userId, cancellationToken) ?? Task.CompletedTask;
}
