using Shouldly;
using TahaKonsept.Agent.Core.VideoServer;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.VideoServer;

/// <summary><see cref="HttpRangeParser"/> tablo testleri (1000 baytlık dosya).</summary>
public sealed class HttpRangeParserTests
{
    private const long Length = 1000;

    /// <summary>Geçerli tek aralıklar doğru çözülür.</summary>
    /// <param name="header">Başlık.</param>
    /// <param name="start">Beklenen başlangıç.</param>
    /// <param name="end">Beklenen bitiş.</param>
    [Theory]
    [InlineData("bytes=0-99", 0, 99)]
    [InlineData("bytes=100-", 100, 999)]
    [InlineData("bytes=-200", 800, 999)]
    [InlineData("bytes=-5000", 0, 999)]
    [InlineData("bytes=990-5000", 990, 999)]
    [InlineData("BYTES= 5 - 9 ", 5, 9)]
    [InlineData("bytes=999-999", 999, 999)]
    public void Parse_Partial(string header, long start, long end)
    {
        var result = HttpRangeParser.Parse(header, Length);

        result.Outcome.ShouldBe(RangeParseOutcome.Partial);
        result.Range.ShouldBe(new ByteRange(start, end));
    }

    /// <summary>Başlık yok, geçersiz ya da çoklu aralık: tüm dosya.</summary>
    /// <param name="header">Başlık.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("items=0-5")]
    [InlineData("bytes=abc-def")]
    [InlineData("bytes=50-10")]
    [InlineData("bytes=0-10,20-30")]
    [InlineData("bytes=10")]
    [InlineData("bytes=-")]
    public void Parse_Full(string? header)
    {
        HttpRangeParser.Parse(header, Length).Outcome.ShouldBe(RangeParseOutcome.Full);
    }

    /// <summary>Dosya dışında başlayan ya da sıfır son ek: 416.</summary>
    /// <param name="header">Başlık.</param>
    [Theory]
    [InlineData("bytes=1000-")]
    [InlineData("bytes=1500-1600")]
    [InlineData("bytes=-0")]
    public void Parse_Unsatisfiable(string header)
    {
        HttpRangeParser.Parse(header, Length).Outcome.ShouldBe(RangeParseOutcome.Unsatisfiable);
    }

    /// <summary>Boş dosyada son ek aralığı karşılanamaz.</summary>
    [Fact]
    public void Parse_EmptyFile_SuffixIsUnsatisfiable()
    {
        HttpRangeParser.Parse("bytes=-10", 0).Outcome.ShouldBe(RangeParseOutcome.Unsatisfiable);
    }

    /// <summary>Content-Range biçimleri.</summary>
    [Fact]
    public void ContentRange_Formats()
    {
        HttpRangeParser.ContentRange(new ByteRange(0, 99), Length).ShouldBe("bytes 0-99/1000");
        HttpRangeParser.UnsatisfiedContentRange(Length).ShouldBe("bytes */1000");
        new ByteRange(10, 19).Length.ShouldBe(10);
    }
}
