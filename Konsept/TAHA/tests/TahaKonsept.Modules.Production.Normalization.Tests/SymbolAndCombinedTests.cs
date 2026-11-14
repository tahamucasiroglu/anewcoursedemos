using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Semboller ve birden çok kuralın birlikte çalıştığı cümleler.</summary>
public sealed class SymbolAndCombinedTests
{
    /// <summary>Semboller.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("Ali & Veli", "Ali ve Veli")]
    [InlineData("Ar&Ge", "Ar ve Ge")]
    [InlineData("3+4", "üç artı dört")]
    [InlineData("3 + 4 = 7", "üç artı dört eşittir yedi")]
    [InlineData("6/2", "altı bölü iki")]
    [InlineData("C++ dili", "Si Plas Plas dili")]
    [InlineData("A+B", "A+B")]
    public void Symbols(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Birleşik cümleler.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData(
        "II. Mehmed 29 Mayıs 1453'te İstanbul'u fethetti.",
        "İkinci Mehmed yirmi dokuz Mayıs bin dört yüz elli üçte İstanbul'u fethetti.")]
    [InlineData(
        "Google 1998'de kuruldu ve %90 pazar payına ulaştı.",
        "Gugıl bin dokuz yüz doksan sekizde kuruldu ve yüzde doksan pazar payına ulaştı.")]
    [InlineData(
        "TBMM 23 Nisan 1920'de açıldı.",
        "te be me me yirmi üç Nisan bin dokuz yüz yirmide açıldı.")]
    [InlineData(
        "I. Dünya Savaşı 1914-1918 yılları arasında sürdü.",
        "Birinci Dünya Savaşı bin dokuz yüz on dört - bin dokuz yüz on sekiz yılları arasında sürdü.")]
    [InlineData(
        "Büyük İskender MÖ 323'te öldü.",
        "Büyük İskender milattan önce üç yüz yirmi üçte öldü.")]
    [InlineData(
        "NVIDIA'nın hisseleri 3,5 kat arttı.",
        "Enviıdiya'nın hisseleri üç virgül beş kat arttı.")]
    [InlineData(
        "XV. yy.'da Osmanlı 2 kıtaya yayıldı.",
        "on beşinci yüzyılda Osmanlı iki kıtaya yayıldı.")]
    [InlineData(
        "ABD'de iPhone 10$ ucuzladı.",
        "a be de'de Ayfon on dolar ucuzladı.")]
    [InlineData(
        "Dr. Ayşe 21. yüzyılın 3. on yılında 5 km koştu.",
        "doktor Ayşe yirmi birinci yüzyılın üçüncü on yılında beş kilometre koştu.")]
    [InlineData(
        "Netflix'in 2024'teki geliri 39 milyar $ oldu.",
        "Netfliks'in iki bin yirmi dörtteki geliri otuz dokuz milyar dolar oldu.")]
    [InlineData(
        "Yaklaşık 1453 yılında 2'si de İstanbul'daydı.",
        "Yaklaşık bin dört yüz elli üç yılında ikisi de İstanbul'daydı.")]
    [InlineData(
        "COVID-19 salgını 2020'de başladı.",
        "Kovid-on dokuz salgını iki bin yirmide başladı.")]
    [InlineData(
        "Tesla'nın Wi-Fi'ı ve Bluetooth'u 7/24 açık.",
        "Tesla'nın Vay Fay'ı ve Bılutut'u yedi bölü yirmi dört açık.")]
    public void Combined(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);
}
