using Avalonia;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Hub;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.Tray.Abstract;
using TahaKonsept.Agent.ViewModels.Concrete;
using TahaKonsept.Agent.WebSite.Abstract;

namespace TahaKonsept.Agent.Tray.Base;

/// <summary>
/// Tepsi ikonu davranışı (platformdan bağımsız kısım): menü eylemleri (<see cref="OpenWebSite"/>, <see cref="OpenStatus"/>,
/// <see cref="RequestExit"/>), hub durumundan ipucu metni (<see cref="BuildToolTip"/>) ve olay aboneliği. İkonu çizmek türetilen sınıftadır.
/// </summary>
public abstract class TrayIconControllerBase : ITrayIconController
{
    private readonly IAgentHubConnection _hub;
    private readonly IUiDispatcher _dispatcher;
    private bool _disposed;

    /// <summary>Oluşturur.</summary>
    /// <param name="hub">Hub bağlantısı.</param>
    /// <param name="navigator">Pencere yöneticisi.</param>
    /// <param name="launcher">Tarayıcı açıcı.</param>
    /// <param name="webSite">Web adresi.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    protected TrayIconControllerBase(
        IAgentHubConnection hub,
        IWindowNavigator navigator,
        IDesktopLauncher launcher,
        IWebSiteLocator webSite,
        IUiDispatcher dispatcher,
        ILogger logger)
    {
        _hub = hub;
        _dispatcher = dispatcher;
        Navigator = navigator;
        Launcher = launcher;
        WebSite = webSite;
        Logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler? ExitRequested;

    /// <summary>Pencere yöneticisi.</summary>
    protected IWindowNavigator Navigator { get; }

    /// <summary>Tarayıcı açıcı.</summary>
    protected IDesktopLauncher Launcher { get; }

    /// <summary>Web adresi.</summary>
    protected IWebSiteLocator WebSite { get; }

    /// <summary>Log.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public void Show(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        CreateIcon(application, BuildToolTip(_hub.Status));
        _hub.StatusChanged += OnHubStatusChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hub.StatusChanged -= OnHubStatusChanged;
        RemoveIcon();
        GC.SuppressFinalize(this);
    }

    /// <summary>İpucu metni: ürün adı + bağlantı durumu.</summary>
    /// <param name="status">Hub durumu.</param>
    /// <returns>Metin.</returns>
    public static string BuildToolTip(HubConnectionStatus status) => "Taha Konsept — " + StatusViewModel.DescribeHubStatus(status);

    /// <summary>"Siteye git": web arayüzünü varsayılan tarayıcıda açar.</summary>
    protected void OpenWebSite()
    {
        var result = Launcher.OpenUrl(WebSite.GetWebUrl());
        if (result.IsFailure)
        {
            Logger.LogWarning("Site açılamadı: {Error}", result.Error.Message);
        }
    }

    /// <summary>"Kurulum / Durum": kurulum gerekiyorsa kurulum, değilse durum penceresi.</summary>
    protected void OpenStatus() => Navigator.ShowDefault();

    /// <summary>"Çıkış".</summary>
    protected void RequestExit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>İkonu oluşturup gösterir.</summary>
    /// <param name="application">Uygulama.</param>
    /// <param name="toolTip">İlk ipucu metni.</param>
    protected abstract void CreateIcon(Application application, string toolTip);

    /// <summary>İpucu metnini günceller.</summary>
    /// <param name="toolTip">Metin.</param>
    protected abstract void UpdateToolTip(string toolTip);

    /// <summary>İkonu kaldırır.</summary>
    protected abstract void RemoveIcon();

    private void OnHubStatusChanged(object? sender, HubConnectionStatus status) => _dispatcher.Post(() =>
    {
        if (!_disposed)
        {
            UpdateToolTip(BuildToolTip(status));
        }
    });
}
