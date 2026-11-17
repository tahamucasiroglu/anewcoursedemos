using System.Net;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Connection;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// API'ye SignalR ile bağlanır: sunucu kapalıysa açılana kadar dener, koparsa kendiliğinden yeniden
/// bağlanır ve her bağlanmada kendini yeniden kaydeder (docs/conventions.md, Known traps). "Test et"
/// komutuna versiyon ve makine adıyla cevap verir. Ajan oturum belirteci <c>Authorization</c> başlığında gider (spec 0012);
/// sunucu "oturum kapandı" bildirirse ya da belirteci reddederse (401) yeniden denemez, <see cref="ServerConnectionBase.RaiseSessionRevoked"/>
/// ile haber verir. <see cref="SuspendAsync"/> sonrası yeniden başlatılabilir.
/// </summary>
public sealed class SignalRServerConnection : ServerConnectionBase
{
    // Sunucunun "ajan güncellenmeli" eksik kimliği (AgentGateway AgentCommandsBase.AgentProtocol ile aynı değer).
    private const string ProtocolMismatch = "agent";
    private readonly AgentSettings _settings;
    private readonly IAgentInfoProvider _info;
    private readonly Func<HttpMessageHandler>? _handlerFactory;
    private readonly ILogger _logger;
    private readonly Func<string?>? _accessToken;
    private readonly IStageRunner? _stages;
    private readonly IVideoStreamHost? _streams;
    private readonly IVideoFileActions? _files;
    private readonly IVideoFileDeleter? _deleter;
    private readonly Func<IReadOnlyList<string>>? _installedComponents;
    private readonly IComponentInstaller? _installer;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource _run;
    private HubConnection? _hub;
    private Task _connectLoop = Task.CompletedTask;
    private int _loopRunning;
    private volatile bool _suspended;

    /// <summary>Initializes a new instance of the <see cref="SignalRServerConnection"/> class.</summary>
    /// <param name="settings">Ajan ayarları.</param>
    /// <param name="info">Ajan bilgisi.</param>
    /// <param name="handlerFactory">Testte HTTP yolunu değiştirmek için; normalde <c>null</c>.</param>
    /// <param name="logger">Ajan teknik logu (spec 0008); verilmezse log yazılmaz.</param>
    /// <param name="accessToken">Ajan oturum belirtecini veren kaynak (spec 0012; <c>Authorization</c> başlığında gider, adreste değil).</param>
    /// <param name="stages">Yerel üretim aşamaları (spec 0017); verilmezse üretim komutlarına cevap verilmez.</param>
    /// <param name="streams">Yerel video akışı (spec 0017); verilmezse akış açılmaz.</param>
    /// <param name="installedComponents">Kurulu bileşen kimlikleri (B-110; kayıtta gönderilir); verilmezse gönderilmez.</param>
    /// <param name="files">Dosya eylemleri (spec 0007); verilmezse komut işlenmez.</param>
    /// <param name="deleter">Kalıcı silme (spec 0019); verilmezse komut işlenmez.</param>
    /// <param name="installer">Eksik bileşen kurulumu (spec 0027); verilmezse araç denetimi kurulabilir bilgisi vermez, kurulum komutu reddedilir.</param>
    public SignalRServerConnection(
        AgentSettings settings,
        IAgentInfoProvider info,
        Func<HttpMessageHandler>? handlerFactory = null,
        ILogger? logger = null,
        Func<string?>? accessToken = null,
        IStageRunner? stages = null,
        IVideoStreamHost? streams = null,
        Func<IReadOnlyList<string>>? installedComponents = null,
        IVideoFileActions? files = null,
        IVideoFileDeleter? deleter = null,
        IComponentInstaller? installer = null)
    {
        _installer = installer;
        _files = files;
        _deleter = deleter;
        _installedComponents = installedComponents;
        _stages = stages;
        _streams = streams;
        _accessToken = accessToken;
        _logger = (logger ?? Serilog.Core.Logger.None).ForContext<SignalRServerConnection>();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _handlerFactory = handlerFactory;
        _run = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
    }

    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _hub ??= BuildHub();
        if (_suspended)
        {
            _suspended = false;
            _run.Dispose();
            _run = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        }

