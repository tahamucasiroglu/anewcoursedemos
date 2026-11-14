using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Tarihler, dönemler (MÖ/MS) ve saat.</summary>
public sealed class DateRuleTests
{
    /// <summary>Tarih biçimleri.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("29 Mayıs 1453", "yirmi dokuz Mayıs bin dört yüz elli üç")]
    [InlineData("29 Mayıs 1453'te", "yirmi dokuz Mayıs bin dört yüz elli üçte")]
    [InlineData("29.05.1453", "yirmi dokuz Mayıs bin dört yüz elli üç")]
    [InlineData("29/05/1453", "yirmi dokuz Mayıs bin dört yüz elli üç")]
    [InlineData("29.10.1923'te", "yirmi dokuz Ekim bin dokuz yüz yirmi üçte")]
    [InlineData("1453 yılında", "bin dört yüz elli üç yılında")]
    [InlineData("Mayıs 1919", "Mayıs bin dokuz yüz on dokuz")]
    [InlineData("3 Mart'ta", "üç Mart'ta")]
    [InlineData("MÖ 330", "milattan önce üç yüz otuz")]
    [InlineData("M.Ö. 330", "milattan önce üç yüz otuz")]
    [InlineData("MS 1071'de", "milattan sonra bin yetmiş birde")]
    [InlineData("M.S. 476", "milattan sonra dört yüz yetmiş altı")]
    [InlineData("MÖ 4. yüzyıl", "milattan önce dördüncü yüzyıl")]
    [InlineData("Saat 14:30'da", "Saat on dört otuzda")]
    [InlineData("09:05", "dokuz sıfır beş")]
    [InlineData("MS Office", "me se Office")]
    public void Dates(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);
}
