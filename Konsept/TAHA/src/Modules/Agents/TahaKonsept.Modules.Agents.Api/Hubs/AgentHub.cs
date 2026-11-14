using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Api.Hubs;

/// <summary>
/// Masaüstü programının bağlandığı SignalR hub'ı (<c>/hubs/agent</c>, JWT <c>access_token</c> sorgusu, <c>[Authorize]</c>).
/// Ajan, token'daki <c>agent_device_id</c> claim'i ya da token ailesi (<c>tfam</c>, eşlemede cihaza yazılır) ile sunucu tarafında tanınır;
/// <c>client</c>=<c>agent</c> olmayan (ör. web oturumu) token'lar reddedilir. <c>Hello</c>'daki bilgisayar adı kimlik için kullanılmaz. Tüm mantık
/// <see cref="IAgentHubService"/>'tedir; hub yalnızca bağlamı kurar, hatada bağlantıyı keser.
/// Sunucu → ajan çağrıları <see cref="IAgentClient"/>, ajan → sunucu metotları <see cref="IAgentHub"/>.
/// </summary>
[Authorize]
public sealed partial class AgentHub : Hub<IAgentClient>, IAgentHub
{
    private readonly IAgentHubService _service;
    private readonly IClock _clock;
    private readonly ILogger<AgentHub> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="service">Hub servisi.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlükçü.</param>
    public AgentHub(IAgentHubService service, IClock clock, ILogger<AgentHub> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Bağlantı açıldı: kaydeder; cihaz iptal edilmişse keser.</summary>
    /// <returns>Görev.</returns>
    public override async Task OnConnectedAsync()
    {
        var context = Context;
        var connection = new AgentConnection(
            context.ConnectionId,
            ReadGuid(context.User, CustomClaimTypes.UserId) ?? Guid.Empty,
            ReadGuid(context.User, AgentClaimTypes.AgentDeviceId),
            _clock.UtcNow,
            context.Abort)
        {
            TokenFamilyId = ReadGuid(context.User, CustomClaimTypes.TokenFamily),
            ClientType = context.User?.FindFirst(CustomClaimTypes.ClientType)?.Value,
        };
        var result = await _service.ConnectedAsync(connection, context.ConnectionAborted);
        if (result.IsFailure)
        {
            LogRejected(_logger, context.ConnectionId, result.Error.Code);
            context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    /// <summary>Bağlantı kapandı.</summary>
    /// <param name="exception">Kopma nedeni.</param>
    /// <returns>Görev.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _service.DisconnectedAsync(Context.ConnectionId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Ajan tanıtımı; cihaz tanınmazsa bağlantı kesilir.</summary>
    /// <param name="hello">Tanıtım.</param>
    /// <returns>Görev.</returns>
    public async Task Hello(AgentHello hello)
    {
        var result = await _service.HelloAsync(CallContext(), hello, Context.ConnectionAborted);
        if (result.IsFailure)
        {
            LogRejected(_logger, Context.ConnectionId, result.Error.Code);
            Context.Abort();
        }
    }

    /// <summary>Canlılık sinyali.</summary>
    /// <returns>Görev.</returns>
    public Task Heartbeat() => _service.HeartbeatAsync(CallContext(), Context.ConnectionAborted);

    /// <summary>Aşama ilerlemesi.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Görev.</returns>
    public Task StageProgress(StageProgressReport report) => _service.StageProgressAsync(CallContext(), report, Context.ConnectionAborted);

    /// <summary>Aşama tamamlandı.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Görev.</returns>
    public Task StageCompleted(StageResultReport report) => _service.StageCompletedAsync(CallContext(), report, Context.ConnectionAborted);

    /// <summary>Aşama başarısız.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Görev.</returns>
    public Task StageFailed(StageFailureReport report) => _service.StageFailedAsync(CallContext(), report, Context.ConnectionAborted);

    /// <summary>Günlük kaydı.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <returns>Görev.</returns>
    public Task Log(AgentLogEntry entry) => _service.LogAsync(CallContext(), entry, Context.ConnectionAborted);

    /// <summary>Ses önizleme sonucu.</summary>
    /// <param name="result">Sonuç.</param>
    /// <returns>Görev.</returns>
    public Task SpeechPreviewCompleted(SpeechPreviewResult result) => _service.SpeechPreviewCompletedAsync(CallContext(), result);

    private static Guid? ReadGuid(ClaimsPrincipal? user, string claimType) =>
        Guid.TryParse(user?.FindFirst(claimType)?.Value, out var value) && value != Guid.Empty ? value : null;

    [LoggerMessage(EventId = 7310, Level = LogLevel.Warning, Message = "Ajan bağlantısı reddedildi ({ConnectionId}): {Code}")]
    private static partial void LogRejected(ILogger logger, string connectionId, string code);

    private AgentCallContext CallContext() => new(
        Context.ConnectionId,
        ReadGuid(Context.User, CustomClaimTypes.UserId) ?? Guid.Empty,
        ReadGuid(Context.User, AgentClaimTypes.AgentDeviceId));
}
