using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Kısaltmalar, birimler, harf harf okunan kısaltmalar.</summary>
public sealed class AbbreviationRuleTests
{
    /// <summary>Noktalı kısaltmalar ve birimler.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("16. yy.'da", "on altıncı yüzyılda")]
    [InlineData("kitap, defter vb. şeyler", "kitap, defter ve benzeri şeyler")]
    [InlineData("kalem, silgi vs.", "kalem, silgi vesaire.")]
    [InlineData("Dr. Ahmet", "doktor Ahmet")]
    [InlineData("Prof. Dr. Ayşe", "profesör doktor Ayşe")]
    [InlineData("Prof. Ali", "profesör Ali")]
    [InlineData("5 km", "beş kilometre")]
    [InlineData("25km yol", "yirmi beş kilometre yol")]
    [InlineData("5 km'den", "beş kilometreden")]
    [InlineData("10 kg'lık", "on kilogramlık")]
    [InlineData("2 kg'ye", "iki kilograma")]
    [InlineData("100 m²", "yüz metrekare")]
    [InlineData("30 °C", "otuz derece")]
    [InlineData("30°C", "otuz derece")]
    [InlineData("10 sn", "on saniye")]
    [InlineData("15 dk", "on beş dakika")]
    [InlineData("3 m", "üç metre")]
    [InlineData("120 km/sa", "yüz yirmi kilometre bölü saat")]
    [InlineData("16 GB", "on altı gigabayt")]
    public void Dotted_and_units(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Büyük harfli kısaltmalar.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("TBMM", "te be me me")]
    [InlineData("ABD", "a be de")]
    [InlineData("ABD'de", "a be de'de")]
    [InlineData("AB", "a be")]
    [InlineData("AB'ye", "a be'ye")]
    [InlineData("BM", "be me")]
    [InlineData("TRT", "te re te")]
    [InlineData("PTT", "pe te te")]
    [InlineData("TBMM'nin", "te be me me'nin")]
    [InlineData("NATO", "Nato")]
    [InlineData("NATO'ya", "Nato'ya")]
    [InlineData("TÜBİTAK", "Tübitak")]
    [InlineData("ÖSYM", "ö se ye me")]
    [InlineData("KKTC", "ke ke te ce")]
    [InlineData("DİKKAT", "Dikkat")]
    [InlineData("ĞX", "yumuşak ge iks")]
    public void Acronyms(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Bağlamsız belirsiz birimler açılmaz.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("m harfi", "m harfi")]
    [InlineData("ha gayret", "ha gayret")]
    [InlineData("sa ve as", "sa ve as")]
    public void Ambiguous_units_need_number(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);
}
