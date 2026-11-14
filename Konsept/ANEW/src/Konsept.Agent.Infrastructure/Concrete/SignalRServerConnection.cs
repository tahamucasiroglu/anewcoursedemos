using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Connection;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR.Client;

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
    private readonly CancellationTokenSource _lifetime = new();
    private HubConnection? _hub;
    private Task _connectLoop = Task.CompletedTask;

    /// <summary>Initializes a new instance of the <see cref="SignalRServerConnection"/> class.</summary>
    /// <param name="settings">Ajan ayarları.</param>
    /// <param name="info">Ajan bilgisi.</param>
    /// <param name="handlerFactory">Testte HTTP yolunu değiştirmek için; normalde <c>null</c>.</param>
    public SignalRServerConnection(AgentSettings settings, IAgentInfoProvider info, Func<HttpMessageHandler>? handlerFactory = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _handlerFactory = handlerFactory;
    }

    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _hub ??= BuildHub();
        _connectLoop = ConnectLoopAsync(_lifetime.Token);
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
        hub.Reconnected += _ => RegisterAsync(_lifetime.Token);
        hub.Closed += _ =>
        {
            SetState(ConnectionState.Disconnected);
            if (!_lifetime.IsCancellationRequested)
            {
                _connectLoop = ConnectLoopAsync(_lifetime.Token);
            }

            return Task.CompletedTask;
        };
        return hub;
    }

    private async Task ConnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SetState(ConnectionState.Connecting);
            try
            {
                await _hub!.StartAsync(cancellationToken).ConfigureAwait(false);
                await RegisterAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                SetState(ConnectionState.Disconnected);
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
    }
}
