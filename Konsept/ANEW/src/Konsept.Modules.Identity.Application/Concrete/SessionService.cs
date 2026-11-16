using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Oturum servisi (refresh, doğrulama, çıkış, geçerli kullanıcı, tüm oturumları iptal). Ajan cihaz iptali verilmişse "tüm
/// oturumları iptal et" eşleşmiş ajanı da düşürür (spec 0012 Req 12); DI her zaman verir.
/// </summary>
public sealed partial class SessionService : SessionServiceBase
{
    private readonly IAgentDeviceRevoker? _agentDevices;
    private readonly IReadOnlyList<IUserSessionsEndedListener> _listeners = [];

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

    /// <summary>Initializes a new instance of the <see cref="SessionService"/> class.</summary>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="currentUser">İsteği yapan kullanıcı.</param>
    /// <param name="logger">Teknik log.</param>
    /// <param name="agentDevices">Ajan cihaz iptali.</param>
    /// <param name="listeners">Tüm oturumlar kapanınca haber alanlar (spec 0006 Req 16: çalışan üretim işi durur).</param>
    public SessionService(
        AuthServiceDependencies dependencies,
        ICurrentUser currentUser,
        ILogger<SessionService> logger,
        IAgentDeviceRevoker agentDevices,
        IEnumerable<IUserSessionsEndedListener> listeners)
        : this(dependencies, currentUser, logger, agentDevices)
    {
        _listeners = [.. listeners ?? throw new ArgumentNullException(nameof(listeners))];
    }

    /// <inheritdoc />
    protected override async Task OnAllSessionsRevokedAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (_agentDevices is not null)
        {
            await _agentDevices.RevokeAllAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        foreach (var listener in _listeners)
        {
            try
            {
                await listener.OnAllSessionsEndedAsync(userId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Çıkış her koşulda tamamlanır (BR-14): dinleyicinin hatası oturum iptalini geri almaz, loglanır.
                LogListenerFailed(Logger, ex, listener.GetType().Name);
            }
        }
    }

    [LoggerMessage(EventId = 2010, Level = LogLevel.Error, Message = "Oturum sonu dinleyicisi başarısız: {Listener}")]
    private static partial void LogListenerFailed(ILogger logger, Exception exception, string listener);
}
