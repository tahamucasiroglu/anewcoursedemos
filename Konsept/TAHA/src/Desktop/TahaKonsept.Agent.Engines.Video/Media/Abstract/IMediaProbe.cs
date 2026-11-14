using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Media.Abstract;

/// <summary>
/// Medya dosyası bilgisi okuyucu (ffprobe). Render sonrası gerçek süreyi ölçmek ve birleştirmede sahne sürelerini
/// hesaplamak için kullanılır. Önemli üye: <see cref="GetDurationAsync"/>.
/// </summary>
public interface IMediaProbe
{
    /// <summary>Dosyanın süresini (sn) döner.</summary>
    /// <param name="path">Video/ses dosyası.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Süre ya da hata.</returns>
    Task<Result<double>> GetDurationAsync(string path, CancellationToken ct);

    /// <summary>Dosyada ses akışı var mı.</summary>
    /// <param name="path">Medya dosyası.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ses akışı varsa <c>true</c>.</returns>
    Task<Result<bool>> HasAudioAsync(string path, CancellationToken ct);
}
