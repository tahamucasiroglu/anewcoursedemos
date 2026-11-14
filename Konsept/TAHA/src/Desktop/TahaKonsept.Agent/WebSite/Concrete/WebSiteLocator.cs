using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Settings.Abstract;
using TahaKonsept.Agent.WebSite.Base;

namespace TahaKonsept.Agent.WebSite.Concrete;

/// <summary>Web adresini masaüstü ayarlarından, API adresini çekirdek ayarlarından (boşsa <see cref="AgentOptions"/> varsayılanı) okur.</summary>
public sealed class WebSiteLocator : WebSiteLocatorBase
{
    private readonly IDesktopAppSettingsStore _desktopSettings;
    private readonly IAgentSettingsStore _agentSettings;
    private readonly AgentOptions _options;

    /// <summary>Oluşturur.</summary>
    /// <param name="desktopSettings">Masaüstü ayarları.</param>
    /// <param name="agentSettings">Çekirdek ayarları.</param>
    /// <param name="options">Çekirdek seçenekleri.</param>
    public WebSiteLocator(IDesktopAppSettingsStore desktopSettings, IAgentSettingsStore agentSettings, AgentOptions options)
    {
        _desktopSettings = desktopSettings;
        _agentSettings = agentSettings;
        _options = options;
    }

    /// <inheritdoc />
    protected override string? GetConfiguredWebUrl() => _desktopSettings.Current.WebBaseUrl;

    /// <inheritdoc />
    protected override string GetServerUrl()
    {
        var url = _agentSettings.Current.ServerBaseUrl;
        return string.IsNullOrWhiteSpace(url) ? _options.DefaultServerBaseUrl : url;
    }
}
