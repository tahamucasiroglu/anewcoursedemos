using TahaKonsept.Agent.WebSite.Abstract;

namespace TahaKonsept.Agent.WebSite.Base;

/// <summary>
/// <see cref="IWebSiteLocator"/> tabanı: açık web adresi varsa onu, yoksa API adresinden türetilmişini döner
/// (<see cref="DeriveFromServerUrl"/>: API portu 7111 → web portu 7110; başka port aynen kalır).
/// </summary>
public abstract class WebSiteLocatorBase : IWebSiteLocator
{
    /// <summary>API varsayılan portu.</summary>
    public const int ApiPort = 7111;

    /// <summary>Web varsayılan portu.</summary>
    public const int WebPort = 7110;

    /// <inheritdoc />
    public string GetWebUrl()
    {
        var configured = GetConfiguredWebUrl();
        if (!string.IsNullOrWhiteSpace(configured) && Uri.TryCreate(configured, UriKind.Absolute, out var uri))
        {
            return uri.AbsoluteUri;
        }

        return DeriveFromServerUrl(GetServerUrl());
    }

    /// <summary>API adresinden web adresini türetir.</summary>
    /// <param name="serverUrl">API adresi.</param>
    /// <returns>Web adresi.</returns>
    public static string DeriveFromServerUrl(string serverUrl)
    {
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
        {
            return "http://localhost:" + WebPort + "/";
        }

        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port == ApiPort ? WebPort : uri.Port, "/");
        return builder.Uri.AbsoluteUri;
    }

    /// <summary>Ayardaki açık web adresi (yoksa <c>null</c>).</summary>
    /// <returns>Adres ya da <c>null</c>.</returns>
    protected abstract string? GetConfiguredWebUrl();

    /// <summary>Güncel API adresi.</summary>
    /// <returns>Adres.</returns>
    protected abstract string GetServerUrl();
}
