using Shouldly;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Concepts.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Konu hafızası: tekrar eleme, tür güçlendirme, prompt listesi tavanı ve sırası, sıfırlama, konsept bağı.
/// </summary>
public sealed class TopicMemoryTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    /// <summary>Aynı konu iki kez yazılmaz; tür güçlenir; IsKnown yazım farkını yakalar.</summary>
    [Fact]
    public async Task Remember_dedupes_and_promotes()
    {
        var pid = _ctx.Profile.Id;
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Bizans'ın Son İmparatoru", "bizansın son imparatoru", "Ayasofya"], TopicMemoryKind.Suggested, TestContext.Current.CancellationToken);
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Bizans Son İmparatoru"], TopicMemoryKind.Disliked, TestContext.Current.CancellationToken);

        var snapshot = await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken);
        snapshot.Items.Count.ShouldBe(2);
        snapshot.Items.Single(i => i.Title.StartsWith("Bizans", StringComparison.Ordinal)).Kind.ShouldBe(TopicMemoryKind.Disliked);
        _ctx.Memory.IsKnown(snapshot, _ctx.Normalizer.Fingerprint("AYASOFYA")).ShouldBeTrue();
        _ctx.Memory.IsKnown(snapshot, _ctx.Normalizer.Fingerprint("Malazgirt")).ShouldBeFalse();
    }

    /// <summary>Prompt listesi en çok 120 satır ve 3500 karakterle sınırlı, negatifler ✗ ile işaretli.</summary>
    [Fact]
    public async Task Render_for_prompt_is_capped_and_marks_negatives()
    {
        var pid = _ctx.Profile.Id;
        var titles = Enumerable.Range(1, 300).Select(_ => $"{Guid.NewGuid():N} {Guid.NewGuid():N}").ToList();
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, titles, TopicMemoryKind.Suggested, TestContext.Current.CancellationToken);
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Silinen özel konu Zeta"], TopicMemoryKind.Deleted, TestContext.Current.CancellationToken);

        var snapshot = await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken);
        snapshot.Items.Count.ShouldBe(301);
        var block = _ctx.Memory.RenderForPrompt(snapshot);
        var lines = block.Split('\n');
        lines.Length.ShouldBeLessThanOrEqualTo(120);
        block.Length.ShouldBeLessThanOrEqualTo(3500);
        lines[0].ShouldBe("✗ Silinen özel konu Zeta");
        lines.Skip(1).ShouldAllBe(l => l.StartsWith("• ", StringComparison.Ordinal));
    }

    /// <summary>Boş hafıza boş liste verir.</summary>
    [Fact]
    public async Task Empty_memory_renders_empty()
    {
        var snapshot = await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken);
        _ctx.Memory.RenderForPrompt(snapshot).ShouldBe(string.Empty);
    }

    /// <summary>Varsayılan sıfırlama yalnız öneri/beğenilmeyenleri siler; all=true hepsini.</summary>
    [Fact]
    public async Task Reset_default_keeps_generated_and_deleted()
    {
        var pid = _ctx.Profile.Id;
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Ayasofya"], TopicMemoryKind.Suggested, TestContext.Current.CancellationToken);
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Kerkoporta"], TopicMemoryKind.Disliked, TestContext.Current.CancellationToken);
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Malazgirt"], TopicMemoryKind.Generated, TestContext.Current.CancellationToken);
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, pid, ["Preveze"], TopicMemoryKind.Deleted, TestContext.Current.CancellationToken);

        (await _ctx.Memory.ResetAsync(_ctx.UserId, false, TestContext.Current.CancellationToken)).ShouldBe(2);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken)).Items.Count.ShouldBe(2);
        (await _ctx.Memory.ResetAsync(_ctx.UserId, true, TestContext.Current.CancellationToken)).ShouldBe(2);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
    }

    /// <summary>Konsept bağı: silme Deleted yapar, geri alma (force) Generated'a indirir, unutma kaydı siler.</summary>
    [Fact]
    public async Task Mark_and_forget_concept()
    {
        var pid = _ctx.Profile.Id;
        var conceptId = Guid.NewGuid();
        await _ctx.Memory.MarkConceptAsync(_ctx.UserId, pid, conceptId, "Kerkoporta", TopicMemoryKind.Generated, false, TestContext.Current.CancellationToken);
        await _ctx.Memory.MarkConceptAsync(_ctx.UserId, pid, conceptId, "Kerkoporta", TopicMemoryKind.Deleted, false, TestContext.Current.CancellationToken);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken)).Items.Single().Kind.ShouldBe(TopicMemoryKind.Deleted);

        await _ctx.Memory.MarkConceptAsync(_ctx.UserId, pid, conceptId, "Kerkoporta", TopicMemoryKind.Generated, forceKind: true, TestContext.Current.CancellationToken);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken)).Items.Single().Kind.ShouldBe(TopicMemoryKind.Generated);

        await _ctx.Memory.ForgetConceptAsync(_ctx.UserId, conceptId, TestContext.Current.CancellationToken);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, pid, TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
