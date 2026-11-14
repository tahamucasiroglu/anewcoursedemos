namespace TahaKonsept.Agent.Engines.Video.Compose.Encoding;

/// <summary>
/// Kodlamanın amacı (kalite/hız dengesi buna göre seçilir): <see cref="Intermediate"/> sahne normalize ara dosyası
/// (libx264 crf 18 veryfast ≈ NVENC cq 18 p4), <see cref="Final"/> son video (libx264 crf 19 medium ≈ NVENC cq 19 p5).
/// </summary>
public enum VideoEncodePurpose
{
    /// <summary>Sahne normalize ara dosyası (hızlı, yüksek kalite).</summary>
    Intermediate = 0,

    /// <summary>Son video (altyazı/çubuk basılmış).</summary>
    Final = 1,
}
