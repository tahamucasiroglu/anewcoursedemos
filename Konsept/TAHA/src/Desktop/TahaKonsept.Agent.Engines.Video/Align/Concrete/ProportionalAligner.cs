using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Engines.Video.Align.Base;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Align.Concrete;

/// <summary>
/// Varsayılan hizalayıcı (model gerektirmez). Zamanı bilinen segmentlerde [başlangıç, bitiş] aralığı, segmentin yazılış kelimelerine
/// harf ağırlığıyla dağıtılır. Zamanı bilinmeyen ardışık segmentler, çevreleyen bilinen sınırlar (yoksa 0 ve ses süresi) arasındaki
/// aralığı okunuş metinlerinin karakter ağırlığıyla paylaşır. Çıktı her zaman <b>yazılış</b> kelimeleridir (ekrandaki "Google",
/// sesteki "Gugıl" olsa da). Ses süresi ffprobe ile okunur (yalnızca bilinmeyen segment varsa gerekir).
/// </summary>
/// <param name="probe">Ses süresi okuyucu.</param>
public sealed class ProportionalAligner(IMediaProbe probe) : AlignerBase
{
    /// <summary>Hizalayıcı anahtarı.</summary>
    public const string AlignerKey = "proportional";

    /// <inheritdoc />
    public override string Key => AlignerKey;

    /// <inheritdoc />
    protected override async Task<Result<IReadOnlyList<CaptionWord>>> AlignCoreAsync(AlignRequest request, CancellationToken ct)
    {
        var segments = request.Segments;
        var needsAudio = segments.Any(s => s.StartSec is null || s.EndSec is null);
        var audioMs = 0;
        if (needsAudio)
        {
            var duration = await probe.GetDurationAsync(request.AudioPath, ct).ConfigureAwait(false);
            if (duration.IsFailure)
            {
                return Result.Failure<IReadOnlyList<CaptionWord>>(duration.Error);
            }

            audioMs = ToMs(duration.Value);
        }

        var ranges = ResolveRanges(segments, audioMs);
        var words = new List<CaptionWord>();
        for (var i = 0; i < segments.Count; i++)
        {
            words.AddRange(Distribute(SplitWords(segments[i].DisplayText), ranges[i].StartMs, ranges[i].EndMs));
        }

        return Result.Success<IReadOnlyList<CaptionWord>>(MakeMonotonic(words));
    }

    /// <summary>Her segment için ms aralığını çözer (bilinmeyenler çevreleyen sınırlar arasında okunuş uzunluğuyla paylaştırılır).</summary>
    /// <param name="segments">Segmentler.</param>
    /// <param name="audioMs">Toplam ses süresi (ms; bilinmeyen segment yoksa kullanılmaz).</param>
    /// <returns>Segment başına (başlangıç, bitiş).</returns>
    public static IReadOnlyList<(int StartMs, int EndMs)> ResolveRanges(IReadOnlyList<CaptionSegment> segments, int audioMs)
    {
        var ranges = new (int StartMs, int EndMs)[segments.Count];
        var i = 0;
        while (i < segments.Count)
        {
            var s = segments[i];
            if (s.StartSec is { } a && s.EndSec is { } b)
            {
                ranges[i] = (ToMs(a), ToMs(b));
                i++;
                continue;
            }

            var runStart = i;
            while (i < segments.Count && (segments[i].StartSec is null || segments[i].EndSec is null))
            {
                i++;
            }

            var lower = runStart > 0 ? ranges[runStart - 1].EndMs : 0;
            lower = segments[runStart].StartSec is { } knownStart ? ToMs(knownStart) : lower;
            var upper = i < segments.Count && segments[i].StartSec is { } nextStart ? ToMs(nextStart) : audioMs;
            upper = segments[i - 1].EndSec is { } knownEnd ? ToMs(knownEnd) : upper;
            upper = Math.Max(upper, lower + (i - runStart));

            var weights = Enumerable.Range(runStart, i - runStart)
                .Select(k => Math.Max(1d, (string.IsNullOrWhiteSpace(segments[k].SpokenText) ? segments[k].DisplayText : segments[k].SpokenText).Count(c => !char.IsWhiteSpace(c))))
                .ToArray();
            var total = weights.Sum();
            var cursor = 0d;
            for (var k = runStart; k < i; k++)
            {
                var from = lower + (int)Math.Round((upper - lower) * cursor / total);
                cursor += weights[k - runStart];
                var to = k == i - 1 ? upper : lower + (int)Math.Round((upper - lower) * cursor / total);
                ranges[k] = (from, Math.Max(from + 1, to));
            }
        }

        return ranges;
    }

    private static IReadOnlyList<CaptionWord> MakeMonotonic(List<CaptionWord> words)
    {
        var result = new List<CaptionWord>(words.Count);
        var last = 0;
        foreach (var w in words)
        {
            var start = Math.Max(w.StartMs, last);
            var end = Math.Max(start + 1, w.EndMs);
            result.Add(w with { StartMs = start, EndMs = end });
            last = end;
        }

        return result;
    }

    private static int ToMs(double seconds) => (int)Math.Round(Math.Max(0, seconds) * 1000);
}
