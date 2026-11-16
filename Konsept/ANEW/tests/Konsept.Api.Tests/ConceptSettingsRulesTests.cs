using Konsept.Modules.Profiles.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>Konsept ayarı kuralları (spec 0005 Req 3–6, AC-2, AC-5): roller, ince ayar seçenek sayıları ve efor çözümü.</summary>
public sealed class ConceptSettingsRulesTests
{
    /// <summary>AC-5: ton 8, izleyici 4, anlatım biçimi 4 seçenek; null (Belirtilmedi) her grupta geçerli; liste dışı değer geçersiz.</summary>
    [Fact]
    public void FineTune_OptionCounts_AndValidity()
    {
        ConceptSettingsRules.Roles.ShouldBe(["concept", "dedupe", "topic"]);
        ConceptSettingsRules.Tones.Count.ShouldBe(8);
        ConceptSettingsRules.Audiences.Count.ShouldBe(4);
        ConceptSettingsRules.Narrations.Count.ShouldBe(4);
        ConceptSettingsRules.Tones.Distinct(StringComparer.Ordinal).Count().ShouldBe(8);

        ConceptSettingsRules.IsOption(ConceptSettingsRules.Tones, null).ShouldBeTrue();
        ConceptSettingsRules.IsOption(ConceptSettingsRules.Tones, "humorous").ShouldBeTrue();
        ConceptSettingsRules.IsOption(ConceptSettingsRules.Tones, "Humorous").ShouldBeFalse();
        ConceptSettingsRules.IsOption(ConceptSettingsRules.Audiences, "calm").ShouldBeFalse();
    }

    /// <summary>AC-2: boş efor → katalog varsayılanı; desteklenen düzey kabul; desteklenmeyen düzey red; efor desteklemeyen modelde efor yok.</summary>
    [Fact]
    public void Effort_DefaultSupportedAndUnsupported()
    {
        string[] levels = ["low", "medium", "high"];

        ConceptSettingsRules.TryResolveEffort(levels, "high", null, out var byDefault).ShouldBeTrue();
        byDefault.ShouldBe("high");
        ConceptSettingsRules.TryResolveEffort(levels, "high", " ", out var blank).ShouldBeTrue();
        blank.ShouldBe("high");
        ConceptSettingsRules.TryResolveEffort(levels, "high", "low", out var low).ShouldBeTrue();
        low.ShouldBe("low");
        ConceptSettingsRules.TryResolveEffort(levels, "high", "max", out _).ShouldBeFalse();
        ConceptSettingsRules.TryResolveEffort([], null, null, out var none).ShouldBeTrue();
        none.ShouldBeNull();
        ConceptSettingsRules.TryResolveEffort([], null, "high", out _).ShouldBeFalse();
    }
}
