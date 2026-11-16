using System.Globalization;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Shared.Infrastructure.Http;
using Konsept.Shared.Infrastructure.Http.Base;
using Microsoft.AspNetCore.SignalR;

namespace Konsept.Modules.AgentGateway.Infrastructure.Hubs;

/// <summary>
/// Ajanın bağlandığı SignalR hub'ı (spec 0012 Req 10): yalnızca eşleşmiş ajanın geçerli oturum belirteciyle bağlanılır
/// (<see cref="AuthPolicies.AgentHub"/>, uç noktada zorlanır). Sahip ve cihaz hub kimliğinden okunur, istemciden alınmaz.
/// Bağlantıyı ajan açar (NAT arkasında); her yeniden bağlanmada kendini yeniden kaydeder. Çerçeve adaptörü.
/// </summary>
public sealed class AgentHub : Hub
{
    private readonly IAgentPresenceService _presence;
    private readonly IAgentLogService _logs;
    private readonly IAgentConnectionTracker _tracker;

    /// <summary>Initializes a new instance of the <see cref="AgentHub"/> class.</summary>
    /// <param name="presence">Ajan varlık servisi.</param>
    /// <param name="logs">Ajan log özeti servisi.</param>
    /// <param name="tracker">Açık bağlantılar (iptalde kesmek için).</param>
    public AgentHub(IAgentPresenceService presence, IAgentLogService logs, IAgentConnectionTracker tracker)
    {
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        _tracker.Track(Context.ConnectionId, Owner(), Context.Abort);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Ajan kendini kaydeder.</summary>
    /// <param name="registration">Ajanın bilgileri.</param>
    /// <returns>Görev.</returns>
    public Task Register(AgentRegistration registration) =>
        _presence.RegisterAsync(Context.ConnectionId, Owner(), registration, Context.ConnectionAborted);

    /// <summary>Ajan Error/Critical log özeti gönderir (spec 0008 Req 7); kayıtsız bağlantıdan ya da düşük seviyede reddedilir.</summary>
    /// <param name="summary">Özet.</param>
    /// <returns>Kabul edildiyse <c>true</c>.</returns>
    public Task<bool> ReportLog(AgentLogSummary summary) =>
        _logs.ReportAsync(Context.ConnectionId, summary, Context.ConnectionAborted);

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.Untrack(Context.ConnectionId);
        await _presence.DisconnectAsync(Context.ConnectionId, CancellationToken.None).ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private AgentOwner Owner() =>
        new(ReadGuid(CurrentUserBase.SubjectClaim), ReadGuid(AuthPolicies.AgentDeviceClaim));

    private Guid ReadGuid(string claim) =>
        Guid.TryParse(Context.User?.FindFirst(claim)?.Value, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new HubException("Ajan oturumu geçersiz.");
}
