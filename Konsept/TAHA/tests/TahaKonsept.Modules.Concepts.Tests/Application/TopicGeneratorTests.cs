using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Concepts.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Konu üreticisi ve konu önerisi servisi: hafıza/iç tekrar eleme, numara temizliği, adet sınırı, hafızaya yazma.
/// </summary>
public sealed class TopicGeneratorTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    private static TopicListLlmOutput Topics(params string[] titles) =>
        new() { Topics = titles.Select(t => new TopicLlmItem { Title = t, Hook = "Kanca " + t }).ToList() };

    /// <summary>Hafızadaki ve kendi içindeki tekrarlar elenir, numaralar temizlenir, ilk N tanesi döner, hafıza listesi prompta girer.</summary>
    [Fact]
    public async Task Filters_known_and_duplicate_titles()
    {
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, _ctx.Profile.Id, ["Ayasofya'nın Kubbesi"], TopicMemoryKind.Deleted, TestContext.Current.CancellationToken);
        _ctx.Gateway.Enqueue(Topics("1. Ayasofya Kubbesi", "2) \"Kerkoporta Kapısı\"", "Kerkoporta kapısı", "- Rum Ateşi", "Malazgirt", "Fazla"));
        var context = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;
        var memory = await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken);

        var result = await _ctx.Topics.GenerateAsync(context, 3, null, null, memory, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Select(i => i.Title).ShouldBe(["Kerkoporta Kapısı", "Rum Ateşi", "Malazgirt"]);
        result.Value.FilteredOut.ShouldBe(2);
        var task = _ctx.Gateway.Calls.Single();
        task.ModelKey.ShouldBe("claude-haiku-5-5");
        task.UserPrompt.ShouldContain("✗ Ayasofya'nın Kubbesi");
        task.UserPrompt.ShouldContain("5 konu öner.");
    }

    /// <summary>Hepsi elenirse NoTopics.</summary>
    [Fact]
    public async Task All_filtered_returns_no_topics()
    {
        await _ctx.Memory.RememberTitlesAsync(_ctx.UserId, _ctx.Profile.Id, ["Rum Ateşi"], TopicMemoryKind.Generated, TestContext.Current.CancellationToken);
        _ctx.Gateway.Enqueue(Topics("Rum ateşi"));
        var context = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;
        var result = await _ctx.Topics.GenerateAsync(context, 1, null, null, await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        result.Error.ShouldBe(ConceptErrors.NoTopics);
    }

    /// <summary>Servis: önerilenler hafızaya Suggested yazılır; ikinci öneride aynısı gelmez; beğenilmeyen negatif olur.</summary>
    [Fact]
    public async Task Suggestion_service_records_memory()
    {
        var service = _ctx.CreateSuggestionService();
        _ctx.Gateway.Enqueue(Topics("Kerkoporta", "Rum Ateşi"));
        var first = await service.SuggestAsync(new() { Count = 2 }, TestContext.Current.CancellationToken);
        first.Value.Items.Count.ShouldBe(2);

        _ctx.Gateway.Enqueue(Topics("kerkoporta", "Yeni Konu"));
        var second = await service.SuggestAsync(new() { Count = 2 }, TestContext.Current.CancellationToken);
        second.Value.Items.Select(i => i.Title).ShouldBe(["Yeni Konu"]);
        second.Value.FilteredOut.ShouldBe(1);

        (await service.DislikeAsync(new() { Titles = ["Yeni Konu"] }, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var memory = await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken);
        memory.Items.Single(i => i.Title == "Yeni Konu").Kind.ShouldBe(TopicMemoryKind.Disliked);
        (await service.ResetMemoryAsync(false, TestContext.Current.CancellationToken)).Value.Removed.ShouldBe(3);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
