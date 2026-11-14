using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Concrete;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Application.Prompts;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Şablon işleyici, gömülü prompt kütüphanesi ve prompt fabrikası.
/// </summary>
public sealed class PromptRenderingTests
{
    private readonly PromptTemplateRenderer _renderer = new();

    /// <summary>Değişkenler yerleşir; dolu bölüm kalır, boş bölüm satırıyla silinir; bilinmeyen değişken boş olur.</summary>
    [Fact]
    public void Renderer_handles_variables_and_sections()
    {
        const string template = "A: {{a}}\n{{#b}}B var: {{b}}{{/b}}\n{{#c}}C var{{/c}}\nSon {{x}}.";
        var text = _renderer.Render(template, new Dictionary<string, string?> { ["a"] = "1", ["b"] = "2", ["c"] = " " });
        text.ShouldBe("A: 1\nB var: 2\nSon .");
    }

    /// <summary>Satır içi bölüm ve çok satırlı bölüm; fazla boş satırlar sıkıştırılır.</summary>
    [Fact]
    public void Renderer_inline_and_multiline_sections()
    {
        const string template = "Kural.{{#f}} Kaynak yaz.{{/f}}\n\n\n\n{{#m}}\nListe:\n{{m}}\n{{/m}}\nBitti";
        _renderer.Render(template, new Dictionary<string, string?> { ["f"] = "1", ["m"] = "• x" })
            .ShouldBe("Kural. Kaynak yaz.\n\nListe:\n• x\n\nBitti");
        _renderer.Render(template, new Dictionary<string, string?>()).ShouldBe("Kural.\n\nBitti");
    }

    /// <summary>Tüm gömülü şablonlar yüklenir.</summary>
    [Fact]
    public void All_embedded_prompts_load()
    {
        var library = new EmbeddedPromptLibrary();
        foreach (var name in PromptNames.All)
        {
            library.Get(name).Length.ShouldBeGreaterThan(20, name);
        }
    }

    /// <summary>Konsept promptu: süre/kelime/sahne hedefleri, dil, kadraj, gerçek bilgi bölümü; artık değişken kalmaz.</summary>
    [Fact]
    public void Concept_prompt_contains_targets_and_fact_section()
    {
        var factory = new ConceptPromptFactory(new EmbeddedPromptLibrary(), _renderer);
        var context = new ConceptGenerationContext
        {
            ProfileBlock = "Kanal: Bizans Günlükleri",
            Preferences = ConceptPreferences.Default with { Tone = "Dramatik", Notes = "Argo yok" },
            TargetDurationSec = 60,
            FactGrounding = true,
            Format = VideoFormat.Vertical,
            WordsPerSecond = 2.4,
        };
        var pair = factory.Concept(context, new ConceptBrief { Title = "Kerkoporta", Hook = "Tek kapı açıktı." });

        pair.System.ShouldContain("Kanal: Bizans Günlükleri");
        pair.System.ShouldContain("≈60 sn");
        pair.System.ShouldContain("≈144 kelime");
        pair.System.ShouldContain("2.4 kelime/sn");
        pair.System.ShouldContain("9:16");
        pair.System.ShouldContain("Seslendirme Türkçe");
        pair.System.ShouldContain("Ton: Dramatik");
        pair.System.ShouldContain("Argo yok");
        pair.System.ShouldContain("web aramasıyla doğrula");
        pair.System.ShouldNotContain("{{");
        pair.User.ShouldBe("Konu: Kerkoporta\nÖnerilen kanca: Tek kapı açıktı.\nKonsepti yaz.");

        var noFact = factory.Concept(context with { FactGrounding = false }, new ConceptBrief { Idea = "Fikir", AngleNumber = 2 });
        noFact.System.ShouldNotContain("web aramasıyla");
        noFact.User.ShouldContain("2. konsept");
        noFact.System.ShouldBe(factory.Revise(context with { FactGrounding = false }, "{}", "kısalt").System);
    }

    /// <summary>Konu promptu hafıza listesini ve adedi içerir; hafıza boşsa başlık yazılmaz.</summary>
    [Fact]
    public void Topics_prompt_includes_memory_only_when_present()
    {
        var factory = new ConceptPromptFactory(new EmbeddedPromptLibrary(), _renderer);
        var context = new ConceptGenerationContext { ProfileBlock = "Kanal: X" };
        var with = factory.Topics(context, "• Eski konu\n✗ Kötü konu", "son yüzyıl", null, 12);
        with.User.ShouldContain("Daha önce işlenenler");
        with.User.ShouldContain("✗ Kötü konu");
        with.User.ShouldContain("Yön: son yüzyıl");
        with.User.ShouldEndWith("12 konu öner.");
        var without = factory.Topics(context, string.Empty, null, null, 5);
        without.User.ShouldNotContain("Daha önce");
        without.User.ShouldNotContain("{{");
    }
}
