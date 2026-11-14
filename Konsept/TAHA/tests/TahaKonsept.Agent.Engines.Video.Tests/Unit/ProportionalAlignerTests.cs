using Shouldly;
using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Engines.Video.Align.Concrete;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>Oransal hizalayıcı: bilinen aralık dağıtımı, bilinmeyen segmentler, yazılış çıktısı, monotonluk.</summary>
public sealed class ProportionalAlignerTests
{
    /// <summary>Zamanı bilinen segment kendi aralığına harf ağırlığıyla dağıtılır; çıktı yazılış kelimeleridir.</summary>
    [Fact]
    public async Task KnownSegments_DistributeWithinRange()
    {
        var aligner = new ProportionalAligner(new FixedDurationProbe(99));
        var request = new AlignRequest("a.wav",
        [
            new CaptionSegment("Google yeni", "Gugıl yeni", 1.0, 2.0),
            new CaptionSegment("1453 yılında", "bin dört yüz elli üç yılında", 2.5, 4.0),
        ], "tr");

        var result = await aligner.AlignAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var words = result.Value;
        words.Select(w => w.Text).ShouldBe(["Google", "yeni", "1453", "yılında"]);
        words[0].StartMs.ShouldBe(1000);
        words[1].EndMs.ShouldBe(2000);
        words[2].StartMs.ShouldBe(2500);
        words[^1].EndMs.ShouldBe(4000);
        words[0].EndMs.ShouldBeGreaterThan(words[0].StartMs + 400); // "Google" (6 harf) > "yeni" (4 harf)
    }

    /// <summary>Zamanı bilinmeyen segmentler tüm sese okunuş uzunluğuyla paylaştırılır.</summary>
    [Fact]
    public async Task UnknownSegments_UseWholeAudioByCharacterWeight()
    {
        var aligner = new ProportionalAligner(new FixedDurationProbe(10));
        var request = new AlignRequest("a.wav",
        [
            new CaptionSegment("Kısa.", "Kısa.", null, null),
            new CaptionSegment("Bu segment çok daha uzun bir cümle içeriyor.", "Bu segment çok daha uzun bir cümle içeriyor.", null, null),
        ], "tr");

        var words = (await aligner.AlignAsync(request, TestContext.Current.CancellationToken)).Value;

        words[0].StartMs.ShouldBe(0);
        words[^1].EndMs.ShouldBe(10_000);
        words[0].EndMs.ShouldBeLessThan(2000);
        for (var i = 1; i < words.Count; i++)
        {
            words[i].StartMs.ShouldBeGreaterThanOrEqualTo(words[i - 1].EndMs);
        }
    }

    /// <summary>Bilinen ve bilinmeyen karışık: bilinmeyen, komşu sınırlar arasında kalır.</summary>
    [Fact]
    public void ResolveRanges_FillsGapBetweenKnownSegments()
    {
        var ranges = ProportionalAligner.ResolveRanges(
        [
            new CaptionSegment("a", "a", 0, 1),
            new CaptionSegment("bb", "bb", null, null),
            new CaptionSegment("c", "c", 3, 4),
        ], 5000);

        ranges[1].ShouldBe((1000, 3000));
    }

    /// <summary>Segment yoksa boş liste.</summary>
    [Fact]
    public async Task NoSegments_ReturnsEmpty() =>
        (await new ProportionalAligner(new FixedDurationProbe(1)).AlignAsync(new AlignRequest("a.wav", [], "tr"), TestContext.Current.CancellationToken)).Value.ShouldBeEmpty();
}
