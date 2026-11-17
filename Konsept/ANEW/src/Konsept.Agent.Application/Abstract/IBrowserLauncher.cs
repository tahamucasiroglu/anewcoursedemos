namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Bir adresi varsayılan tarayıcıda açar (tray "Siteye git").
/// </summary>
public interface IBrowserLauncher
{
    /// <summary>
    /// Adresi açar. Tarayıcı açılamazsa (varsayılan tarayıcı yok, kabuk ilişkilendirmesi bozuk; B-153) hata fırlatmaz, <c>false</c> döner —
    /// çağıran adresi kullanıcıya gösterir. Yalnızca http/https dışındaki adres <see cref="ArgumentException"/> fırlatır.
    /// </summary>
    /// <param name="address">Açılacak adres.</param>
    /// <returns>Tarayıcı açıldıysa <c>true</c>.</returns>
    bool Open(Uri address);
}
