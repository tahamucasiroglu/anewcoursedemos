using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Kelime tekrar denetimi: yerel analiz, bulgu yoksa LLM çağrılmaması, yalnız ilgili sahnelerin yeniden yazılması, uzunluk koruması.
/// </summary>
public sealed class WordRepetitionTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    private static ConceptDocument Doc(params string[] narrations) => new()
    {
        Title = "Bizans İmparatoru",
        Scenes = narrations.Select((n, i) => new ConceptScene { Index = i, Narration = n }).ToList(),
    };

    private static readonly ConceptDocument Repetitive = Doc(
        "Muhteşem saray muhteşem bahçelerle çevriliydi.",
        "Bizans imparatoru muhteşem bir tören düzenledi.",
        "Halk bu muhteşem gösteriyi izledi ve muhteşem dedi.",
        "Sonra herkes evine döndü.");

    /// <summary>Kök bazında fazla tekrar bulunur; başlıktaki konu kelimeleri ve dolgu kelimeleri sayılmaz.</summary>
    [Fact]
    public void Analyzer_finds_overused_words()
    {
        var findings = _ctx.Analyzer.Analyze(Repetitive);
        var f = findings.Single();
        f.Word.ShouldBe("muhteşem");
        f.Count.ShouldBe(5);
        f.SceneIndexes.ShouldBe([0, 1, 2]);
    }

    /// <summary>Bulgu yoksa LLM çağrılmaz ve maliyet 0'dır.</summary>
    [Fact]
    public async Task No_findings_means_no_llm_call()
    {
        var context = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;
        var outcome = await _ctx.WordCheck.CheckAsync(context, Doc("Kısa ve temiz bir cümle.", "Başka bir sahne."), TestContext.Current.CancellationToken);
        outcome.Value.Findings.ShouldBeEmpty();
        outcome.Value.CostUsd.ShouldBe(0);
        _ctx.Gateway.Calls.ShouldBeEmpty();
    }

    /// <summary>Yalnız ilgili sahneler gönderilir; uygun uzunluktaki yanıt uygulanır, çok kısa olan ve istenmeyen sahne reddedilir.</summary>
    [Fact]
    public async Task Rewrites_only_affected_scenes_with_length_guard()
    {
        _ctx.Gateway.Enqueue(new NarrationRewriteLlmOutput
        {
            Scenes =
            [
                new NarrationRewriteLlm { Index = 0, Narration = "Görkemli saray zarif bahçelerle çevriliydi." },
                new NarrationRewriteLlm { Index = 2, Narration = "Kısa." },
                new NarrationRewriteLlm { Index = 3, Narration = "İstenmeyen sahne değişikliği burada." },
            ],
        }, 0.002m);
        var context = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;

        var outcome = (await _ctx.WordCheck.CheckAsync(context, Repetitive, TestContext.Current.CancellationToken)).Value;

        outcome.RewrittenScenes.ShouldBe(1);
        outcome.Document.Scenes[0].Narration.ShouldBe("Görkemli saray zarif bahçelerle çevriliydi.");
        outcome.Document.Scenes[2].Narration.ShouldBe(Repetitive.Scenes[2].Narration);
        outcome.Document.Scenes[3].Narration.ShouldBe(Repetitive.Scenes[3].Narration);
        outcome.CostUsd.ShouldBe(0.002m);
        var task = _ctx.Gateway.Calls.Single();
        task.Purpose.ShouldBe("word-check");
        task.UserPrompt.ShouldContain("[1] Bizans imparatoru");
        task.UserPrompt.ShouldNotContain("[3]");
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
