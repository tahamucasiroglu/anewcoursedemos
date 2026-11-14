using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Concrete;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Modules.Concepts.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Gerçek bilgi prompt seçimi: web araması olan model → kaynak zorunlu bölüm; olmayan model (yerel LLM / katalog bayrağı) →
/// kaynak istemeyen, uydurmayı yasaklayan, şüphelileri <c>reviewNotes</c>'a yazdıran bölüm; gerçek bilgi kapalı → ikisi de yok.
/// Bağlam kurucu yeteneği katalogdan okur; üretici web aramasını yalnız yetenekli modelde ister; kontrol notları belgeye geçer.
/// </summary>
public sealed class FactGroundingPromptTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();
    private readonly ConceptPromptFactory _factory = new(new EmbeddedPromptLibrary(), new PromptTemplateRenderer());

    private static ConceptGenerationContext Context(bool fact, bool web) => new()
    {
        ProfileBlock = "Kanal: Bizans Günlükleri",
        TargetDurationSec = 60,
        FactGrounding = fact,
        WebSearchAvailable = web,
    };

    /// <summary>Web araması olan modelde kaynak istenir, çevrimdışı talimat yoktur.</summary>
    [Fact]
    public void Web_capable_model_demands_sources()
    {
        var system = _factory.Concept(Context(true, true), new ConceptBrief { Title = "Kerkoporta" }).System;

        system.ShouldContain("web aramasıyla doğrula");
        system.ShouldContain("en az 2 gerçek kaynağı");
        system.ShouldNotContain("Web araması yapamıyorsun");
    }

    /// <summary>Web araması olmayan modelde kaynak istenmez; yerleşik bilgi, boş kaynak ve kontrol notu talimatı verilir.</summary>
    [Fact]
    public void Offline_model_does_not_demand_sources()
    {
        var system = _factory.Concept(Context(true, false), new ConceptBrief { Title = "Kerkoporta" }).System;

        system.ShouldNotContain("web aramasıyla doğrula");
        system.ShouldNotContain("en az 2 gerçek kaynağı");
        system.ShouldContain("Web araması yapamıyorsun");
        system.ShouldContain("yaygın kabul görmüş");
        system.ShouldContain("sources alanını boş bırak");
        system.ShouldContain("reviewNotes");
    }

    /// <summary>Gerçek bilgi kapalıyken iki bölüm de yoktur.</summary>
    [Fact]
    public void Fact_off_has_neither_section()
    {
        var system = _factory.Concept(Context(false, false), new ConceptBrief { Title = "Kerkoporta" }).System;

        system.ShouldNotContain("web aramasıyla doğrula");
        system.ShouldNotContain("Web araması yapamıyorsun");
    }

    /// <summary>Bağlam: yerel LLM web araması yapamaz; katalogda web araması olan model yapar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Context_reads_capability_from_catalog()
    {
        await _ctx.SettingsRepository.AddAsync(
            TahaKonsept.Modules.Concepts.Domain.Entities.ConceptSettings.Create(_ctx.UserId, ConceptPreferences.Default with { ConceptModelKey = "local-openai:qwen3:8b", FactGrounding = true }),
            TestContext.Current.CancellationToken);
        await _ctx.SettingsRepository.UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        var local = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;
        local.WebSearchAvailable.ShouldBeFalse();
        local.FactGrounding.ShouldBeTrue();
        _ctx.Contexts.CanSearchWeb("claude-sonnet-5-5").ShouldBeTrue();
        _ctx.Contexts.CanSearchWeb(null).ShouldBeFalse();
    }

    /// <summary>Üretici: web araması yetenekli değilse istenmez; kontrol notları temizlenip belgeye geçer.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Generator_skips_web_search_and_keeps_review_notes()
    {
        var output = ConceptsTestContext.SampleOutput() with { Sources = [], ReviewNotes = ["  Sahne 2: 53 gün süresi kontrol edilmeli  ", "", "sahne 2: 53 gün süresi kontrol edilmeli"] };
        _ctx.Gateway.Enqueue(output, 0m);
        var context = Context(true, false) with { UserId = _ctx.UserId };

        var result = await _ctx.Generator.GenerateAsync(context, new ConceptBrief { Title = "Kerkoporta" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _ctx.Gateway.Calls.Last().UseWebSearch.ShouldBeFalse();
        result.Value.Document.ReviewNotes.ShouldBe(["Sahne 2: 53 gün süresi kontrol edilmeli"]);
        result.Value.Document.Sources.ShouldBeEmpty();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
