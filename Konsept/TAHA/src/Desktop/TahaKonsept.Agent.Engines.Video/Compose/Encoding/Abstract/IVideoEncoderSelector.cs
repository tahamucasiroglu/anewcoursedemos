namespace TahaKonsept.Agent.Engines.Video.Compose.Encoding.Abstract;

/// <summary>
/// Birleştiricinin H.264 kodlayıcı seçicisi ("Önce GPU"): NVENC kullanılabiliyorsa <c>h264_nvenc</c>, değilse nedeniyle <c>libx264</c>.
/// Yoklama ffmpeg yolu başına bir kez yapılır (<see cref="SelectAsync"/>); kodlama sırasında NVENC düşerse <see cref="ReportGpuFailure"/>
/// ile süreç boyunca yazılıma geçilir. Her düşüş <c>Warning</c> olarak loglanır.
/// </summary>
public interface IVideoEncoderSelector
{
    /// <summary>Kodlayıcıyı seçer (ilk çağrıda yoklar, sonra önbellekten).</summary>
    /// <param name="ffmpegPath">ffmpeg yolu.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Seçim.</returns>
    Task<VideoEncoderChoice> SelectAsync(string ffmpegPath, CancellationToken ct);

    /// <summary>NVENC kodlaması başarısız oldu: bu süreçte artık yazılım kodlayıcı seçilir.</summary>
    /// <param name="reason">Neden (loga yazılır).</param>
    /// <returns>Bundan sonra kullanılacak (yazılım) seçim.</returns>
    VideoEncoderChoice ReportGpuFailure(string reason);
}
