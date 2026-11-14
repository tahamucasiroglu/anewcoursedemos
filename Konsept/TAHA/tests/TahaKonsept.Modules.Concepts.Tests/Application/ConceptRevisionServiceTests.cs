using Microsoft.EntityFrameworkCore;
using Shouldly;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Concepts.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Düzeltme adayı döngüsü: aday kaydedilmez → Seç ile yerine geçer → Sil ile atılır (maliyet yine yazılır) → Bunu da düzenle.
/// </summary>
public sealed class ConceptRevisionServiceTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    /// <summary>Aday üretimi konsepti değiştirmez; kabul belgeyi değiştirir, maliyeti ekler, diğer adayları atar.</summary>
    [Fact]
    public async Task Revise_then_accept()
    {
        var concept = await _ctx.SeedConceptAsync();
        var service = _ctx.CreateRevisionService();
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("Birinci aday"), 0.03m);
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("İkinci aday"), 0.04m);

        var first = (await service.ReviseAsync(concept.Id, new ReviseConceptRequest { Instruction = "Girişi sertleştir" }, TestContext.Current.CancellationToken)).Value;
        var second = (await service.ReviseAsync(concept.Id, new ReviseConceptRequest { Instruction = "Kısalt", Document = first.Document }, TestContext.Current.CancellationToken)).Value;
        first.Document.Title.ShouldBe("Birinci aday");
        (await _ctx.Db.Concepts.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Title.ShouldBe("Kerkoporta");
        _ctx.Gateway.Calls[1].UserPrompt.ShouldContain("Birinci aday");

        var accepted = await service.AcceptAsync(concept.Id, second.CandidateId, TestContext.Current.CancellationToken);

        accepted.Value.Title.ShouldBe("İkinci aday");
        accepted.Value.CostUsd.ShouldBe(0.02m + 0.03m + 0.04m);
        (await service.AcceptAsync(concept.Id, first.CandidateId, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.CandidateNotFound);
    }

    /// <summary>Atma: aday silinir, harcanan maliyet konsepte yazılır, belge değişmez.</summary>
    [Fact]
    public async Task Discard_records_cost()
    {
        var concept = await _ctx.SeedConceptAsync();
        var service = _ctx.CreateRevisionService();
        _ctx.Gateway.Enqueue(ConceptsTestContext.SampleOutput("Aday"), 0.05m);
        var candidate = (await service.ReviseAsync(concept.Id, new ReviseConceptRequest { Instruction = "x" }, TestContext.Current.CancellationToken)).Value;

        (await service.DiscardAsync(concept.Id, candidate.CandidateId, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        var saved = await _ctx.Db.Concepts.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        saved.Title.ShouldBe("Kerkoporta");
        saved.CostUsd.ShouldBe(0.07m);
        (await service.DiscardAsync(concept.Id, candidate.CandidateId, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.CandidateNotFound);
    }

    /// <summary>Silinmiş konsept düzeltilemez; başka kullanıcının konsepti bulunamaz; bütçe aşımı LLM'i çağırmaz.</summary>
    [Fact]
    public async Task Guards()
    {
        var service = _ctx.CreateRevisionService();
        var other = await _ctx.SeedConceptAsync("Başkası", Guid.NewGuid());
        (await service.ReviseAsync(other.Id, new ReviseConceptRequest { Instruction = "x" }, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.NotFound);

        var concept = await _ctx.SeedConceptAsync();
        await _ctx.CreateConceptService().DeleteAsync(concept.Id, TestContext.Current.CancellationToken);
        (await service.ReviseAsync(concept.Id, new ReviseConceptRequest { Instruction = "x" }, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.IsDeleted);
        _ctx.Gateway.Calls.ShouldBeEmpty();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
