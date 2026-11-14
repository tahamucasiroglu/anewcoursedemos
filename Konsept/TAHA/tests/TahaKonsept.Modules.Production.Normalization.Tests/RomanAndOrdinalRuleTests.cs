using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Romen rakamları ve rakamlı sıra sayıları; yanlış pozitif koruması.</summary>
public sealed class RomanAndOrdinalRuleTests
{
    /// <summary>Romen rakamları.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("II. Mehmed", "İkinci Mehmed")]
    [InlineData("XV. yüzyıl", "on beşinci yüzyıl")]
    [InlineData("XV yüzyıl", "on beşinci yüzyıl")]
    [InlineData("I. Dünya Savaşı", "Birinci Dünya Savaşı")]
    [InlineData("II. Dünya Savaşı'nda", "İkinci Dünya Savaşı'nda")]
    [InlineData("III. Selim", "Üçüncü Selim")]
    [InlineData("IV. Murad", "Dördüncü Murad")]
    [InlineData("XIX. yüzyılda", "on dokuzuncu yüzyılda")]
    [InlineData("XX. yy.", "yirminci yüzyıl.")]
    [InlineData("XIV. Louis", "On dördüncü Louis")]
    public void Roman(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Romen rakamı sanılmaması gerekenler.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("I am here", "I am here")]
    [InlineData("V harfi", "V harfi")]
    [InlineData("X ekseni", "X ekseni")]
    [InlineData("M. Kemal geldi", "M. Kemal geldi")]
    [InlineData("C. Ronaldo", "C. Ronaldo")]
    [InlineData("C vitamini", "C vitamini")]
    [InlineData("IIII. bölüm", "ı ı ı ı. bölüm")]
    public void Roman_false_positives(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Rakamlı sıra sayıları.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("3. sırada", "üçüncü sırada")]
    [InlineData("21. yüzyıl", "yirmi birinci yüzyıl")]
    [InlineData("1. Dünya Savaşı", "Birinci Dünya Savaşı")]
    [InlineData("3. Selim", "Üçüncü Selim")]
    [InlineData("100. yıl", "yüzüncü yıl")]
    [InlineData("Yıl 1453. Sonra", "Yıl bin dört yüz elli üç. Sonra")]
    [InlineData("Puan 3.", "Puan üç.")]
    [InlineData("4. bölüm", "dördüncü bölüm")]
    public void Ordinal(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);
}
