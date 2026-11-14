using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Concrete;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Ek uyumu: ünlü uyumu, d/t benzeşmesi, kaynaştırma, dört → dörd yumuşaması.</summary>
public sealed class SuffixHarmonizerTests
{
    private static readonly TurkishSuffixHarmonizer Harmonizer = new();

    /// <summary>Gövde + ek → okunuş.</summary>
    /// <param name="stem">Gövde.</param>
    /// <param name="suffix">Yazılıştaki ek.</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData("üç", "te", "üçte")]
    [InlineData("üç", "de", "üçte")]
    [InlineData("kırk", "ta", "kırkta")]
    [InlineData("altı", "da", "altıda")]
    [InlineData("yedi", "de", "yedide")]
    [InlineData("dokuz", "u", "dokuzu")]
    [InlineData("on", "dan", "ondan")]
    [InlineData("yüz", "ü", "yüzü")]
    [InlineData("dört", "ü", "dördü")]
    [InlineData("dört", "te", "dörtte")]
    [InlineData("kilogram", "ye", "kilograma")]
    [InlineData("lira", "ye", "liraya")]
    [InlineData("doksan", "larda", "doksanlarda")]
    [InlineData("seksen", "lerde", "seksenlerde")]
    [InlineData("iki", "si", "ikisi")]
    public void Attach(string stem, string suffix, string expected) => Harmonizer.Attach(stem, suffix).ShouldBe(expected);

    /// <summary>Yalnız ek uyumu (gövde değişmez).</summary>
    [Fact]
    public void Harmonize_suffix_only()
    {
        Harmonizer.HarmonizeSuffix("Gugıl", "ın").ShouldBe("ın");
        Harmonizer.HarmonizeSuffix("me", "de").ShouldBe("de");
        Harmonizer.HarmonizeSuffix("Nato", "ya").ShouldBe("ya");
        Harmonizer.HarmonizeSuffix("Epıl", "e").ShouldBe("a");
    }
}
