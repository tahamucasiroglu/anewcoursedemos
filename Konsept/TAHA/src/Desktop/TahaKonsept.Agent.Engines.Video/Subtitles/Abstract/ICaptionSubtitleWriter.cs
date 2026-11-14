using TahaKonsept.Agent.Abstractions.Align;

namespace TahaKonsept.Agent.Engines.Video.Subtitles.Abstract;

/// <summary>
/// Kelime zamanlamalarından altyazı dosyası metni üretir (varsayılan: ASS karaoke).
/// Önemli üyeler: <see cref="FileExtension"/>, <see cref="GroupLines"/>, <see cref="Build"/>.
/// </summary>
public interface ICaptionSubtitleWriter
{
    /// <summary>Dosya uzantısı (ör. <c>.ass</c>).</summary>
    string FileExtension { get; }

    /// <summary>Kelimeleri ekran satırlarına böler (en çok N kelime, uzun duraklama ve cümle sonunda yeni satır).</summary>
    /// <param name="words">Kelimeler.</param>
    /// <param name="layout">Yerleşim.</param>
    /// <returns>Satırlar.</returns>
    IReadOnlyList<CaptionLine> GroupLines(IReadOnlyList<CaptionWord> words, SubtitleLayout layout);

    /// <summary>Altyazı dosyasının tam metnini üretir.</summary>
    /// <param name="words">Kelimeler (yazılış).</param>
    /// <param name="layout">Yerleşim.</param>
    /// <returns>Dosya metni.</returns>
    string Build(IReadOnlyList<CaptionWord> words, SubtitleLayout layout);
}
