using System.Globalization;

namespace TahaKonsept.Agent.Core.VideoServer;

/// <summary>
/// RFC 9110 <c>Range: bytes=…</c> çözücü (tek aralık). Desteklenen biçimler: <c>bytes=a-b</c>, <c>bytes=a-</c>, <c>bytes=-n</c> (son n bayt).
/// Geçersiz söz dizimi ya da çoklu aralık yok sayılır (tüm dosya); dosya dışındaki başlangıç <see cref="RangeParseOutcome.Unsatisfiable"/>'dır.
/// </summary>
public static class HttpRangeParser
{
    private const string Prefix = "bytes=";

    /// <summary>Başlığı çözer.</summary>
    /// <param name="header">Range başlığı (boş olabilir).</param>
    /// <param name="length">Dosya uzunluğu (bayt).</param>
    /// <returns>Sonuç.</returns>
    public static RangeParseResult Parse(string? header, long length)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return new RangeParseResult(RangeParseOutcome.Full);
        }

        var value = header.Trim();
        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return new RangeParseResult(RangeParseOutcome.Full);
        }

        var spec = value[Prefix.Length..].Trim();
        if (spec.Length == 0 || spec.Contains(',', StringComparison.Ordinal))
        {
            return new RangeParseResult(RangeParseOutcome.Full);
        }

        var dash = spec.IndexOf('-', StringComparison.Ordinal);
        if (dash < 0)
        {
            return new RangeParseResult(RangeParseOutcome.Full);
        }

        var startText = spec[..dash].Trim();
        var endText = spec[(dash + 1)..].Trim();

        if (startText.Length == 0)
        {
            if (!TryParse(endText, out var suffix))
            {
                return new RangeParseResult(RangeParseOutcome.Full);
            }

            if (suffix == 0 || length == 0)
            {
                return new RangeParseResult(RangeParseOutcome.Unsatisfiable);
            }

            return Partial(Math.Max(0, length - suffix), length - 1);
        }

        if (!TryParse(startText, out var start))
        {
            return new RangeParseResult(RangeParseOutcome.Full);
        }

        long end;
        if (endText.Length == 0)
        {
            end = length - 1;
        }
        else if (!TryParse(endText, out end) || end < start)
        {
            return new RangeParseResult(RangeParseOutcome.Full);
        }

        if (start >= length)
        {
            return new RangeParseResult(RangeParseOutcome.Unsatisfiable);
        }

        return Partial(start, Math.Min(end, length - 1));
    }

    /// <summary><c>Content-Range</c> başlık değeri üretir.</summary>
    /// <param name="range">Aralık.</param>
    /// <param name="length">Dosya uzunluğu.</param>
    /// <returns>Ör. <c>bytes 0-99/1000</c>.</returns>
    public static string ContentRange(ByteRange range, long length) =>
        string.Create(CultureInfo.InvariantCulture, $"bytes {range.Start}-{range.End}/{length}");

    /// <summary>Karşılanamaz aralık için <c>Content-Range</c> değeri.</summary>
    /// <param name="length">Dosya uzunluğu.</param>
    /// <returns>Ör. <c>bytes */1000</c>.</returns>
    public static string UnsatisfiedContentRange(long length) =>
        string.Create(CultureInfo.InvariantCulture, $"bytes */{length}");

    private static RangeParseResult Partial(long start, long end) =>
        new(RangeParseOutcome.Partial, new ByteRange(start, end));

    private static bool TryParse(string text, out long value) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
