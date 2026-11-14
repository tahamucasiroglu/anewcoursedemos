namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Bir adresi varsayılan tarayıcıda açar (tray "Siteye git").
/// </summary>
public interface IBrowserLauncher
{
    /// <summary>Adresi açar.</summary>
    /// <param name="address">Açılacak adres.</param>
    void Open(Uri address);
}
