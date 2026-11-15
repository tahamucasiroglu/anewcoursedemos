using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Mapping;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Application.Options;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// <see cref="IAgentGateway"/> uygulaması: komutları kullanıcının birincil ajan bağlantısına SignalR ile gönderir,
/// ses önizlemesini <see cref="ISpeechPreviewCorrelator"/> ile bekler (zaman aşımı), yerel sunucu adreslerini
/// <see cref="IAgentVideoUrlBuilder"/> ile kurar. Scoped (cihaz deposunu kullanır).
/// </summary>
public sealed class AgentGateway : AgentGatewayBase, IAgentGateway
{
    private readonly IAgentDeviceRepository _devices;
    private readonly ISpeechPreviewCorrelator _previews;
    private readonly IAgentVideoUrlBuilder _urls;
    private readonly AgentsOptions _options;

    /// <summary>Oluşturur.</summary>
    /// <param name="tracker">Bağlantı izleyici.</param>
    /// <param name="clients">İstemci vekil sağlayıcı.</param>
    /// <param name="devices">Cihaz deposu.</param>
    /// <param name="previews">Önizleme eşleyici.</param>
    /// <param name="urls">Adres üretici.</param>
    /// <param name="options">Ayarlar.</param>
    public AgentGateway(
        IAgentConnectionTracker tracker,
        IAgentClientProvider clients,
        IAgentDeviceRepository devices,
        ISpeechPreviewCorrelator previews,
        IAgentVideoUrlBuilder urls,
        AgentsOptions options)
        : base(tracker, clients)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _previews = previews ?? throw new ArgumentNullException(nameof(previews));
        _urls = urls ?? throw new ArgumentNullException(nameof(urls));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Çevrimiçi ajan var mı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    public bool IsAgentOnline(Guid userId) => Tracker.GetPrimary(userId) is not null;

    /// <summary>Çevrimiçi ajanın bilgisi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bilgi ya da <c>null</c>.</returns>
    public async Task<AgentDeviceInfo?> GetOnlineAgentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = Tracker.GetPrimary(userId);
        if (connection is null)
        {
            return null;
        }

        var device = connection.DeviceId is { } deviceId ? await _devices.GetByIdAsync(deviceId, cancellationToken) : null;
        if (device is null)
        {
            return new AgentDeviceInfo
            {
                Id = connection.DeviceId ?? Guid.Empty,
                MachineName = connection.MachineName ?? string.Empty,
                IsOnline = true,
                LastSeenAtUtc = connection.LastSeenAtUtc,
                LanAddress = connection.LanAddress,
                VideoServerPort = connection.VideoServerPort,
            };
        }

        return ToInfo(device, connection);
    }

    /// <summary>Aşama komutu gönderir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> SendStageAsync(Guid userId, StageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return InvokeAsync(userId, c => c.ExecuteStage(command), cancellationToken);
    }

    /// <summary>İşi iptal ettirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> CancelJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default) =>
        InvokeAsync(userId, c => c.CancelJob(jobId), cancellationToken);

    /// <summary>İşi duraklatır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> PauseJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default) =>
        InvokeAsync(userId, c => c.PauseJob(jobId), cancellationToken);

    /// <summary>Klasörü açtırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> OpenFolderAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default) =>
        InvokeAsync(userId, c => c.OpenFolder(videoId), cancellationToken);

    /// <summary>Sistem oynatıcısında açtırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> OpenInSystemPlayerAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default) =>
        InvokeAsync(userId, c => c.OpenInSystemPlayer(videoId), cancellationToken);

    /// <summary>Yerel video dosyalarını sildirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> DeleteVideoAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default) =>
        InvokeAsync(userId, c => c.DeleteVideo(videoId), cancellationToken);

    /// <summary>Yetenekleri yeniletir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> RefreshCapabilitiesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        InvokeAsync(userId, c => c.RefreshCapabilities(), cancellationToken);

    /// <summary>Ses önizlemesi üretir ve yanıtı bekler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="command">Komut.</param>
    /// <param name="timeout">Bekleme süresi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result<SpeechPreviewResult>> PreviewSpeechAsync(
        Guid userId,
        SpeechPreviewCommand command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var connection = ResolveConnection(userId);
        if (connection.IsFailure)
        {
            return connection.Error;
        }

        var request = command.RequestId == Guid.Empty ? command with { RequestId = Guid.CreateVersion7() } : command;
        var pending = _previews.Register(userId, request.RequestId);
        var sent = await InvokeAsync(connection.Value, c => c.PreviewSpeech(request), cancellationToken);
        if (sent.IsFailure)
        {
            _previews.Cancel(request.RequestId);
            return sent.Error;
        }

        var wait = timeout ?? TimeSpan.FromSeconds(Math.Max(1, _options.SpeechPreviewTimeoutSeconds));
        try
        {
            return await pending.WaitAsync(wait, cancellationToken);
        }
        catch (TimeoutException)
        {
            _previews.Cancel(request.RequestId);
            return AgentErrors.PreviewTimeout;
        }
        catch (OperationCanceledException)
        {
            _previews.Cancel(request.RequestId);
            throw;
        }
    }

    /// <summary>Video adresi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Adres ya da hata.</returns>
    public Result<string> GetVideoUrl(Guid userId, Guid videoId) =>
        ResolveAddress(userId).Map(c => _urls.BuildVideoUrl(c.LanAddress!, c.VideoServerPort!.Value, c.StreamKey!, videoId));

    /// <summary>Küçük resim adresi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Adres ya da hata.</returns>
    public Result<string> GetThumbnailUrl(Guid userId, Guid videoId) =>
        ResolveAddress(userId).Map(c => _urls.BuildThumbnailUrl(c.LanAddress!, c.VideoServerPort!.Value, c.StreamKey!, videoId));

    /// <summary>Göreli yol için adres.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="relativePath">Göreli yol.</param>
    /// <returns>Adres ya da hata.</returns>
    public Result<string> GetAgentFileUrl(Guid userId, string relativePath) =>
        ResolveAddress(userId).Map(c => _urls.BuildFileUrl(c.LanAddress!, c.VideoServerPort!.Value, c.StreamKey!, relativePath));

    private static AgentDeviceInfo ToInfo(AgentDevice device, AgentConnection connection) =>
        AgentDeviceMapper.ToInfo(device, isOnline: true) with
        {
            LastSeenAtUtc = connection.LastSeenAtUtc,
            LanAddress = connection.LanAddress ?? device.LanAddress,
            VideoServerPort = connection.VideoServerPort ?? device.VideoServerPort,
        };
}
