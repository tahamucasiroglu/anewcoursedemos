using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Navigation.Base;
using TahaKonsept.Agent.Settings.Abstract;
using TahaKonsept.Agent.ViewModels.Concrete;
using TahaKonsept.Agent.Views;

namespace TahaKonsept.Agent.Navigation.Concrete;

/// <summary>
/// Avalonia pencere yöneticisi: <see cref="SetupWindow"/> ve <see cref="StatusWindow"/>'u görünüm modelleriyle (DI'dan, geçici) yaratır,
/// açıksa öne getirir, kapanınca görünüm modelini bırakır. Durum penceresindeki "Eşle" kurulum penceresini eşleme adımıyla açar.
/// </summary>
public sealed class AvaloniaWindowNavigator : WindowNavigatorBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AvaloniaWindowNavigator> _logger;
    private SetupWindow? _setup;
    private StatusWindow? _status;

    /// <summary>Oluşturur.</summary>
    /// <param name="services">Servis sağlayıcı.</param>
    /// <param name="settings">Masaüstü ayarları.</param>
    /// <param name="prerequisites">Ön gereksinimler.</param>
    /// <param name="auth">Oturum.</param>
    /// <param name="logger">Log.</param>
    public AvaloniaWindowNavigator(
        IServiceProvider services,
        IDesktopAppSettingsStore settings,
        IPrerequisiteInstaller prerequisites,
        IAgentAuthService auth,
        ILogger<AvaloniaWindowNavigator> logger)
        : base(settings, prerequisites, auth)
    {
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    public override void ShowSetup(bool pairingOnly = false)
    {
        if (_setup is not null)
        {
            if (pairingOnly && _setup.DataContext is SetupViewModel existing)
            {
                existing.SkipToPairingIfReady();
            }

            Activate(_setup);
            return;
        }

        var vm = _services.GetRequiredService<SetupViewModel>();
        var window = new SetupWindow { DataContext = vm };
        vm.CloseRequested += (_, _) => window.Close();
        window.Closed += (_, _) =>
        {
            vm.Dispose();
            _setup = null;
        };
        _setup = window;
        window.Show();
        Activate(window);
        _ = vm.DetectGpuAsync(CancellationToken.None);
        if (pairingOnly)
        {
            vm.SkipToPairingIfReady();
        }

        _logger.LogInformation("Kurulum penceresi açıldı.");
    }

    /// <inheritdoc />
    public override void ShowStatus()
    {
        if (_status is not null)
        {
            Activate(_status);
            return;
        }

        var vm = _services.GetRequiredService<StatusViewModel>();
        var window = new StatusWindow { DataContext = vm };
        vm.PairRequested += (_, _) => ShowSetup(pairingOnly: true);
        window.Closed += (_, _) =>
        {
            vm.Dispose();
            _status = null;
        };
        _status = window;
        window.Show();
        Activate(window);
        _ = vm.LoadAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public override void CloseAll()
    {
        _setup?.Close();
        _status?.Close();
    }

    private static void Activate(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
    }
}
