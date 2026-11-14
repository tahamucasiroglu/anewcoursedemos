using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Application.Options;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Ajan hub mantığı. Bağlantıda yalnız ajan token'ını (<c>client</c>=<c>agent</c>) kabul eder; cihazı token'daki cihaz claim'i ya da token ailesiyle
/// (eşlemede cihaza yazılan <c>TokenFamilyId</c>) bulur, sahiplik/iptal kontrolü yapar ve çevrimiçi işaretler — cihazı bulunamayan bağlantı reddedilir.
/// Hello yalnız bağlantının cihazına uygulanır (istemcinin bildirdiği bilgisayar adıyla başka cihaza bağlanılamaz); ağ/yetenek bilgisini yazar (akış anahtarı <see cref="ISecretProtector"/> ile şifreli),
/// bellekteki bağlantıya düz anahtarı koyar. Heartbeat son görülmeyi en fazla <see cref="AgentsOptions.LastSeenWriteIntervalSeconds"/> aralıkla yazar.
/// Aşama olayları <see cref="IAgentStageEventHandler"/>'a, günlükler <see cref="IAgentLogSink"/>'e, önizleme sonuçları <see cref="ISpeechPreviewCorrelator"/>'a gider.
/// </summary>
public sealed class AgentHubService : AgentsServiceBase, IAgentHubService
{
    private readonly IAgentDeviceRepository _devices;
    private readonly IAgentConnectionTracker _tracker;
    private readonly IAgentStageEventHandler _stageHandler;
    private readonly IAgentLogSink _logSink;
    private readonly ISpeechPreviewCorrelator _previews;
    private readonly ISecretProtector _protector;
    private readonly AgentsOptions _options;

    /// <summary>Oluşturur.</summary>
    /// <param name="devices">Cihaz deposu.</param>
    /// <param name="tracker">Bağlantı izleyici.</param>
    /// <param name="stageHandler">Aşama olay alıcısı.</param>
    /// <param name="logSink">Günlük alıcısı.</param>
    /// <param name="previews">Önizleme eşleyici.</param>
    /// <param name="protector">Gizli değer koruyucu.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="clock">Saat.</param>
    public AgentHubService(
        IAgentDeviceRepository devices,
        IAgentConnectionTracker tracker,
        IAgentStageEventHandler stageHandler,
        IAgentLogSink logSink,
        ISpeechPreviewCorrelator previews,
        ISecretProtector protector,
        AgentsOptions options,
        IClock clock)
        : base(clock)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _stageHandler = stageHandler ?? throw new ArgumentNullException(nameof(stageHandler));
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
        _previews = previews ?? throw new ArgumentNullException(nameof(previews));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Cihaz iptal edildi / tanınmıyor hatası.</summary>
    public static Error DeviceRevokedError { get; } =
        Error.Forbidden("agents.device_revoked", "Bu bilgisayarın eşlemesi kaldırılmış. Masaüstü programında yeniden eşleyin.");

    /// <summary>Ajan olmayan token ile bağlanma hatası.</summary>
    public static Error NotAgentClientError { get; } =
        Error.Forbidden("agents.not_agent_client", "Bu bağlantı yalnız masaüstü programı içindir.");

    /// <summary>Bağlantı kayıtlı değil hatası.</summary>
    public static Error ConnectionUnknownError { get; } =
        Error.Failure("agents.connection_unknown", "Bağlantı kaydı bulunamadı.");

    /// <summary>Yeni bağlantıyı doğrular ve kaydeder.</summary>
    /// <param name="connection">Bağlantı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> ConnectedAsync(AgentConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.UserId == Guid.Empty)
        {
            return UnauthorizedError;
        }

        if (!string.Equals(connection.ClientType, AgentClaimTypes.AgentClientValue, StringComparison.Ordinal))
        {
            return NotAgentClientError;
        }

        var device = connection.DeviceId is { } deviceId
            ? await FindOwnedActiveAsync(connection.UserId, deviceId, cancellationToken)
            : connection.TokenFamilyId is { } family
                ? await _devices.FindActiveByTokenFamilyAsync(connection.UserId, family, cancellationToken)
                : null;
        if (device is null)
        {
            return DeviceRevokedError;
        }

