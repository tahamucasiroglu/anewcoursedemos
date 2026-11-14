using Avalonia;

namespace TahaKonsept.Agent.Tray.Abstract;

/// <summary>
/// Görev çubuğu (sağ alt) tepsi ikonu: menü "Siteye git", "Kurulum / Durum", "Çıkış"; ikona tıklama durum/kurulum penceresini açar,
/// ipucu metni bağlantı durumunu gösterir. "Çıkış" seçilince <see cref="ExitRequested"/> tetiklenir.
/// </summary>
public interface ITrayIconController : IDisposable
{
    /// <summary>Kullanıcı "Çıkış" seçince tetiklenir (UI iş parçacığında).</summary>
    event EventHandler? ExitRequested;

    /// <summary>Tepsi ikonunu uygulamaya ekler ve gösterir.</summary>
    /// <param name="application">Avalonia uygulaması.</param>
    void Show(Application application);
}
