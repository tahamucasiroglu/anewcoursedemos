using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Text;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Sayı → Türkçe açık yazı (asıl, sıra, ondalık, büyük sayılar, baştaki sıfırlar).</summary>
public sealed class NumberToTurkishWordsTests
{
    /// <summary>Asıl sayılar.</summary>
    /// <param name="value">Sayı.</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData(0L, "sıfır")]
    [InlineData(1L, "bir")]
    [InlineData(10L, "on")]
    [InlineData(11L, "on bir")]
    [InlineData(40L, "kırk")]
    [InlineData(99L, "doksan dokuz")]
    [InlineData(100L, "yüz")]
    [InlineData(101L, "yüz bir")]
    [InlineData(200L, "iki yüz")]
    [InlineData(1000L, "bin")]
    [InlineData(1001L, "bin bir")]
    [InlineData(1453L, "bin dört yüz elli üç")]
    [InlineData(1923L, "bin dokuz yüz yirmi üç")]
    [InlineData(2025L, "iki bin yirmi beş")]
    [InlineData(21000L, "yirmi bir bin")]
    [InlineData(100000L, "yüz bin")]
    [InlineData(1000000L, "bir milyon")]
    [InlineData(1001000L, "bir milyon bin")]
    [InlineData(1250000L, "bir milyon iki yüz elli bin")]
    [InlineData(3000000000L, "üç milyar")]
    [InlineData(7000000000000L, "yedi trilyon")]
    [InlineData(-5L, "eksi beş")]
    [InlineData(-1453L, "eksi bin dört yüz elli üç")]
    public void Cardinal(long value, string expected) => NumberToTurkishWords.ToWords(value).ShouldBe(expected);

    /// <summary>Sıra sayıları.</summary>
    /// <param name="value">Sayı.</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData(1L, "birinci")]
    [InlineData(2L, "ikinci")]
    [InlineData(3L, "üçüncü")]
    [InlineData(4L, "dördüncü")]
    [InlineData(6L, "altıncı")]
    [InlineData(9L, "dokuzuncu")]
    [InlineData(10L, "onuncu")]
    [InlineData(15L, "on beşinci")]
    [InlineData(21L, "yirmi birinci")]
    [InlineData(40L, "kırkıncı")]
    [InlineData(60L, "altmışıncı")]
    [InlineData(100L, "yüzüncü")]
    [InlineData(1000L, "bininci")]
    public void Ordinal(long value, string expected) => NumberToTurkishWords.ToOrdinal(value).ShouldBe(expected);

    /// <summary>Ondalık, baştaki sıfırlar ve min değer.</summary>
    [Fact]
    public void Decimals_leading_zeros_and_extremes()
    {
        NumberToTurkishWords.ToDecimalWords("3", "5").ShouldBe("üç virgül beş");
        NumberToTurkishWords.ToDecimalWords("3", "05").ShouldBe("üç virgül sıfır beş");
        NumberToTurkishWords.ToWords("007").ShouldBe("sıfır sıfır yedi");
        NumberToTurkishWords.ToWords("1234567890123456789012").ShouldStartWith("bir iki üç");
        NumberToTurkishWords.ToWords(long.MinValue).ShouldStartWith("eksi dokuz kentilyon");
    }
}
