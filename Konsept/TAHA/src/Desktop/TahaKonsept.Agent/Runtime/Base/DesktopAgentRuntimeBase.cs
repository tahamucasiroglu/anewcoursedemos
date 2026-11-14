using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Runtime.Abstract;
using TahaKonsept.Agent.Settings.Abstract;
using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.Agent.Updates.Abstract;

namespace TahaKonsept.Agent.Runtime.Base;

/// <summary>
/// <see cref="IDesktopAgentRuntime"/> tabanı: başlangıç sırası (başlangıç kaydı varsayılanı → pencere kararı → çekirdek → güncelleme)
/// ve tek seferlik durdurma. Pencereyi UI iş parçacığında açmak için <see cref="RunOnUi"/> türetilen sınıftadır.
/// </summary>
public abstract class DesktopAgentRuntimeBase : IDesktopAgentRuntime
{
    private readonly IAgentHost _host;
    private readonly IStartupRegistration _startup;
    private readonly IDesktopAppSettingsStore _settings;
    private readonly IWindowNavigator _navigator;
    private readonly IAgentUpdater _updater;
    private int _stopped;

    /// <summary>Oluşturur.</summary>
    /// <param name="host">Ajan çekirdeği.</param>
    /// <param name="startup">Windows ile başlat kaydı.</param>
    /// <param name="settings">Masaüstü ayarları.</param>
    /// <param name="navigator">Pencere yöneticisi.</param>
    /// <param name="updater">Güncelleyici.</param>
    /// <param name="logger">Log.</param>
    protected DesktopAgentRuntimeBase(
        IAgentHost host,
        IStartupRegistration startup,
        IDesktopAppSettingsStore settings,
        IWindowNavigator navigator,
        IAgentUpdater updater,
        ILogger logger)
    {
        _host = host;
        _startup = startup;
        _settings = settings;
        _navigator = navigator;
        _updater = updater;
        Logger = logger;
    }

    /// <summary>Log.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task StartAsync(DesktopLaunchOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);
        Logger.LogInformation("Taha Konsept ajanı başlıyor (oturum açılışından: {FromLogin}).", options.StartedFromLogin);
        ApplyStartupDefault();

        // Bileşen denetimi süreç başlatabilir (node --version vb.); UI iş parçacığını kilitlememek için arka planda.
        var setupRequired = await Task.Run(_navigator.IsSetupRequired, ct).ConfigureAwait(false);
        RunOnUi(() =>
        {
            if (setupRequired)
            {
                _navigator.ShowSetup();
            }
            else if (!options.StartedFromLogin)
            {
                _navigator.ShowStatus();
            }
        });

        var result = await Task.Run(() => _host.StartAsync(ct), ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            Logger.LogWarning("Ajan çekirdeği uyarıyla başladı: {Error}", result.Error.Message);
        }
        else
        {
            Logger.LogInformation("Ajan çekirdeği başladı.");
        }

        _ = Task.Run(() => _updater.CheckAndDownloadAsync(ct), ct);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _stopped, 1) == 1)
        {
            return;
        }

        RunOnUi(_navigator.CloseAll);
        try
        {
            await _host.StopAsync(ct).ConfigureAwait(false);
            Logger.LogInformation("Ajan çekirdeği durduruldu.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Ajan çekirdeği zamanında durmadı; kapanış sürüyor.");
        }

        _updater.ApplyOnExit();
    }

    /// <summary>İşi UI iş parçacığında çalıştırır.</summary>
    /// <param name="action">İş.</param>
    protected abstract void RunOnUi(Action action);

    private void ApplyStartupDefault()
    {
        if (_settings.Current.StartupDefaultApplied)
        {
            return;
        }

        var result = _startup.SetEnabled(true);
        if (result.IsFailure)
        {
            Logger.LogWarning("Windows ile başlat açılamadı: {Error}", result.Error.Message);
            return;
        }

        _settings.Update(s => s with { StartupDefaultApplied = true });
        Logger.LogInformation("Windows ile başlat varsayılan olarak açıldı.");
    }
}
