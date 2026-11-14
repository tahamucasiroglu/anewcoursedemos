using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.DependencyInjection;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Runtime;
using TahaKonsept.Agent.Runtime.Abstract;
using TahaKonsept.Agent.SingleInstance.Abstract;
using TahaKonsept.Agent.Tray.Abstract;

namespace TahaKonsept.Agent;

/// <summary>
/// Avalonia uygulaması: tema (App.axaml, sistem açık/koyu modunu izler), servis sağlayıcıyı kurar, tepsi ikonunu gösterir,
/// <see cref="IDesktopAgentRuntime"/>'ı başlatır, tek örnek etkinleştirmelerini pencereye çevirir ve "Çıkış"ta her şeyi
/// sırayla kapatır (<see cref="ExitAsync"/>: çekirdek durdur → <c>ServiceProvider.DisposeAsync</c> → uygulama kapat).
/// </summary>
public partial class App : Application
{
    private readonly ISingleInstanceGuard? _guard;
    private readonly string[] _args;
    private ServiceProvider? _services;
    private ILogger<App>? _logger;
    private int _exiting;

    /// <summary>Tasarımcı/önizleme için.</summary>
    public App()
        : this(null, [])
    {
    }

    /// <summary>Oluşturur.</summary>
    /// <param name="guard">Tek örnek kilidi.</param>
    /// <param name="args">Komut satırı.</param>
    public App(ISingleInstanceGuard? guard, string[] args)
    {
        _guard = guard;
        _args = args;
    }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _services = new ServiceCollection()
                .AddDesktopAgent(DesktopAgentServiceCollectionExtensions.CreateAgentOptions())
                .BuildServiceProvider();
            _logger = _services.GetRequiredService<ILogger<App>>();
            HookUnhandledExceptions();

            var tray = _services.GetRequiredService<ITrayIconController>();
            tray.ExitRequested += (_, _) => _ = ExitAsync(desktop);
            tray.Show(this);

            if (_guard is not null)
            {
                _guard.ActivationRequested += (_, _) => Dispatcher.UIThread.Post(() => _services?.GetService<IWindowNavigator>()?.ShowDefault());
                _guard.StartListening();
            }

            desktop.Exit += (_, _) => StopSynchronously();
            _ = StartRuntimeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartRuntimeAsync()
    {
        try
        {
            var runtime = _services!.GetRequiredService<IDesktopAgentRuntime>();
            await runtime.StartAsync(DesktopLaunchOptions.Parse(_args), CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Ajan başlatılamadı.");
        }
    }

    private async Task ExitAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (Interlocked.Exchange(ref _exiting, 1) == 1)
        {
            return;
        }

        _logger?.LogInformation("Çıkış istendi.");
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            if (_services is not null)
            {
                await _services.GetRequiredService<IDesktopAgentRuntime>().StopAsync(timeout.Token).ConfigureAwait(true);
                _services.GetService<ITrayIconController>()?.Dispose();
                await _services.DisposeAsync().ConfigureAwait(true);
                _services = null;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Kapanışta hata.");
        }
        finally
        {
            desktop.Shutdown();
        }
    }

    private void StopSynchronously()
    {
        if (Interlocked.Exchange(ref _exiting, 1) == 1 || _services is null)
        {
            return;
        }

        // Oturum kapanışı vb. ile doğrudan kapanış: UI iş parçacığını kilitlememek için arka planda, süre sınırıyla.
        var services = _services;
        _services = null;
        Task.Run(async () =>
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await services.GetRequiredService<IDesktopAgentRuntime>().StopAsync(timeout.Token).ConfigureAwait(false);
            await services.DisposeAsync().ConfigureAwait(false);
        }).Wait(TimeSpan.FromSeconds(10));
    }

    private void HookUnhandledExceptions()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            _logger?.LogCritical(e.ExceptionObject as Exception, "Yakalanmamış hata (kapanıyor: {Terminating}).", e.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger?.LogError(e.Exception, "Gözlenmeyen görev hatası.");
            e.SetObserved();
        };
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            _logger?.LogError(e.Exception, "Arayüz hatası.");
            e.Handled = true;
        };
    }
}
