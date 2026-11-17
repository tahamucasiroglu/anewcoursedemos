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

    /// <summary>
    /// npm bileşenini kurar (spec 0018 / B-118): ajana gömülü <c>package.json</c> + <c>package-lock.json</c> hedefe yazılır, kilit dosyasının
    /// SHA-256'sı <paramref name="lockSha256"/> ile karşılaştırılır, kurulu Node'un npm'iyle <c>npm ci --ignore-scripts</c> çalışır.
    /// </summary>
    /// <param name="componentId">Bileşen kimliği (gömülü dosyaların klasörü).</param>
    /// <param name="lockSha256">Kilit dosyasının beklenen SHA-256'sı.</param>
    /// <param name="nodeDirectory">Kurulu Node klasörü.</param>
    /// <param name="destination">Hedef klasör.</param>
    /// <param name="cancellationToken">İptal.</param>
    /// <returns>Kurulduysa <c>true</c>.</returns>
    Task<bool> InstallPackagesAsync(string componentId, string lockSha256, string nodeDirectory, string destination, CancellationToken cancellationToken);

    /// <summary>
    /// Python paket ortamını kurar (spec 0028 Req 4): ajana gömülü kilit dosyası (<c>python/&lt;id&gt;[.&lt;varyant&gt;].txt</c>) okunur, SHA-256'sı
    /// <paramref name="lockSha256"/> ile karşılaştırılır, uv ile her paketi hash'li olarak <paramref name="destination"/>'a kurulur. uv önbelleği
    /// <paramref name="cacheDirectory"/>'dedir ve sonra silinir; kullanıcının uv/pip yapılandırmasına ve kayıt defterine dokunulmaz.
    /// </summary>
    /// <param name="componentId">Bileşen kimliği.</param>
    /// <param name="variant">Varyant ya da <c>null</c>.</param>
    /// <param name="lockSha256">Kilit dosyasının beklenen SHA-256'sı.</param>
    /// <param name="uvExecutable">Kurulu uv.</param>
    /// <param name="pythonExecutable">Kurulu taşınabilir Python.</param>
    /// <param name="destination">Hedef klasör.</param>
    /// <param name="cacheDirectory">Geçici önbellek klasörü.</param>
    /// <param name="cancellationToken">İptal.</param>
    /// <returns>Kurulduysa <c>true</c>.</returns>
    Task<bool> InstallPythonPackagesAsync(
        string componentId,
        string? variant,
        string lockSha256,
        string uvExecutable,
        string pythonExecutable,
        string destination,
        string cacheDirectory,
        CancellationToken cancellationToken);
}
