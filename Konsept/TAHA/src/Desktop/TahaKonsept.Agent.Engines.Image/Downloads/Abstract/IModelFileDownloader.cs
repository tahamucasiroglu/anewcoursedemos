using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.Downloads.Abstract;

/// <summary>
/// Büyük dosya indirici: <c>.part</c> dosyasına yazar, kesintide HTTP Range ile kaldığı yerden devam eder,
/// boyut ve isteğe bağlı SHA-256 doğrular, sonra hedefe taşır.
/// </summary>
public interface IModelFileDownloader
{
    /// <summary>Dosyayı indirir; hedef zaten doğru boyutta varsa hemen başarı döner.</summary>
    /// <param name="source">Kaynak adres.</param>
    /// <param name="destinationPath">Hedef dosya.</param>
    /// <param name="expectedSize">Beklenen boyut (bilinmiyorsa <c>null</c>).</param>
    /// <param name="sha256">Beklenen SHA-256 hex (isteğe bağlı).</param>
    /// <param name="progress">İlerleme (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    Task<Result> DownloadAsync(Uri source, string destinationPath, long? expectedSize, string? sha256, IProgress<ModelDownloadProgress>? progress, CancellationToken ct);
}
