using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.Agent.Account.Abstract;
using TahaKonsept.Agent.Account.Concrete;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.DependencyInjection;
using TahaKonsept.Agent.EngineList.Abstract;
using TahaKonsept.Agent.EngineList.Concrete;
using TahaKonsept.Agent.Engines.Image.DependencyInjection;
using TahaKonsept.Agent.Engines.Speech.DependencyInjection;
using TahaKonsept.Agent.Engines.Video.DependencyInjection;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Navigation.Concrete;
using TahaKonsept.Agent.Runtime.Abstract;
using TahaKonsept.Agent.Runtime.Concrete;
using TahaKonsept.Agent.Settings.Abstract;
using TahaKonsept.Agent.Settings.Concrete;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Shell.Concrete;
using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.Agent.Startup.Concrete;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.Threading.Concrete;
using TahaKonsept.Agent.Tray.Abstract;
using TahaKonsept.Agent.Tray.Concrete;
using TahaKonsept.Agent.Updates.Abstract;
using TahaKonsept.Agent.Updates.Concrete;
using TahaKonsept.Agent.ViewModels.Concrete;
using TahaKonsept.Agent.WebSite.Abstract;
using TahaKonsept.Agent.WebSite.Concrete;

namespace TahaKonsept.Agent.DependencyInjection;

/// <summary>
/// Masaüstü uygulamasının bileşim kökü: <c>AddAgentCore(options)</c> + <c>AddVideoEngines</c> + <c>AddSpeechEngines</c> +
/// <c>AddImageEngines</c> ve uygulamanın kendi servisleri (tepsi, pencere yöneticisi, ayarlar, başlangıç kaydı, güncelleyici, görünüm modelleri).
/// <see cref="CreateAgentOptions"/> geliştirici ortam değişkenlerini (<c>TAHAKONSEPT_AGENT_ROOT</c>, <c>TAHAKONSEPT_AGENT_VIDEO_PORT</c>,
/// <c>TAHAKONSEPT_AGENT_SERVER</c>) uygular.
/// </summary>
public static class DesktopAgentServiceCollectionExtensions
{
    /// <summary>Tüm servisleri ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="options">Çekirdek seçenekleri.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddDesktopAgent(this IServiceCollection services, AgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddAgentCore(options);
        services.AddVideoEngines();
        services.AddSpeechEngines();
        services.AddImageEngines();

        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IDesktopAppSettingsStore, JsonDesktopAppSettingsStore>();
        services.AddSingleton<IWebSiteLocator, WebSiteLocator>();
        services.AddSingleton<IDesktopLauncher, WindowsDesktopLauncher>();
        services.AddSingleton<IStartupRegistration, RegistryStartupRegistration>();
        services.AddSingleton<IPairedAccountReader, TokenStorePairedAccountReader>();
        services.AddSingleton<IEngineCatalog, RegisteredEngineCatalog>();
        services.AddSingleton<IAgentUpdater, VelopackAgentUpdater>();
        services.AddSingleton<IWindowNavigator, AvaloniaWindowNavigator>();
        services.AddSingleton<ITrayIconController, AvaloniaTrayIconController>();
        services.AddSingleton<IDesktopAgentRuntime, DesktopAgentRuntime>();

        services.AddTransient<PairingViewModel>();
        services.AddTransient<SetupViewModel>();
        services.AddTransient<StatusViewModel>();
        return services;
    }

    /// <summary>Varsayılan çekirdek seçeneklerini üretir (geliştirici ortam değişkenleriyle).</summary>
    /// <returns>Seçenekler.</returns>
    public static AgentOptions CreateAgentOptions()
    {
        var options = new AgentOptions();
        var root = Environment.GetEnvironmentVariable("TAHAKONSEPT_AGENT_ROOT");
        if (!string.IsNullOrWhiteSpace(root))
        {
            options.RootDirectory = root;
        }

        var port = Environment.GetEnvironmentVariable("TAHAKONSEPT_AGENT_VIDEO_PORT");
        if (int.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) && p is > 0 and < 65536)
        {
            options.VideoServerPort = p;
        }

        var server = Environment.GetEnvironmentVariable("TAHAKONSEPT_AGENT_SERVER");
        if (!string.IsNullOrWhiteSpace(server))
        {
            options.DefaultServerBaseUrl = server.TrimEnd('/');
        }

        return options;
    }
}