        var now = Now;
        device.MarkOnline(now);
        await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);
        connection.DeviceId = device.Id;
        connection.LastPersistedAtUtc = now;
        _tracker.Add(connection);
        return Result.Success();
    }

    /// <summary>Bağlantı koptu; cihazın başka bağlantısı yoksa çevrimdışı işaretler.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task DisconnectedAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var connection = _tracker.Remove(connectionId);
        if (connection?.DeviceId is not { } deviceId || _tracker.IsDeviceOnline(deviceId))
        {
            return;
        }

        var device = await _devices.GetByIdAsync(deviceId, cancellationToken);
        if (device is null || device.IsRevoked)
        {
            return;
        }

        device.MarkOffline(Now);
        await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tanıtımı uygular.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="hello">Tanıtım.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> HelloAsync(AgentCallContext context, AgentHello hello, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(hello);
        var connection = _tracker.Get(context.ConnectionId);
        if (connection is null)
        {
            return ConnectionUnknownError;
        }

        var device = connection.DeviceId is { } deviceId
            ? await FindOwnedActiveAsync(connection.UserId, deviceId, cancellationToken)
            : null;
        if (device is null)
        {
            return DeviceRevokedError;
        }

        var now = Now;
        var streamKey = string.IsNullOrWhiteSpace(hello.StreamKey) ? null : hello.StreamKey.Trim();
        var lanAddress = string.IsNullOrWhiteSpace(hello.LanIpAddress) ? null : hello.LanIpAddress.Trim();
        int? port = hello.VideoServerPort > 0 ? hello.VideoServerPort : null;
        device.ApplyHello(
            hello.MachineName,
            hello.AgentVersion,
            string.IsNullOrWhiteSpace(hello.OsDescription) ? null : hello.OsDescription,
            lanAddress,
            port,
            streamKey is null ? null : _protector.Protect(streamKey),
            WriteCapabilities(hello.Capabilities),
            now);
        await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);

        connection.DeviceId = device.Id;
        connection.MachineName = device.MachineName;
        connection.LanAddress = lanAddress;
        connection.VideoServerPort = port;
        connection.StreamKey = streamKey;
        connection.LastSeenAtUtc = now;
        connection.LastPersistedAtUtc = now;
        connection.HelloReceived = true;
        return Result.Success();
    }

    /// <summary>Canlılık sinyali.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task HeartbeatAsync(AgentCallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var connection = _tracker.Get(context.ConnectionId);
        if (connection is null)
        {
            return;
        }

        var now = Now;
        connection.LastSeenAtUtc = now;
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.LastSeenWriteIntervalSeconds));
        if (connection.DeviceId is not { } deviceId
            || (connection.LastPersistedAtUtc is { } last && now - last < interval))
        {
            return;
        }

        var device = await _devices.GetByIdAsync(deviceId, cancellationToken);
        if (device is null || device.IsRevoked)
        {
            return;
        }

        device.MarkOnline(now);
        await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);
        connection.LastPersistedAtUtc = now;
    }

    /// <summary>Aşama ilerlemesini iletir.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task StageProgressAsync(AgentCallContext context, StageProgressReport report, CancellationToken cancellationToken = default) =>
        _stageHandler.OnStageProgressAsync(Enrich(context), report, cancellationToken);

    /// <summary>Aşama tamamlanmasını iletir.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task StageCompletedAsync(AgentCallContext context, StageResultReport report, CancellationToken cancellationToken = default) =>
        _stageHandler.OnStageCompletedAsync(Enrich(context), report, cancellationToken);

    /// <summary>Aşama hatasını iletir.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task StageFailedAsync(AgentCallContext context, StageFailureReport report, CancellationToken cancellationToken = default) =>
        _stageHandler.OnStageFailedAsync(Enrich(context), report, cancellationToken);

    /// <summary>Günlüğü iletir.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task LogAsync(AgentCallContext context, AgentLogEntry entry, CancellationToken cancellationToken = default) =>
        _logSink.WriteAsync(Enrich(context), entry, cancellationToken);

    /// <summary>Önizleme sonucunu bekleyen isteğe iletir.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="result">Sonuç.</param>
    /// <returns>Görev.</returns>
    public Task SpeechPreviewCompletedAsync(AgentCallContext context, SpeechPreviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _previews.Complete(result);
        return Task.CompletedTask;
    }

    private AgentCallContext Enrich(AgentCallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.DeviceId is null && _tracker.Get(context.ConnectionId)?.DeviceId is { } deviceId
            ? context with { DeviceId = deviceId }
            : context;
    }

    private async Task<AgentDevice?> FindOwnedActiveAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken)
    {
        var device = await _devices.GetByIdAsync(deviceId, cancellationToken);
        return device is null || device.UserId != userId || device.IsRevoked ? null : device;
    }
}
