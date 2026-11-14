using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Concepts.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Konsept üreticisi (sahte LLM geçidi): LLM çıktısının belgeye dönüşümü ve gönderilen işin alanları.
/// </summary>
public sealed class ConceptGeneratorTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    private async Task<ConceptGenerationContext> ContextAsync() => (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null)).Value;

    /// <summary>Boş sahne atılır, sıra yeniden numaralanır, geçersiz URL elenir, etiket/okunuş temizlenir, eksik süre kelimeden hesaplanır.</summary>
    [Fact]
    public async Task Builds_clean_document()
    {
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput(), 0.07m);
        var result = await _ctx.Generator.GenerateAsync(await ContextAsync(), new ConceptBrief { Title = "Kerkoporta" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var doc = result.Value.Document;
        doc.Scenes.Count.ShouldBe(3);
        doc.Scenes.Select(s => s.Index).ShouldBe([0, 1, 2]);
        doc.Scenes[1].DurationHintSec.ShouldBe(Math.Round(10 / 2.4, 1));
        doc.Sources.Count.ShouldBe(1);
        doc.Tags.ShouldBe(["bizans", "tarih"]);
        doc.Pronunciations.Select(p => p.Written).ShouldBe(["Constantine"]);
        doc.Language.ShouldBe("tr");
        doc.Format.ShouldBe(VideoFormat.Vertical);
        doc.TargetDurationSec.ShouldBe(60);
        doc.Hook.ShouldStartWith("29 Mayıs");
        result.Value.CostUsd.ShouldBe(0.07m);
        result.Value.InputTokens.ShouldBe(100);
    }

    /// <summary>İş: konsept modeli, efor, önbellekli sistem promptu, tarih profilinde web araması açık, amaç "concept".</summary>
    [Fact]
    public async Task Sends_expected_task()
    {
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput());
        await _ctx.Generator.GenerateAsync(await ContextAsync(), new ConceptBrief { Title = "Kerkoporta" }, TestContext.Current.CancellationToken);

        var task = _ctx.Gateway.Calls.Single();
        task.ModelKey.ShouldBe("claude-sonnet-5-5");
        task.UseWebSearch.ShouldBeTrue();
        task.CacheSystemPrompt.ShouldBeTrue();
        task.Purpose.ShouldBe("concept");
        task.MaxOutputTokens.ShouldBeGreaterThanOrEqualTo(8000);
        task.SystemPrompt.ShouldContain("Bizans Günlükleri");
        task.UserId.ShouldBe(_ctx.UserId);
    }

    /// <summary>Sahnesiz çıktı geçersiz sayılır; geçit hatası aynen döner.</summary>
    [Fact]
    public async Task Invalid_output_and_gateway_error()
    {
        _ctx.Gateway.Enqueue(new ConceptLlmOutput { Title = "Boş" });
        (await _ctx.Generator.GenerateAsync(await ContextAsync(), new ConceptBrief { Title = "x" }, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.InvalidModelOutput);

        var error = Error.Failure("providers.key_missing", "Anahtar yok");
        _ctx.Gateway.EnqueueError(error);
        (await _ctx.Generator.GenerateAsync(await ContextAsync(), new ConceptBrief { Title = "x" }, TestContext.Current.CancellationToken)).Error.ShouldBe(error);
    }

    /// <summary>Düzeltme: mevcut belge prompta kaçışsız JSON olarak gider; kanal alanları şablon belgeden korunur.</summary>
    [Fact]
    public async Task Revise_keeps_channel_fields_and_sends_document()
    {
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("Düzeltilmiş"));
        var template = new ConceptDocument { Title = "Eski başlık ğüşı", Language = "en", Format = VideoFormat.Horizontal, TargetDurationSec = 90, Scenes = [new ConceptScene { Narration = "x" }] };
        var result = await _ctx.Generator.ReviseAsync(await ContextAsync(), template, " daha kısa ", TestContext.Current.CancellationToken);

        result.Value.Document.Language.ShouldBe("en");
        result.Value.Document.Format.ShouldBe(VideoFormat.Horizontal);
        result.Value.Document.TargetDurationSec.ShouldBe(90);
        var task = _ctx.Gateway.Calls.Single();
        task.Purpose.ShouldBe("concept-revise");
        task.UserPrompt.ShouldContain("Eski başlık ğüşı");
        task.UserPrompt.ShouldContain("Düzeltme talimatı: daha kısa");
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
