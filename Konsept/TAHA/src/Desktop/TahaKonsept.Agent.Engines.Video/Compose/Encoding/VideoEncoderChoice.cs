namespace TahaKonsept.Agent.Engines.Video.Compose.Encoding;

/// <summary>
/// Seçilen H.264 kodlayıcı ("Önce GPU"): <see cref="Nvenc"/> (<c>h264_nvenc</c>, NVIDIA GPU) ya da <see cref="Software"/>
/// (<c>libx264</c>, yalnız yedek). <see cref="FallbackReason"/> GPU kodlayıcı kullanılamadıysa nedenidir.
/// <see cref="Arguments"/> amaca göre ffmpeg kodlayıcı argümanlarını üretir (piksel biçimi her zaman <c>yuv420p</c>).
/// NVENC kalite eşlemesi: <c>-rc vbr -cq N -b:v 0</c> (sabit kalite, bit hızı sınırı yok) libx264 <c>-crf N</c> karşılığıdır.
/// </summary>
/// <param name="Encoder">ffmpeg kodlayıcı adı (<c>h264_nvenc</c> ya da <c>libx264</c>).</param>
/// <param name="IsGpu">GPU kodlayıcı mı.</param>
/// <param name="FallbackReason">GPU kodlayıcı kullanılamadıysa nedeni; aksi hâlde <c>null</c>.</param>
public sealed record VideoEncoderChoice(string Encoder, bool IsGpu, string? FallbackReason)
{
    /// <summary>NVIDIA NVENC H.264 kodlayıcı adı.</summary>
    public const string NvencEncoder = "h264_nvenc";

    /// <summary>Yazılım H.264 kodlayıcı adı.</summary>
    public const string SoftwareEncoder = "libx264";

    /// <summary>NVENC seçimi.</summary>
    public static VideoEncoderChoice Nvenc { get; } = new(NvencEncoder, true, null);

    /// <summary>Tasarım gereği yazılım kodlayıcı (düşüş değil).</summary>
    public static VideoEncoderChoice Software { get; } = new(SoftwareEncoder, false, null);

    /// <summary>GPU kodlayıcıdan yazılıma düşüş.</summary>
    /// <param name="reason">Neden (loga yazılır).</param>
    /// <returns>Seçim.</returns>
    public static VideoEncoderChoice SoftwareFallback(string reason) => new(SoftwareEncoder, false, reason);

    /// <summary>Amaca göre kodlayıcı argümanları (<c>-c:v …</c> ve kalite ayarları, <c>-pix_fmt yuv420p</c>).</summary>
    /// <param name="purpose">Kodlama amacı.</param>
    /// <returns>ffmpeg argümanları.</returns>
    public IReadOnlyList<string> Arguments(VideoEncodePurpose purpose) => (IsGpu, purpose) switch
    {
        (true, VideoEncodePurpose.Intermediate) =>
            ["-c:v", NvencEncoder, "-preset", "p4", "-tune", "hq", "-rc", "vbr", "-cq", "18", "-b:v", "0", "-pix_fmt", "yuv420p"],
        (true, _) =>
            ["-c:v", NvencEncoder, "-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", "19", "-b:v", "0", "-spatial-aq", "1", "-pix_fmt", "yuv420p"],
        (false, VideoEncodePurpose.Intermediate) =>
            ["-c:v", SoftwareEncoder, "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p"],
        _ =>
            ["-c:v", SoftwareEncoder, "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p"],
    };
}
