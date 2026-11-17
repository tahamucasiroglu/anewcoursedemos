using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Tarayıcı açıcıların temeli: yalnızca http/https adreslerini kabul eder; açma hatası ajanı düşürmez (B-153: kullanıcının tıkladığı düğme
/// <c>FileNotFoundException</c> ile ajanı kapatıyordu) — önce yedek yol denenir, o da olmazsa <c>false</c>.
/// </summary>
public abstract class BrowserLauncherBase : IBrowserLauncher
{
    /// <inheritdoc />
    public bool Open(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Yalnızca http/https adresleri açılır.", nameof(address));
        }

        try
        {
            Launch(address);
            return true;
        }
        catch (Exception first) when (first is not OutOfMemoryException)
        {
            try
            {
                LaunchFallback(address);
                OnLaunchFailed(address, first, recovered: true);
                return true;
            }
            catch (Exception second) when (second is not OutOfMemoryException)
            {
                OnLaunchFailed(address, second, recovered: false);
                return false;
            }
        }
    }

    /// <summary>Birincil yol başarısız olunca denenen yedek açma yolu.</summary>
    /// <param name="address">Doğrulanmış adres.</param>
    protected abstract void LaunchFallback(Uri address);

    /// <summary>Açma hatasını bildirir (log); varsayılan hiçbir şey yapmaz.</summary>
    /// <param name="address">Adres.</param>
    /// <param name="exception">Hata.</param>
    /// <param name="recovered">Yedek yol açtıysa <c>true</c>.</param>
    protected virtual void OnLaunchFailed(Uri address, Exception exception, bool recovered)
    {
    }

    /// <summary>Adresi işletim sisteminin varsayılan tarayıcısında açar.</summary>
    /// <param name="address">Doğrulanmış adres.</param>
    protected abstract void Launch(Uri address);
}
