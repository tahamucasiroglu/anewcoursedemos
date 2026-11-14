using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Runtime.Base;
using TahaKonsept.Agent.Settings.Abstract;
using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.Updates.Abstract;

namespace TahaKonsept.Agent.Runtime.Concrete;

/// <summary>Uygulama yaşam döngüsü; UI işleri <see cref="IUiDispatcher"/> ile gönderilir.</summary>
public sealed class DesktopAgentRuntime : DesktopAgentRuntimeBase
{
    private readonly IUiDispatcher _dispatcher;

    /// <summary>Oluşturur.</summary>
    /// <param name="host">Ajan çekirdeği.</param>
    /// <param name="startup">Windows ile başlat kaydı.</param>
    /// <param name="settings">Masaüstü ayarları.</param>
    /// <param name="navigator">Pencere yöneticisi.</param>
    /// <param name="updater">Güncelleyici.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    public DesktopAgentRuntime(
        IAgentHost host,
        IStartupRegistration startup,
        IDesktopAppSettingsStore settings,
        IWindowNavigator navigator,
        IAgentUpdater updater,
        IUiDispatcher dispatcher,
        ILogger<DesktopAgentRuntime> logger)
        : base(host, startup, settings, navigator, updater, logger)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    protected override void RunOnUi(Action action) => _dispatcher.Post(action);
}
