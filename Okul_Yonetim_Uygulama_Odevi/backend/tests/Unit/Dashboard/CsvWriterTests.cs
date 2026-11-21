using System.Text;
using OkulYonetim.Application.Features.Dashboard.Export;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Dashboard;

/// <summary>
/// Spec 0017 AC-10 (biçim) ve AC-11 (formül kaçışı): dosya UTF-8 BOM ile başlar, ayırıcı <c>;</c>, satır sonu CRLF; tutar "1234,50" (binlik
/// ayırıcısız, ondalık virgül), gün "GG.AA.YYYY"; <c>;</c> ya da <c>"</c> içeren metin tırnaklanır; <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>,
/// sekme ve satır başıyla başlayan metin hücresine <c>'</c> eklenir, tutar ve sayı sütunları kaçışlanmaz (sayı kalır).
/// </summary>
public sealed class CsvWriterTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    [Fact]
    public void Write_HeaderAndRows_BomSemicolonCrlfTurkishFormats()
    {
        byte[] file = CsvWriter.Write(
            ["Öğrenci", "Tutar (TL)", "Tarih", "Makbuz no"],
            [[CsvCell.Of("Çağla Şimşek"), CsvCell.Money(1234.5m), CsvCell.Date(new DateOnly(2026, 11, 5)), CsvCell.Number(12)]]);

        file.Take(3).ShouldBe(Bom);
        string text = Encoding.UTF8.GetString(file, 3, file.Length - 3);
        text.ShouldBe("Öğrenci;Tutar (TL);Tarih;Makbuz no\r\nÇağla Şimşek;1234,50;05.11.2026;12\r\n");
    }

    [Theory]
    [InlineData("1234567.89", "1234567,89")]
    [InlineData("0.1", "0,10")]
    [InlineData("10000", "10000,00")]
    [InlineData("41500.00", "41500,00")]
    public void Field_Money_DecimalCommaNoGrouping(string amount, string expected) =>
        CsvWriter.Field(CsvCell.Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture))).ShouldBe(expected);

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+905551112233", "'+905551112233")]
    [InlineData("-5", "'-5")]
    [InlineData("@SUM(A1:A2)", "'@SUM(A1:A2)")]
    [InlineData("\tSekme", "'\tSekme")]
    [InlineData("\rSatır", "\"'\rSatır\"")]
    [InlineData("\nSatır", "\"'\nSatır\"")]
    [InlineData("Ali Yılmaz", "Ali Yılmaz")]
    [InlineData("Ali=Veli", "Ali=Veli")]
    public void Field_TextStartingWithFormulaCharacter_IsPrefixedWithApostrophe(string value, string expected) =>
        CsvWriter.Field(CsvCell.Of(value)).ShouldBe(expected);

    [Theory]
    [InlineData("Yılmaz; Ali", "\"Yılmaz; Ali\"")]
    [InlineData("Ali \"Can\" Kaya", "\"Ali \"\"Can\"\" Kaya\"")]
    [InlineData("=HYPERLINK(\"x\";\"y\")", "\"'=HYPERLINK(\"\"x\"\";\"\"y\"\")\"")]
    public void Field_TextWithSeparatorOrQuote_IsQuotedAndQuotesDoubled(string value, string expected) =>
        CsvWriter.Field(CsvCell.Of(value)).ShouldBe(expected);

    [Fact]
    public void Field_NumbersAndAmounts_AreNotEscaped()
    {
        CsvWriter.Field(CsvCell.Number(-5)).ShouldBe("-5");
        CsvWriter.Field(CsvCell.Money(-5m)).ShouldBe("-5,00");
        CsvWriter.Field(CsvCell.Empty).ShouldBe(string.Empty);
    }

    [Fact]
    public void Write_HeaderCellsAreEscapedLikeText()
    {
        byte[] file = CsvWriter.Write(["=Başlık", "A;B"], []);
        Encoding.UTF8.GetString(file, 3, file.Length - 3).ShouldBe("'=Başlık;\"A;B\"\r\n");
    }
}
