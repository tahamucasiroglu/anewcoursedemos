using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Concrete;
using TahaKonsept.Modules.Production.Normalization.DependencyInjection;
using TahaKonsept.Modules.Production.Normalization.Factory;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Genel davranış: yazılış değişmez, değişiklik kaydı, dil seçimi, DI, determinizm, boş girdi.</summary>
public sealed class NormalizerBehaviourTests
{
    /// <summary>Yazılış aynen kalır, her değişiklik kural anahtarıyla kaydedilir.</summary>
    [Fact]
    public void Display_unchanged_and_changes_recorded()
    {
        const string text = "II. Mehmed 29 Mayıs 1453'te Google & TBMM ile 5 km yürüdü, 3. kez.";
        var result = TestNormalizer.Run(text);
        result.Display.ShouldBe(text);
        var rules = result.Changes.Select(c => c.Rule).ToHashSet();
        rules.ShouldContain(NormalizationRuleKeys.Roman);
        rules.ShouldContain(NormalizationRuleKeys.Date);
        rules.ShouldContain(NormalizationRuleKeys.Lexicon);
        rules.ShouldContain(NormalizationRuleKeys.Symbol);
        rules.ShouldContain(NormalizationRuleKeys.Abbreviation);
        rules.ShouldContain(NormalizationRuleKeys.Ordinal);
        result.Changes.ShouldContain(c => c.Original == "1453'te" || c.Original == "29 Mayıs 1453'te");
    }

    /// <summary>Sayı kuralı değişikliği kaydedilir.</summary>
    [Fact]
    public void Number_change_recorded()
    {
        var result = TestNormalizer.Run("%40");
        result.Changes.ShouldHaveSingleItem().ShouldBe(new NormalizationChange("%40", "yüzde kırk", NormalizationRuleKeys.Number));
    }

    /// <summary>Türkçe dışı dilde yalnız kullanıcı sözlüğü uygulanır.</summary>
    [Fact]
    public void Other_languages_are_lexicon_only()
    {
        var normalizer = SpeechTextNormalizerFactory.CreateDefault();
        var plain = normalizer.Normalize("Google bought 2 companies in 1998.", "en", []);
        plain.Spoken.ShouldBe("Google bought 2 companies in 1998.");
        plain.Changes.ShouldBeEmpty();

        var withLexicon = normalizer.Normalize("Taha's video", "en-US", [TestNormalizer.Entry("Taha", "Tahaa")]);
        withLexicon.Spoken.ShouldBe("Tahaa's video");
    }

    /// <summary>Dil seçici.</summary>
    [Fact]
    public void Selector_picks_by_language()
    {
        var selector = SpeechTextNormalizerFactory.CreateSelector();
        selector.Select("tr").Name.ShouldBe("turkish");
        selector.Select("tr-TR").Name.ShouldBe("turkish");
        selector.Select(string.Empty).Name.ShouldBe("turkish");
        selector.Select("en").Name.ShouldBe("lexicon-only");
        selector.Select("de-DE").Name.ShouldBe("lexicon-only");
    }

    /// <summary>Boş metin.</summary>
    [Fact]
    public void Empty_text_returns_empty()
    {
        var result = TestNormalizer.Run("   ");
        result.Spoken.ShouldBe("   ");
        result.Changes.ShouldBeEmpty();
    }

    /// <summary>Aynı girdi → aynı çıktı.</summary>
    [Fact]
    public void Deterministic()
    {
        const string text = "XV. yüzyılda 1.250.000 kişi %40 oranında NATO'ya katıldı.";
        TestNormalizer.Spoken(text).ShouldBe(TestNormalizer.Spoken(text));
    }

    /// <summary>tr-TR büyük/küçük harf: İ/i, I/ı.</summary>
    [Fact]
    public void Turkish_casing()
    {
        TestNormalizer.Spoken("İSTANBUL").ShouldBe("İstanbul");
        TestNormalizer.Spoken("IRMAK").ShouldBe("Irmak");
        TestNormalizer.Spoken("INSTAGRAM").ShouldBe("İnstagram");
    }

    /// <summary>DI kaydı çözülür ve çalışır.</summary>
    [Fact]
    public void Di_registration_resolves()
    {
        using var provider = new ServiceCollection().AddSpeechTextNormalization().AddSpeechTextNormalization().BuildServiceProvider();
        var normalizer = provider.GetRequiredService<ISpeechTextNormalizer>();
        normalizer.ShouldBeOfType<LanguageRoutingSpeechTextNormalizer>();
        normalizer.Normalize("1453", "tr", []).Spoken.ShouldBe("bin dört yüz elli üç");
        provider.GetRequiredService<INumberToTurkishWords>().ToWords(40).ShouldBe("kırk");
        provider.GetRequiredService<INormalizerSelector>().Select("en").ShouldBeOfType<LexiconOnlySpeechTextNormalizer>();
    }
}
