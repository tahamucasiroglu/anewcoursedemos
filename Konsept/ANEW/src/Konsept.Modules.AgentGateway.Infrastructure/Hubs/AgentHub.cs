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
    private readonly IAgentErrorReportService _errors;

    /// <summary>Initializes a new instance of the <see cref="AgentHub"/> class.</summary>
    /// <param name="presence">Ajan varlık servisi.</param>
    /// <param name="logs">Ajan log özeti servisi.</param>
    /// <param name="tracker">Açık bağlantılar (iptalde kesmek için).</param>
    /// <param name="errors">Ajan hata bildirimi servisi (spec 0022).</param>
    public AgentHub(IAgentPresenceService presence, IAgentLogService logs, IAgentConnectionTracker tracker, IAgentErrorReportService errors)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        if (!_tracker.Track(Context.ConnectionId, Owner(), Context.Abort))
        {
            // Kimlik doğrulandıktan sonra cihaz iptal edildi (review r1 M-1): eski oturum bağlı kalamaz.
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Ajan kendini kaydeder.</summary>
    /// <param name="registration">Ajanın bilgileri.</param>
    /// <returns>Sunucu sürümü ve ajanın güncellenmesi gerekip gerekmediği (spec 0023 Req 4).</returns>
    public Task<AgentRegisterReply> Register(AgentRegistration registration) =>
        _tracker.Contains(Context.ConnectionId)
            ? _presence.RegisterAsync(Context.ConnectionId, Owner(), registration, Context.ConnectionAborted)
            : throw new HubException("Ajan oturumu kapandı.");

    /// <summary>
    /// Ajan güncellemeye başlamak istiyor (spec 0023 Req 3): kullanıcının "Üretiliyor" işi varsa <c>false</c>; değilse oturum "güncelleniyor"
    /// olur ve ajana iş gitmez. İptal edilmiş oturumdan reddedilir.
    /// </summary>
    /// <returns>Başlayabilirse <c>true</c>.</returns>
    public Task<bool> BeginUpdate() =>
        _tracker.Contains(Context.ConnectionId)
            ? _presence.BeginUpdateAsync(Context.ConnectionId, Owner(), Context.ConnectionAborted)
            : throw new HubException("Ajan oturumu kapandı.");

    /// <summary>Kurulu bileşen listesi değişti (spec 0027 Req 9, B-149).</summary>
    /// <param name="components">Kurulu bileşen kimlikleri.</param>
    /// <returns>Kabul edildiyse <c>true</c>.</returns>
    public bool ReportComponents(IReadOnlyList<string> components) =>
        _tracker.Contains(Context.ConnectionId)
            ? _presence.ReportComponents(Context.ConnectionId, Owner(), components ?? [])
            : throw new HubException("Ajan oturumu kapandı.");

    /// <summary>Ajan Error/Critical log özeti gönderir (spec 0008 Req 7); kayıtsız bağlantıdan ya da düşük seviyede reddedilir.</summary>
    /// <param name="summary">Özet.</param>
    /// <returns>Kabul edildiyse <c>true</c>.</returns>
    public Task<bool> ReportLog(AgentLogSummary summary) =>
        _logs.ReportAsync(Context.ConnectionId, summary, Context.ConnectionAborted);

    /// <summary>
    /// Kullanıcının ajanda seçtiği hata bildirimi (spec 0022 Req 4, 8): sahip hub kimliğinden; iptal edilmiş oturumdan reddedilir.
    /// </summary>
    /// <param name="request">Bildirim.</param>
    /// <returns>Sonuç.</returns>
    public Task<AgentErrorReportReply> ReportError(AgentErrorReportRequest request) =>
        _tracker.Contains(Context.ConnectionId)
            ? _errors.ReportAsync(Owner().UserId, request, Context.ConnectionAborted)
            : throw new HubException("Ajan oturumu kapandı.");

    /// <summary>Güncelleme kurulmadan bitti (review r1 H-1): oturum yeniden iş alır. İptal edilmiş oturumdan reddedilir.</summary>
    public void EndUpdate()
    {
        if (!_tracker.Contains(Context.ConnectionId))
        {
            throw new HubException("Ajan oturumu kapandı.");
        }

        _presence.EndUpdate(Context.ConnectionId, Owner());
    }

    /// <summary>Ajanın eşleştiği hesap (spec 0022 Req 8): bekleyen bildirimleri hesaba bağlamak için; iptal edilmiş oturumdan reddedilir.</summary>
    /// <returns>Kullanıcı kimliği.</returns>
    public Guid WhoAmI() =>
        _tracker.Contains(Context.ConnectionId) ? Owner().UserId : throw new HubException("Ajan oturumu kapandı.");

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
