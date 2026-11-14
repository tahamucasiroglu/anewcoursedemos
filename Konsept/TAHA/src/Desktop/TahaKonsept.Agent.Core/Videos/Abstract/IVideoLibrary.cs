using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Videos.Abstract;

/// <summary>
/// Yerel video kütüphanesi: son videoyu kaydetme, Klasörde aç, Sistem oynatıcısında aç, Sil (dosyalar + çalışma alanı + kayıt).
/// Hub'daki <c>OpenFolder</c>/<c>OpenInSystemPlayer</c>/<c>DeleteVideo</c> komutları buraya gelir.
/// </summary>
public interface IVideoLibrary
{
    /// <summary>Kaydı bulur.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    VideoEntry? Find(Guid videoId);

    /// <summary>Videoyu dizine ekler.</summary>
    /// <param name="entry">Kayıt.</param>
    void Register(VideoEntry entry);

    /// <summary>Video dosyasını Gezgin'de seçili gösterir.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Sonuç.</returns>
    Result OpenFolder(Guid videoId);

    /// <summary>Videoyu sistem oynatıcısında açar.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Sonuç.</returns>
    Result OpenInSystemPlayer(Guid videoId);

    /// <summary>Video dosyasını, küçük resmini, iş çalışma alanını ve kaydı siler.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Sonuç.</returns>
    Result Delete(Guid videoId);
}
