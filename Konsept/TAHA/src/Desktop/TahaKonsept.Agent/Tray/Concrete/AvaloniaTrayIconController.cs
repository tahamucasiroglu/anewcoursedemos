using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.Tray.Base;
using TahaKonsept.Agent.WebSite.Abstract;

namespace TahaKonsept.Agent.Tray.Concrete;

/// <summary>Avalonia <see cref="TrayIcon"/> ile tepsi ikonu ve yerel (native) sağ tık menüsü.</summary>
public sealed class AvaloniaTrayIconController : TrayIconControllerBase
{
    /// <summary>Gömülü ikon adresi.</summary>
    public const string IconUri = "avares://TahaKonsept.Agent/Assets/app.ico";

    private TrayIcon? _icon;
    private Application? _application;

    /// <summary>Oluşturur.</summary>
    /// <param name="hub">Hub bağlantısı.</param>
    /// <param name="navigator">Pencere yöneticisi.</param>
    /// <param name="launcher">Tarayıcı açıcı.</param>
    /// <param name="webSite">Web adresi.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    public AvaloniaTrayIconController(
        IAgentHubConnection hub,
        IWindowNavigator navigator,
        IDesktopLauncher launcher,
        IWebSiteLocator webSite,
        IUiDispatcher dispatcher,
        ILogger<AvaloniaTrayIconController> logger)
        : base(hub, navigator, launcher, webSite, dispatcher, logger)
    {
    }

    /// <inheritdoc />
    protected override void CreateIcon(Application application, string toolTip)
    {
        _application = application;
        var menu = new NativeMenu();
        menu.Items.Add(CreateItem("Siteye git", OpenWebSite));
        menu.Items.Add(CreateItem("Kurulum / Durum", OpenStatus));
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(CreateItem("Çıkış", RequestExit));

        using var stream = AssetLoader.Open(new Uri(IconUri));
        _icon = new TrayIcon
        {
            Icon = new WindowIcon(stream),
            ToolTipText = toolTip,
            Menu = menu,
            IsVisible = true,
        };
        _icon.Clicked += (_, _) => OpenStatus();
        TrayIcon.SetIcons(application, [_icon]);
        Logger.LogInformation("Tepsi ikonu gösterildi.");
    }

    /// <inheritdoc />
    protected override void UpdateToolTip(string toolTip)
    {
        if (_icon is not null)
        {
            _icon.ToolTipText = toolTip;
        }
    }

    /// <inheritdoc />
    protected override void RemoveIcon()
    {
        if (_icon is null)
        {
            return;
        }

        _icon.IsVisible = false;
        if (_application is not null)
        {
            TrayIcon.SetIcons(_application, []);
        }

        _icon.Dispose();
        _icon = null;
    }

    private static NativeMenuItem CreateItem(string header, Action action)
    {
        var item = new NativeMenuItem(header);
        item.Click += (_, _) => action();
        return item;
    }
}
