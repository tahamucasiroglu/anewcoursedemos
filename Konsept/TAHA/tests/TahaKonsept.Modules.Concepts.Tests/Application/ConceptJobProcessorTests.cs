using Microsoft.EntityFrameworkCore;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Concepts.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// İş çalıştırıcısı (sahte LLM): başlık işi, fikirden çok açılı iş, ölümcül hatada durma, iptal.
/// </summary>
public sealed class ConceptJobProcessorTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    private ConceptJob Register(ConceptJobKind kind, IReadOnlyList<string> titles, string? idea, int count)
    {
        var job = new ConceptJob { UserId = _ctx.UserId, ProfileId = _ctx.Profile.Id, Kind = kind, Titles = titles, Idea = idea, Count = count, CreatedAtUtc = _ctx.Clock.UtcNow };
        _ctx.Tracker.Register(job);
        return job;
    }

    /// <summary>Başlık işi: her başlık için konsept kaydedilir, iş Completed, maliyet toplanır, hafıza Generated olur.</summary>
    [Fact]
    public async Task Topics_job_produces_concepts()
    {
        var job = Register(ConceptJobKind.Topics, ["Kerkoporta", "Rum Ateşi"], null, 2);
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("Kerkoporta'nın sırrı"), 0.05m);
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("Rum Ateşi"), 0.05m);

        await _ctx.CreateProcessor().ProcessAsync(job, CancellationToken.None);

        var state = _ctx.Tracker.GetCurrent(_ctx.UserId);
        state.Status.ShouldBe(ConceptJobStatus.Completed);
        state.Completed.ShouldBe(2);
        state.Total.ShouldBe(2);
        state.CostUsd.ShouldBe(0.10m);
        state.ProducedIds.Count.ShouldBe(2);
        var concepts = await _ctx.Db.Concepts.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        concepts.Count.ShouldBe(2);
        concepts.ShouldAllBe(c => c.Status == ConceptStatus.Generated && c.Origin == ConceptOrigin.Topic && c.ProfileId == _ctx.Profile.Id);
        var memory = await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken);
        memory.Items.ShouldAllBe(i => i.Kind == TopicMemoryKind.Generated);
        memory.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    /// <summary>Fikir × 3: önce açılar (konu modeli), sonra 3 konsept; kaynak Idea, açı numarası prompta girer.</summary>
    [Fact]
    public async Task Idea_job_plans_angles_then_generates()
    {
        var job = Register(ConceptJobKind.Idea, [], "Şahi toplarının dökümü", 3);
        _ctx.Gateway.Enqueue(new TopicListLlmOutput { Topics = [new() { Title = "Dökümhane" }, new() { Title = "Macar Urban" }, new() { Title = "Surlara ilk atış" }] }, 0.001m);
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("A"));
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("B"));
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("C"));

        await _ctx.CreateProcessor().ProcessAsync(job, CancellationToken.None);

        _ctx.Gateway.Calls[0].Purpose.ShouldBe("topics");
        _ctx.Gateway.Calls[0].UserPrompt.ShouldContain("Şahi toplarının dökümü");
        _ctx.Gateway.Calls[2].UserPrompt.ShouldContain("Konu: Macar Urban");
        _ctx.Gateway.Calls[2].UserPrompt.ShouldContain("2. konsept");
        (await _ctx.Db.Concepts.CountAsync(c => c.Origin == ConceptOrigin.Idea, TestContext.Current.CancellationToken)).ShouldBe(3);
        _ctx.Tracker.GetCurrent(_ctx.UserId).Status.ShouldBe(ConceptJobStatus.Completed);
    }

    /// <summary>Anahtar eksik (ölümcül): iş durur, kalanlar denenmez, Failed + mesaj.</summary>
    [Fact]
    public async Task Fatal_error_stops_job()
    {
        var job = Register(ConceptJobKind.Topics, ["A", "B", "C"], null, 3);
        _ctx.Gateway.EnqueueError(Error.Validation("providers.key_missing", "Anthropic anahtarı girilmemiş."));

        await _ctx.CreateProcessor().ProcessAsync(job, CancellationToken.None);

        _ctx.Gateway.Calls.Count.ShouldBe(1);
        var state = _ctx.Tracker.GetCurrent(_ctx.UserId);
        state.Status.ShouldBe(ConceptJobStatus.Failed);
        state.Failed.ShouldBe(1);
        state.Message.ShouldBe("Anthropic anahtarı girilmemiş.");
    }

    /// <summary>Geçici hata öğeyi atlar, iş devam eder ve Completed olur (başarısız sayısı mesajda).</summary>
    [Fact]
    public async Task Transient_error_skips_item()
    {
        var job = Register(ConceptJobKind.Topics, ["A", "B"], null, 2);
        _ctx.Gateway.EnqueueError(Error.Failure("providers.transient", "Geçici"));
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("B"));

        await _ctx.CreateProcessor().ProcessAsync(job, CancellationToken.None);

        var state = _ctx.Tracker.GetCurrent(_ctx.UserId);
        state.Status.ShouldBe(ConceptJobStatus.Completed);
        state.Completed.ShouldBe(1);
        state.Failed.ShouldBe(1);
        state.Message!.ShouldContain("1 konsept üretilemedi");
    }

    /// <summary>İptal edilmiş belirteçle iş Cancelled biter.</summary>
    [Fact]
    public async Task Cancelled_job()
    {
        var job = Register(ConceptJobKind.Topics, ["A"], null, 1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await _ctx.CreateProcessor().ProcessAsync(job, cts.Token);
        _ctx.Tracker.GetCurrent(_ctx.UserId).Status.ShouldBe(ConceptJobStatus.Cancelled);
    }

    /// <summary>Kelime denetimi açıkken üretimden sonra denetim çalışır (tekrarlı anlatımda ikinci LLM çağrısı).</summary>
    [Fact]
    public async Task Word_check_runs_when_enabled()
    {
        await _ctx.CreateSettingsService().SaveAsync(new ConceptSettingsDto { WordRepetitionCheck = true }, TestContext.Current.CancellationToken);
        var job = Register(ConceptJobKind.Topics, ["Saray"], null, 1);
        var output = ConceptsTestContext.SampleOutput("Saray") with
        {
            Scenes =
            [
                new ConceptSceneLlm { Title = "1", Narration = "Muhteşem saray muhteşem bahçeler muhteşem kapılar." },
                new ConceptSceneLlm { Title = "2", Narration = "Muhteşem tören, muhteşem halk." },
            ],
        };
        _ctx.Gateway.Enqueue(output, 0.05m);
        _ctx.Gateway.Enqueue(new NarrationRewriteLlmOutput { Scenes = [new NarrationRewriteLlm { Index = 0, Narration = "Görkemli saray zarif bahçeler büyük kapılar." }] }, 0.01m);

        await _ctx.CreateProcessor().ProcessAsync(job, CancellationToken.None);

        _ctx.Gateway.Calls.Count.ShouldBe(2);
        var concept = await _ctx.Db.Concepts.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        concept.Document.Scenes[0].Narration.ShouldStartWith("Görkemli");
        concept.CostUsd.ShouldBe(0.06m);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
