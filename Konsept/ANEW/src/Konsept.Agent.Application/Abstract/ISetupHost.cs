namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Kurulum motorunun dış dünyası (spec 0009): ağ, disk alanı, indirme, hash, arşiv açma ve araç çalıştırma. Gerçeği Windows'ta
/// (<c>WindowsSetupHost</c>); testlerde sahte (gerçek indirme yok).
/// </summary>
public interface ISetupHost
{
    /// <summary>İnternet bağlantısı var mı.</summary>
    /// <returns>Var mı.</returns>
    bool IsOnline();

    /// <summary>Klasörün bulunduğu sürücüdeki boş alan.</summary>
    /// <param name="directory">Klasör.</param>
    /// <returns>Bayt.</returns>
    long FreeBytes(string directory);

    /// <summary>Dosyayı indirir (hedef üzerine yazılır).</summary>
    /// <param name="source">Adres.</param>
    /// <param name="destination">Hedef dosya.</param>
    /// <param name="progress">0–1 arası ilerleme.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task DownloadAsync(Uri source, string destination, IProgress<double>? progress, CancellationToken cancellationToken);

    /// <summary>Dosyanın SHA-256'sını hesaplar.</summary>
    /// <param name="file">Dosya.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Küçük harf onaltılık hash.</returns>
    Task<string> ComputeSha256Async(string file, CancellationToken cancellationToken);

    /// <summary>Arşivi klasöre açar.</summary>
    /// <param name="archive">Arşiv.</param>
    /// <param name="destination">Hedef klasör.</param>
    void ExtractArchive(string archive, string destination);

    /// <summary>Aracı çalıştırır ve çıktısını döndürür (Hazır denetimi).</summary>
    /// <param name="executable">Yürütülebilir dosya.</param>
    /// <param name="arguments">Argümanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Standart çıktı + hata; çalıştırılamazsa <c>null</c>.</returns>
    Task<string?> RunAsync(string executable, string arguments, CancellationToken cancellationToken);
}
