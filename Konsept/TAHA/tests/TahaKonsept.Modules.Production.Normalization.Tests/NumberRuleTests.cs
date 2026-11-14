using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Sayılar: tam/negatif/ondalık/binlik, yüzde, aralık, para birimi ve ekler.</summary>
public sealed class NumberRuleTests
{
    /// <summary>Sayı biçimleri.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("1453", "bin dört yüz elli üç")]
    [InlineData("0", "sıfır")]
    [InlineData("-5 derece", "eksi beş derece")]
    [InlineData("3,5", "üç virgül beş")]
    [InlineData("0,25", "sıfır virgül yirmi beş")]
    [InlineData("2.5", "iki nokta beş")]
    [InlineData("1.250.000 kişi", "bir milyon iki yüz elli bin kişi")]
    [InlineData("1.250.000.", "bir milyon iki yüz elli bin.")]
    [InlineData("%40", "yüzde kırk")]
    [InlineData("40%", "yüzde kırk")]
    [InlineData("%40'ı", "yüzde kırkı")]
    [InlineData("%3,5", "yüzde üç virgül beş")]
    [InlineData("1914-1918", "bin dokuz yüz on dört - bin dokuz yüz on sekiz")]
    [InlineData("1914–1918 yılları", "bin dokuz yüz on dört - bin dokuz yüz on sekiz yılları")]
    [InlineData("1914-1918'de", "bin dokuz yüz on dört - bin dokuz yüz on sekizde")]
    [InlineData("10$", "on dolar")]
    [InlineData("$10", "on dolar")]
    [InlineData("€5", "beş avro")]
    [InlineData("100₺", "yüz lira")]
    [InlineData("100 TL", "yüz lira")]
    [InlineData("100 TL'ye", "yüz liraya")]
    [InlineData("5 trilyon", "beş trilyon")]
    [InlineData("999999999999", "dokuz yüz doksan dokuz milyar dokuz yüz doksan dokuz milyon dokuz yüz doksan dokuz bin dokuz yüz doksan dokuz")]
    public void Formats(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Kesme ekleri (ünlü uyumu, benzeşme, yumuşama, sıra ekleri).</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("1453'te", "bin dört yüz elli üçte")]
    [InlineData("1923'te", "bin dokuz yüz yirmi üçte")]
    [InlineData("1923'de", "bin dokuz yüz yirmi üçte")]
    [InlineData("40'ta", "kırkta")]
    [InlineData("2'nci", "ikinci")]
    [InlineData("5'inci", "beşinci")]
    [InlineData("3'üncü", "üçüncü")]
    [InlineData("2'ncisi", "ikincisi")]
    [InlineData("100'ü", "yüzü")]
    [InlineData("6'da", "altıda")]
    [InlineData("7'de", "yedide")]
    [InlineData("9'u", "dokuzu")]
    [InlineData("10'dan", "ondan")]
    [InlineData("4'ü", "dördü")]
    [InlineData("4'e", "dörde")]
    [InlineData("1'e", "bire")]
    [InlineData("2'ye", "ikiye")]
    [InlineData("1990'larda", "bin dokuz yüz doksanlarda")]
    [InlineData("80'lerde", "seksenlerde")]
    [InlineData("2000'li yıllar", "iki binli yıllar")]
    [InlineData("1453’te", "bin dört yüz elli üçte")]
    public void Suffixes(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Sözcük içindeki rakamlara dokunulmaz.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("MP3 dosyası", "MP3 dosyası")]
    [InlineData("H2O", "H2O")]
    public void Digits_inside_words_are_kept(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);
}
