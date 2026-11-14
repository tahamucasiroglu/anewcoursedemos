using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Engines.Video.Subtitles.Abstract;

namespace TahaKonsept.Agent.Engines.Video.Subtitles.Base;

/// <summary>
/// <see cref="ICaptionSubtitleWriter"/> ortak davranışı: boş kelimeleri ayıklar, zamana göre sıralar ve satırlara böler.
/// Satır kuralı: en çok <see cref="SubtitleLayout.MaxWordsPerLine"/> kelime ve <see cref="SubtitleLayout.MaxCharsPerLine"/> karakter; kelimeler arası boşluk <see cref="LineBreakGapMs"/>'i aşarsa
/// ya da önceki kelime cümle sonu (. ! ? …) ile bitiyorsa yeni satır. Biçime özgü çıktı <see cref="Render"/> ile üretilir.
/// </summary>
public abstract class CaptionSubtitleWriterBase : ICaptionSubtitleWriter
{
    /// <inheritdoc />
    public abstract string FileExtension { get; }

    /// <summary>Bu süreden uzun duraklamada yeni satıra geçilir (ms).</summary>
    protected virtual int LineBreakGapMs => 700;

    /// <inheritdoc />
    public IReadOnlyList<CaptionLine> GroupLines(IReadOnlyList<CaptionWord> words, SubtitleLayout layout)
    {
        var max = Math.Max(1, layout.MaxWordsPerLine);
        var clean = words
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .Select(w => w with { Text = w.Text.Trim(), EndMs = Math.Max(w.EndMs, w.StartMs + 1) })
            .OrderBy(w => w.StartMs)
            .ToList();

        var lines = new List<CaptionLine>();
        var current = new List<CaptionWord>();
        var chars = 0;
        foreach (var word in clean)
        {
            if (current.Count > 0
                && (current.Count >= max
                    || chars + 1 + word.Text.Length > layout.MaxCharsPerLine
                    || word.StartMs - current[^1].EndMs > LineBreakGapMs
                    || EndsSentence(current[^1].Text)))
            {
                lines.Add(new CaptionLine(current));
                current = [];
                chars = 0;
            }

            chars += (current.Count > 0 ? 1 : 0) + word.Text.Length;
            current.Add(word);
        }

        if (current.Count > 0)
        {
            lines.Add(new CaptionLine(current));
        }

        return lines;
    }

    /// <inheritdoc />
    public string Build(IReadOnlyList<CaptionWord> words, SubtitleLayout layout) => Render(GroupLines(words, layout), layout);

    /// <summary>Satırlardan dosya metnini üretir.</summary>
    /// <param name="lines">Satırlar.</param>
    /// <param name="layout">Yerleşim.</param>
    /// <returns>Dosya metni.</returns>
    protected abstract string Render(IReadOnlyList<CaptionLine> lines, SubtitleLayout layout);

    private static bool EndsSentence(string text) => text.Length > 0 && text[^1] is '.' or '!' or '?' or '…';
}
