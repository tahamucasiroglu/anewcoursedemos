using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.VideoServer.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Hosting.Base;

/// <summary>
/// Ajan yaşam döngüsü tabanı: başlarken video sunucusu (seçenek açıksa) + hub; video portu ayarlardan değişirse sunucu yeniden başlar.
/// Dururken önce iş çalıştırıcı (bekleyenler <c>agent.shutdown</c> raporlanır), sonra hub ve video sunucusu kapanır.
/// </summary>
public abstract class AgentHostBase : IAgentHost
{
    private readonly IVideoServer _videoServer;
    private readonly IJobRunner _jobs;
    private readonly IAgentSettingsStore _settings;
    private readonly AgentOptions _options;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private int _videoPort;

    /// <summary>Oluşturur.</summary>
    /// <param name="hub">Hub bağlantısı.</param>
    /// <param name="pairing">Eşleme servisi.</param>
    /// <param name="prerequisites">Kurulum yöneticisi.</param>
    /// <param name="videoServer">Video sunucusu.</param>
    /// <param name="jobs">İş çalıştırıcı.</param>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="options">Seçenekler.</param>
    protected AgentHostBase(
        IAgentHubConnection hub,
        IDevicePairingService pairing,
        IPrerequisiteInstaller prerequisites,
        IVideoServer videoServer,
        IJobRunner jobs,
        IAgentSettingsStore settings,
        AgentOptions options)
    {
        Hub = hub;
        Pairing = pairing;
        Prerequisites = prerequisites;
        _videoServer = videoServer;
        _jobs = jobs;
        _settings = settings;
        _options = options;
    }

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public IAgentHubConnection Hub { get; }

    /// <inheritdoc />
    public IDevicePairingService Pairing { get; }

    /// <inheritdoc />
    public IPrerequisiteInstaller Prerequisites { get; }

    /// <inheritdoc />
    public async Task<Result> StartAsync(CancellationToken ct)
    {
        await _lifecycle.WaitAsync(ct);
        try
        {
            if (IsRunning)
            {
                return Result.Success();
            }

            var settings = _settings.Current;
            OnStarting(settings);
            var result = Result.Success();
            if (_options.StartVideoServer)
            {
                result = await _videoServer.StartAsync(ct);
                _videoPort = settings.VideoServerPort;
            }

            await Hub.StartAsync(ct);
            _settings.Changed += OnSettingsChanged;
            IsRunning = true;
            return result;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        await _lifecycle.WaitAsync(ct);
        try
        {
            if (!IsRunning)
            {
                return;
            }

            _settings.Changed -= OnSettingsChanged;
            await _jobs.StopAsync(ct);
            await Hub.StopAsync(ct);
            await _videoServer.StopAsync(ct);
            IsRunning = false;
            OnStopped();
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>Başlarken çağrılır (loglama).</summary>
    /// <param name="settings">Güncel ayarlar.</param>
    protected virtual void OnStarting(AgentSettings settings)
    {
    }

    /// <summary>Durunca çağrılır.</summary>
    protected virtual void OnStopped()
    {
    }

    /// <summary>Arka plan işleminde hata olunca çağrılır.</summary>
    /// <param name="exception">İstisna.</param>
    protected virtual void OnBackgroundError(Exception exception)
    {
    }

    private void OnSettingsChanged(object? sender, AgentSettings settings)
    {
        if (!_options.StartVideoServer || settings.VideoServerPort == _videoPort)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _videoServer.StopAsync(CancellationToken.None);
                var restarted = await _videoServer.StartAsync(CancellationToken.None);
                if (restarted.IsSuccess)
                {
                    _videoPort = settings.VideoServerPort;
                    await Hub.SendHelloAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                OnBackgroundError(ex);
            }
        });
    }
}
