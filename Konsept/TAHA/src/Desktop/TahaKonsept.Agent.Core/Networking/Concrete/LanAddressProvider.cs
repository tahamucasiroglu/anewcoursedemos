using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Networking.Base;

namespace TahaKonsept.Agent.Core.Networking.Concrete;

/// <summary>LAN adresi bulucu: hedef olarak ayarlardaki sunucu adresinin ana makinesini kullanır.</summary>
public sealed class LanAddressProvider : LanAddressProviderBase
{
    private readonly IAgentSettingsStore _settings;

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Ayar deposu.</param>
    public LanAddressProvider(IAgentSettingsStore settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    protected override string? GetTargetHost() =>
        Uri.TryCreate(_settings.Current.ServerBaseUrl, UriKind.Absolute, out var uri) ? uri.Host : null;
}
