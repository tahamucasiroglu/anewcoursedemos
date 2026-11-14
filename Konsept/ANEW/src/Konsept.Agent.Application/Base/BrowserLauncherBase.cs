using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Tarayıcı açıcıların temeli: yalnızca http/https adreslerini kabul eder.
/// </summary>
public abstract class BrowserLauncherBase : IBrowserLauncher
{
    /// <inheritdoc />
    public void Open(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Yalnızca http/https adresleri açılır.", nameof(address));
        }

        Launch(address);
    }

    /// <summary>Adresi işletim sisteminin varsayılan tarayıcısında açar.</summary>
    /// <param name="address">Doğrulanmış adres.</param>
    protected abstract void Launch(Uri address);
}
