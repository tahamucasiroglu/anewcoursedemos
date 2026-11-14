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
/// komutuna versiyon ve makine adıyla cevap verir.
/// </summary>
public sealed class SignalRServerConnection : ServerConnectionBase
{
    private readonly AgentSettings _settings;
    private readonly IAgentInfoProvider _info;
    private readonly Func<HttpMessageHandler>? _handlerFactory;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _lifetime = new();
    private HubConnection? _hub;
    private Task _connectLoop = Task.CompletedTask;
    private int _loopRunning;

    /// <summary>Initializes a new instance of the <see cref="SignalRServerConnection"/> class.</summary>
    /// <param name="settings">Ajan ayarları.</param>
    /// <param name="info">Ajan bilgisi.</param>
    /// <param name="handlerFactory">Testte HTTP yolunu değiştirmek için; normalde <c>null</c>.</param>
    /// <param name="logger">Ajan teknik logu (spec 0008); verilmezse log yazılmaz.</param>
    public SignalRServerConnection(AgentSettings settings, IAgentInfoProvider info, Func<HttpMessageHandler>? handlerFactory = null, ILogger? logger = null)
    {
        _logger = (logger ?? Serilog.Core.Logger.None).ForContext<SignalRServerConnection>();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _handlerFactory = handlerFactory;
    }

    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _hub ??= BuildHub();
        EnsureConnectLoop();
        return Task.CompletedTask;
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
    protected override async ValueTask DisposeCoreAsync()
    {
        await _lifetime.CancelAsync().ConfigureAwait(false);
        if (_hub is not null)
        {
            await _hub.DisposeAsync().ConfigureAwait(false);
        }

        await _connectLoop.ConfigureAwait(false);
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
            })
            .WithAutomaticReconnect(new ForeverRetryPolicy(_settings.ReconnectDelay))
            .Build();

        hub.On(AgentHubContract.Ping, () => new AgentPingReply(_info.MachineName, _info.Version));
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
        hub.Closed += _ =>
        {
            SetState(ConnectionState.Disconnected);
            if (!_lifetime.IsCancellationRequested)
            {
                EnsureConnectLoop();
            }

            return Task.CompletedTask;
        };
        return hub;
    }

    private void EnsureConnectLoop()
    {
        // Aynı anda tek döngü: Closed ve Reconnected aynı anda tetiklense de ikinci bir döngü açılmaz.
        if (Interlocked.CompareExchange(ref _loopRunning, 1, 0) == 0)
        {
            _connectLoop = RunConnectLoopAsync(_lifetime.Token);
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
        await _hub!.InvokeAsync(
            AgentHubContract.Register,
            new AgentRegistration(_info.MachineName, _info.Version),
            cancellationToken).ConfigureAwait(false);
        SetState(ConnectionState.Connected);
        _logger.Information("Sunucuya bağlanıldı ve kayıt olundu: {Server}", _settings.ServerUrl);
    }
}
