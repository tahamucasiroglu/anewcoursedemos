using TahaKonsept.Agent.Abstractions.Align;

namespace TahaKonsept.Agent.Engines.Video.Subtitles;

/// <summary>Ekranda birlikte gösterilen kelime grubu (altyazı satırı).</summary>
/// <param name="Words">Satırdaki kelimeler (zaman sırasıyla, en az bir kelime).</param>
public sealed record CaptionLine(IReadOnlyList<CaptionWord> Words)
{
    /// <summary>Satır başlangıcı (ms).</summary>
    public int StartMs => Words[0].StartMs;

    /// <summary>Satır bitişi (ms).</summary>
    public int EndMs => Words[^1].EndMs;
}
