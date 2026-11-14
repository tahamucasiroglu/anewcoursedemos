using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Media.Abstract;

/// <summary>FFmpeg/ffprobe tabanlı medya yardımcıları: süre ölçme, küçük resim çıkarma, ses birleştirme.</summary>
public interface IMediaProbe
{
    /// <summary>Medya süresini (sn) ölçer.</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Süre ya da hata.</returns>
    Task<Result<double>> GetDurationAsync(string path, CancellationToken ct);

    /// <summary>Videodan tek kare küçük resim (jpg) çıkarır.</summary>
    /// <param name="videoPath">Video.</param>
    /// <param name="outputPath">Çıktı .jpg.</param>
    /// <param name="atSec">Kare zamanı (sn).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> ExtractThumbnailAsync(string videoPath, string outputPath, double atSec, CancellationToken ct);

    /// <summary>Ses dosyalarını FFmpeg <c>concat</c> filtresiyle birleştirir.</summary>
    /// <param name="inputs">Girdiler.</param>
    /// <param name="outputPath">Çıktı .wav.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> ConcatAudioAsync(IReadOnlyList<string> inputs, string outputPath, CancellationToken ct);
}