        EnsureConnectLoop();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task SuspendAsync(CancellationToken cancellationToken)
    {
        _suspended = true;
        await _run.CancelAsync().ConfigureAwait(false);
        if (_hub is not null)
        {
            await _hub.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        await _connectLoop.ConfigureAwait(false);
        SetState(ConnectionState.Disconnected);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lifetime.CancelAsync().ConfigureAwait(false);
        if (_hub is not null)
        {
            await _hub.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        await _connectLoop.ConfigureAwait(false);
        SetState(ConnectionState.Disconnected);
    }

    /// <inheritdoc />
    public override async Task<bool> TryReportLogAsync(LogSummary summary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(summary);
        if (State != ConnectionState.Connected || _hub is null)
        {
            return false;
        }

        try
        {
            return await _hub.InvokeAsync<bool>(
                AgentHubContract.ReportLog,
                new AgentLogSummary(summary.Critical ? 5 : 4, summary.OccurredAt, summary.Message, _info.Version),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Özet gönderimi en iyi çabadır; hatası loglanmaz (döngü olmasın) ve ajanı etkilemez.
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }

    /// <inheritdoc />
    public override async Task<Guid?> TryGetAccountAsync(CancellationToken cancellationToken)
    {
        if (State != ConnectionState.Connected || _hub is null)
        {
            return null;
        }

        try
        {
            return await _hub.InvokeAsync<Guid>(AgentHubContract.WhoAmI, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Hesap okunamazsa bildirim bekletilir; ajanı etkilemez.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    /// <inheritdoc />
    public override async Task<bool?> TryBeginUpdateAsync(CancellationToken cancellationToken)
    {
        if (State != ConnectionState.Connected || _hub is null)
        {
            return null;
        }

        try
        {
            return await _hub.InvokeAsync<bool>(AgentHubContract.BeginUpdate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.Warning("Güncelleme başlangıcı sunucuya bildirilemedi: {ExceptionType}", ex.GetType().Name);
            return null;
        }
    }

    /// <inheritdoc />
    public override async Task TryEndUpdateAsync()
    {
        if (State != ConnectionState.Connected || _hub is null)
        {
            return; // Yeniden bağlanınca yeni oturum zaten iş alır.
        }

        try
        {
            await _hub.InvokeAsync(AgentHubContract.EndUpdate).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.Warning("Güncellemenin bittiği sunucuya bildirilemedi: {ExceptionType}", ex.GetType().Name);
        }
    }

    /// <inheritdoc />
    public override async Task<bool> TryReportComponentsAsync(CancellationToken cancellationToken)
    {
        if (State != ConnectionState.Connected || _hub is null || _installedComponents is null || UpdateRequired)
        {
            return false;
        }

        try
        {
            return await _hub.InvokeAsync<bool>(AgentHubContract.ReportComponents, _installedComponents(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.Warning("Kurulu bileşen listesi sunucuya bildirilemedi: {ExceptionType}", ex.GetType().Name);
            return false;
        }
    }

    /// <inheritdoc />
    public override async Task<ReportOutcome?> TrySendErrorReportAsync(AgentErrorReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (State != ConnectionState.Connected || _hub is null)
        {
            return null;
        }

        try
        {
            var reply = await _hub.InvokeAsync<AgentErrorReportReply>(
                AgentHubContract.ReportError,
                new AgentErrorReportRequest(report.AccountId, report.Summary, report.AgentVersion, report.OperatingSystem, report.Component, report.Step, report.LogLines),
                cancellationToken).ConfigureAwait(false);
            return reply.Result switch
            {
                AgentErrorReportResult.Sent => ReportOutcome.Sent,
                AgentErrorReportResult.RateLimited => ReportOutcome.RateLimited,
                AgentErrorReportResult.OtherAccount => ReportOutcome.OtherAccount,
                _ => ReportOutcome.Rejected,
            };
        }
#pragma warning disable CA1031 // İletilemeyen bildirim bekletilir (spec 0022 Req 7); hata ajanı etkilemez.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    /// <summary>Hata, sunucunun ajan oturumunu reddettiği anlamına mı geliyor (401).</summary>
    /// <param name="exception">Hata.</param>
    /// <returns>Reddedildiyse <c>true</c>.</returns>
    internal static bool IsUnauthorized(Exception? exception) =>
        exception is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized };

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        await _lifetime.CancelAsync().ConfigureAwait(false);
        if (_hub is not null)
        {
            await _hub.DisposeAsync().ConfigureAwait(false);
        }

        await _connectLoop.ConfigureAwait(false);
        _run.Dispose();
        _lifetime.Dispose();
    }

    private HubConnection BuildHub()
    {
        var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(_settings.ServerUrl, AgentHubContract.HubPath.TrimStart('/')), options =>
            {
                if (_handlerFactory is not null)
                {
                    options.HttpMessageHandlerFactory = _ => _handlerFactory();
                }

                if (_accessToken is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult(_accessToken());
                }
            })
            .WithAutomaticReconnect(new ForeverRetryPolicy(_settings.ReconnectDelay))
            .Build();

        hub.On(AgentHubContract.Ping, () => new AgentPingReply(_info.MachineName, _info.Version));
        if (_stages is { } stages)
        {
            // Üretim komutları yalnızca bu oturumun sunucusundan gelir (spec 0017 Req 13); ajan online sağlayıcıya istek atmaz.
            // Sürüm uyumsuzken (spec 0023 Req 4) hiçbir komut yürütülmez; sunucu da göndermez (çift güvence).
            hub.On<AgentToolCheckRequest, AgentToolCheckReply>(AgentHubContract.CheckTools, request => UpdateRequired ? new AgentToolCheckReply([ProtocolMismatch]) : CheckTools(stages, request));
            hub.On<AgentStageCommand, AgentStageReply>(
                AgentHubContract.RunStage, command => UpdateRequired ? Task.FromResult(new AgentStageReply(false, AgentStageReply.Failed)) : stages.RunAsync(command));
            hub.On<Guid>(AgentHubContract.CancelStage, stages.Cancel);
        }

        // Kullanıcının onayladığı eksik bileşenler (spec 0027 Req 5, 8): yalnızca gömülü listedeki kimlikler kurulur.
        hub.On<AgentInstallRequest, AgentInstallReply>(AgentHubContract.InstallComponents, InstallAsync);

        if (_streams is { } streams)
        {
            hub.On<AgentStreamRequest, AgentStreamReply>(AgentHubContract.OpenStream, request => UpdateRequired ? new AgentStreamReply(null, AgentFileActionReply.Failed) : streams.Open(request));
        }

        if (_files is { } files)
        {
            // Klasörde aç / Sistem oynatıcısında aç (spec 0007 Req 4): yalnızca bu oturumun sunucusundan.
            hub.On<AgentFileActionRequest, AgentFileActionReply>(
                AgentHubContract.OpenFile, request => UpdateRequired ? new AgentFileActionReply(false, AgentFileActionReply.Failed) : files.Open(request));
        }

        if (_deleter is { } deleter)
        {
            // Kalıcı sil (spec 0019 Req 4): yalnızca bu oturumun sunucusundan, yalnızca video klasöründeki .mp4 adları.
            hub.On<AgentDeleteFilesRequest, AgentDeleteFilesReply>(
                AgentHubContract.DeleteFiles, request => UpdateRequired ? new AgentDeleteFilesReply(request.FileNames) : deleter.Delete(request));
        }

        hub.On(AgentHubContract.SessionClosed, () =>
        {
            _logger.Warning("Sunucu ajan oturumunu kapattı");
            RaiseSessionRevoked();
        });
        hub.Reconnecting += _ =>
        {
            SetState(ConnectionState.Connecting);
            return Task.CompletedTask;
        };
        // Yeniden bağlanınca kayıt; kayıt başarısızsa döngü yalnızca kaydı yeniden dener (hub zaten bağlı).
        hub.Reconnected += _ =>
        {
            EnsureConnectLoop();
            return Task.CompletedTask;
        };
        hub.Closed += exception =>
        {
            SetState(ConnectionState.Disconnected);
            if (IsUnauthorized(exception))
            {
                // Belirteç geçersiz (çıkış, yeni eşleştirme, 90 gün): eski oturumla yeniden denenmez (spec 0012 Req 6, 12).
                RaiseSessionRevoked();
                return Task.CompletedTask;
            }

            if (!_lifetime.IsCancellationRequested && !_suspended)
            {
                EnsureConnectLoop();
            }

            return Task.CompletedTask;
        };
        return hub;
    }

    private AgentToolCheckReply CheckTools(IStageRunner stages, AgentToolCheckRequest request)
    {
        var reply = stages.CheckTools(request);
        return reply.Missing.Count == 0 || _installer is null ? reply : reply with { Installable = _installer.Describe(reply.Missing) };
    }

    private async Task<AgentInstallReply> InstallAsync(AgentInstallRequest request)
    {
        if (UpdateRequired || _installer is null)
        {
            return new AgentInstallReply(false, request?.Components ?? []);
        }

        _logger.Information("Sunucu bileşen kurulumu istedi (kullanıcı onayladı): {Components}", string.Join(", ", request.Components));
        try
        {
            var reply = await _installer.InstallAsync(request, _lifetime.Token).ConfigureAwait(false);
            await TryReportComponentsAsync(_lifetime.Token).ConfigureAwait(false);
            return reply;
        }
        catch (OperationCanceledException)
        {
            return new AgentInstallReply(false, request.Components); // Ajan kapanıyor.
        }
    }

    private void EnsureConnectLoop()
    {
        // Aynı anda tek döngü: Closed ve Reconnected aynı anda tetiklense de ikinci bir döngü açılmaz.
        if (Interlocked.CompareExchange(ref _loopRunning, 1, 0) == 0)
        {
            _connectLoop = RunConnectLoopAsync(_run.Token);
        }
    }

    private async Task RunConnectLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ConnectLoopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _loopRunning, 0);
        }
    }

    private async Task ConnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SetState(ConnectionState.Connecting);
            try
            {
                // Hub zaten bağlıysa (ör. kayıt sunucuda hata verdi) yalnızca kayıt yeniden denenir;
                // bağlıyken StartAsync çağrılmaz (review round 1, M-1).
                if (_hub!.State == HubConnectionState.Disconnected)
                {
                    await _hub.StartAsync(cancellationToken).ConfigureAwait(false);
                }

                if (_hub.State != HubConnectionState.Connected)
                {
                    // Otomatik yeniden bağlanma sürüyor; bitince Reconnected/Closed döngüyü yeniden başlatır.
                    return;
                }

                await RegisterAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (IsUnauthorized(ex) && !cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("Sunucu ajan oturumunu reddetti; yeniden eşleştirme gerekiyor");
                SetState(ConnectionState.Disconnected);
                RaiseSessionRevoked();
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.Warning(ex, "Sunucuya bağlanılamadı ya da kayıt olunamadı; {Delay} sonra yeniden denenecek", _settings.ReconnectDelay);
                SetState(_hub!.State == HubConnectionState.Connected ? ConnectionState.Connecting : ConnectionState.Disconnected);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await Task.Delay(_settings.ReconnectDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var reply = await _hub!.InvokeAsync<AgentRegisterReply?>(
            AgentHubContract.Register,
            new AgentRegistration(_info.MachineName, _info.Version, _installedComponents?.Invoke()),
            cancellationToken).ConfigureAwait(false);
        if (reply is { UpdateRequired: true })
        {
            _logger.Warning("Sürüm uyumsuz: ajan {Version}, sunucu {ServerVersion}; güncelleme gerekli, komutlar yürütülmeyecek", _info.Version, reply.ServerVersion);
        }

        SetUpdateRequired(reply?.UpdateRequired == true, reply?.ServerVersion ?? _info.Version);
        SetState(ConnectionState.Connected);
        _logger.Information("Sunucuya bağlanıldı ve kayıt olundu: {Server}", _settings.ServerUrl);
    }
}
