namespace TahaKonsept.Agent.Engines.Video.Subtitles;

/// <summary>Altyazı yerleşimi: video boyutu, satır başına en çok kelime ve karakter (tek satırda kalsın diye).</summary>
/// <param name="Width">Video genişliği.</param>
/// <param name="Height">Video yüksekliği.</param>
/// <param name="MaxWordsPerLine">Satır başına en çok kelime.</param>
/// <param name="MaxCharsPerLine">Satır başına en çok karakter (boşluklar dahil; tek kelime bunu aşabilir).</param>
public sealed record SubtitleLayout(int Width, int Height, int MaxWordsPerLine, int MaxCharsPerLine = int.MaxValue)
{
    /// <summary>Dikey (yükseklik &gt; genişlik) mi.</summary>
    public bool IsVertical => Height > Width;

    /// <summary>
    /// Boyuta göre varsayılan yerleşim: en çok 5 kelime; karakter sınırı dikeyde 22, yatayda 42
    /// (punto boyutla orantılı olduğundan çözünürlükten bağımsız).
    /// </summary>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <returns>Yerleşim.</returns>
    public static SubtitleLayout For(int width, int height) => new(width, height, 5, height > width ? 22 : 42);
}
