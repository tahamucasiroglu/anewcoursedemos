using Shouldly;
using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Engines.Video.Subtitles;
using TahaKonsept.Agent.Engines.Video.Subtitles.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>ASS karaoke yazıcısı: satır gruplama, \k süreleri, zaman biçimi, Türkçe karakterler, dikey/yatay punto.</summary>
public sealed class AssKaraokeSubtitleWriterTests
{
    private readonly AssKaraokeSubtitleWriter _writer = new();

    /// <summary>Satır en çok N kelime içerir ve cümle sonunda bölünür.</summary>
    [Fact]
    public void GroupLines_SplitsByMaxWordsAndSentenceEnd()
    {
        var words = Words("Bir iki üç dört beş altı. Yedi sekiz");
        var lines = _writer.GroupLines(words, new SubtitleLayout(1080, 1920, 5));

        lines.Select(l => string.Join(' ', l.Words.Select(w => w.Text))).ShouldBe(["Bir iki üç dört beş", "altı.", "Yedi sekiz"]);
    }

    /// <summary>Uzun duraklama yeni satır başlatır.</summary>
    [Fact]
    public void GroupLines_BreaksOnLongPause()
    {
        var words = new List<CaptionWord> { new("Merhaba", 0, 400), new("dünya", 2000, 2400) };
        _writer.GroupLines(words, new SubtitleLayout(1920, 1080, 5)).Count.ShouldBe(2);
    }

    /// <summary>\k değerleri bir sonraki kelimenin başlangıcına kadar santisaniye; metin Türkçe karakterleri korur.</summary>
    [Fact]
    public void Build_WritesKaraokeTagsAndTurkishText()
    {
        var words = new List<CaptionWord> { new("İstanbul'un", 1000, 1600), new("fethi", 1700, 2200), new("çağ", 2200, 2500), new("değiştirdi.", 2500, 3300) };
        var ass = _writer.Build(words, SubtitleLayout.For(1920, 1080));

        ass.ShouldContain("PlayResX: 1920");
        ass.ShouldContain("PlayResY: 1080");
        ass.ShouldContain("Dialogue: 0,0:00:01.00,0:00:03.30,Karaoke,,0,0,0,,{\\k70}İstanbul'un {\\k50}fethi {\\k30}çağ {\\k80}değiştirdi.");
        ass.ShouldContain(",-1,0,0,0,"); // kalın
    }

    /// <summary>Dikeyde karakter sınırı uzun Türkçe kelimeleri tek satıra sığacak şekilde böler.</summary>
    [Fact]
    public void GroupLines_RespectsCharacterLimitOnVertical()
    {
        var words = Words("Merhaba dünya, İstanbul'un fethi");
        var lines = _writer.GroupLines(words, SubtitleLayout.For(1080, 1920));

        lines.Select(l => string.Join(' ', l.Words.Select(w => w.Text))).ShouldBe(["Merhaba dünya,", "İstanbul'un fethi"]);
    }

    /// <summary>Dikey ve yatay videoda punto ve alt boşluk farklıdır.</summary>
    [Fact]
    public void FontSize_DiffersForVerticalAndHorizontal()
    {
        var vertical = SubtitleLayout.For(1080, 1920);
        var horizontal = SubtitleLayout.For(1920, 1080);

        AssKaraokeSubtitleWriter.FontSizeFor(vertical).ShouldBe(76);
        AssKaraokeSubtitleWriter.FontSizeFor(horizontal).ShouldBe(62);
        AssKaraokeSubtitleWriter.MarginVFor(vertical).ShouldBeGreaterThan(AssKaraokeSubtitleWriter.MarginVFor(horizontal));
        vertical.MaxWordsPerLine.ShouldBeLessThanOrEqualTo(5);
    }

    /// <summary>Zaman biçimi h:mm:ss.cc.</summary>
    [Theory]
    [InlineData(0, "0:00:00.00")]
    [InlineData(61_234, "0:01:01.23")]
    [InlineData(3_723_450, "1:02:03.45")]
    public void FormatTime_UsesAssFormat(int ms, string expected) => AssKaraokeSubtitleWriter.FormatTime(ms).ShouldBe(expected);

    /// <summary>Süslü parantez override sayılmaz.</summary>
    [Fact]
    public void EscapeText_NeutralizesOverrides() => AssKaraokeSubtitleWriter.EscapeText("a{\\b1}b").ShouldBe("a(/b1)b");

    private static List<CaptionWord> Words(string text) =>
        text.Split(' ').Select((w, i) => new CaptionWord(w, i * 300, (i * 300) + 280)).ToList();
}
