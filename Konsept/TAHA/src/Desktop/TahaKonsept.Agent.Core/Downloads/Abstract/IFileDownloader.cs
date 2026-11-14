using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Downloads.Abstract;

/// <summary>
/// İnternetten (kimlik doğrulamasız) indirme: ilerleme bildirimi, SHA256 hesaplama ve beklenen hash verilmişse doğrulama.
/// Ön gereksinim kurulumunda kullanılır.
/// </summary>
public interface IFileDownloader
{
    /// <summary>Dosyayı indirir (önce <c>.partial</c>, doğrulanınca son ad).</summary>
    /// <param name="url">Adres.</param>
    /// <param name="destinationPath">Hedef.</param>
    /// <param name="expectedSha256">Beklenen SHA256 (hex; <c>null</c> = doğrulama yok).</param>
    /// <param name="progress">0–100 ilerleme (boyut biliniyorsa).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>İndirilen dosya ya da hata (<c>agent.download_hash_mismatch</c>…).</returns>
    Task<Result<DownloadedFile>> DownloadAsync(Uri url, string destinationPath, string? expectedSha256, IProgress<double>? progress, CancellationToken ct);

    /// <summary>Metin içerik indirir (JSON uç noktaları, SHASUMS dosyaları).</summary>
    /// <param name="url">Adres.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Metin ya da hata.</returns>
    Task<Result<string>> GetStringAsync(Uri url, CancellationToken ct);
}
