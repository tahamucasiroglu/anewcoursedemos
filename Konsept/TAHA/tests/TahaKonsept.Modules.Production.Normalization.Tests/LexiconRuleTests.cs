using Shouldly;
using TahaKonsept.Modules.Production.Normalization.Concrete;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Production.Normalization.Tests;

/// <summary>Okunuş sözlüğü: yerleşik girdiler, büyük/küçük harf, kesme ekleri, öncelik, bütün sözcük.</summary>
public sealed class LexiconRuleTests
{
    /// <summary>Yerleşik sözlük.</summary>
    /// <param name="input">Yazılış.</param>
    /// <param name="expected">Okunuş.</param>
    [Theory]
    [InlineData("Google", "Gugıl")]
    [InlineData("google", "Gugıl")]
    [InlineData("GOOGLE", "Gugıl")]
    [InlineData("Google'ın", "Gugıl'ın")]
    [InlineData("Google’da", "Gugıl'da")]
    [InlineData("YouTube'a video yükledi", "Yutub'a video yükledi")]
    [InlineData("Facebook", "Feysbuk")]
    [InlineData("iPhone", "Ayfon")]
    [InlineData("Apple'ın yeni ürünü", "Epıl'ın yeni ürünü")]
    [InlineData("Microsoft", "Maykrosoft")]
    [InlineData("NVIDIA'nın", "Enviıdiya'nın")]
    [InlineData("Netflix'te", "Netfliks'te")]
    [InlineData("Instagram", "İnstagram")]
    [InlineData("WhatsApp", "Vatsap")]
    [InlineData("Twitter", "Tivitır")]
    [InlineData("ChatGPT", "Çet Ci Pi Ti")]
    [InlineData("OpenAI", "Open Ey Ay")]
    [InlineData("SpaceX", "Speys Eks")]
    [InlineData("Wi-Fi", "Vay Fay")]
    [InlineData("Bluetooth", "Bılutut")]
    [InlineData("Windows", "Vindovs")]
    [InlineData("Shakespeare'in", "Şekspir'in")]
    [InlineData("Napoleon", "Napolyon")]
    [InlineData("Washington'a", "Vaşington'a")]
    [InlineData("New York'ta", "Nüyork'ta")]
    [InlineData("Leonardo da Vinci", "Leonardo da Vinçi")]
    public void Built_in_entries(string input, string expected) => TestNormalizer.Spoken(input).ShouldBe(expected);

    /// <summary>Kelime içinde eşleşme yapılmaz.</summary>
    [Fact]
    public void Whole_word_only() => TestNormalizer.Spoken("Googleplex ve MyApple").ShouldBe("Googleplex ve MyApple");

    /// <summary>Kullanıcı girdisi yerleşik girdiden önce gelir.</summary>
    [Fact]
    public void User_entry_wins_over_built_in()
    {
        var result = TestNormalizer.Run("Google'ı aç", TestNormalizer.Entry("Google", "Guugıl"));
        result.Spoken.ShouldBe("Guugıl'ı aç");
        result.Changes.ShouldContain(c => c.Rule == NormalizationRuleKeys.Lexicon && c.Original == "Google'ı");
    }

    /// <summary>Kullanıcı listesinde önce gelen kazanır ve uzun eşleşme önceliklidir.</summary>
    [Fact]
    public void First_entry_wins_and_longest_match()
    {
        TestNormalizer.Run("Taha", TestNormalizer.Entry("Taha", "Ta-ha"), TestNormalizer.Entry("taha", "X")).Spoken.ShouldBe("Ta-ha");
        TestNormalizer.Run("Kara Kule", TestNormalizer.Entry("Kara", "K"), TestNormalizer.Entry("Kara Kule", "Karakule")).Spoken.ShouldBe("Karakule");
    }

    /// <summary>Yerleşik sözlük ≥ 80 girdi içerir.</summary>
    [Fact]
    public void Built_in_lexicon_has_at_least_80_entries() => new EmbeddedBuiltInLexiconProvider().Entries.Count.ShouldBeGreaterThanOrEqualTo(80);
}
