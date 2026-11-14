using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Domain;

/// <summary>
/// <see cref="Concept"/> durum geçişleri ve belge kuralları.
/// </summary>
public sealed class ConceptTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    private static Concept NewConcept() => Concept.Create(
        Guid.NewGuid(),
        null,
        ConceptOrigin.Idea,
        new ConceptDocument { Title = " Başlık ", Scenes = [new ConceptScene { Index = 5, Narration = "a" }, new ConceptScene { Index = 9, Narration = "b" }] },
        "m",
        10,
        5,
        0.1m);

    /// <summary>Yeni konsept Generated; başlık kırpılır, sahne sırası 0'dan yeniden numaralanır.</summary>
    [Fact]
    public void Create_sets_generated_and_normalizes_document()
    {
        var c = NewConcept();
        c.Status.ShouldBe(ConceptStatus.Generated);
        c.Title.ShouldBe("Başlık");
        c.Document.Title.ShouldBe("Başlık");
        c.Document.Scenes.Select(s => s.Index).ShouldBe([0, 1]);
        c.CostUsd.ShouldBe(0.1m);
    }

    /// <summary>Generated → Approved → Deleted → (geri al) Generated; onay anı sıfırlanır.</summary>
    [Fact]
    public void Approve_delete_restore_cycle()
    {
        var c = NewConcept();
        c.Approve(Now).IsSuccess.ShouldBeTrue();
        c.Status.ShouldBe(ConceptStatus.Approved);
        c.ApprovedAtUtc.ShouldBe(Now);

        c.MarkDeleted(Now.AddMinutes(1));
        c.Status.ShouldBe(ConceptStatus.Deleted);
        c.CanPurge.ShouldBeTrue();

        c.Restore().IsSuccess.ShouldBeTrue();
        c.Status.ShouldBe(ConceptStatus.Generated);
        c.ApprovedAtUtc.ShouldBeNull();
        c.DeletedAtUtc.ShouldBeNull();
        c.CanPurge.ShouldBeFalse();
    }

    /// <summary>Silinmiş konsept onaylanamaz ve düzenlenemez.</summary>
    [Fact]
    public void Deleted_concept_rejects_approve_and_edit()
    {
        var c = NewConcept();
        c.MarkDeleted(Now);
        c.Approve(Now).Error.ShouldBe(ConceptErrors.IsDeleted);
        c.UpdateDocument(new ConceptDocument { Scenes = [new ConceptScene()] }, "x").Error.ShouldBe(ConceptErrors.IsDeleted);
    }

    /// <summary>Silinmemiş konsept geri alınamaz (Conflict).</summary>
    [Fact]
    public void Restore_requires_deleted()
    {
        var result = NewConcept().Restore();
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    /// <summary>Sahnesiz belge reddedilir; başlık verilmezse belgedeki kullanılır.</summary>
    [Fact]
    public void Update_document_rules()
    {
        var c = NewConcept();
        c.UpdateDocument(new ConceptDocument { Title = "x" }, null).Error.ShouldBe(ConceptErrors.EmptyDocument);
        c.UpdateDocument(new ConceptDocument { Title = "Yeni", Scenes = [new ConceptScene { Narration = "n" }] }, null).IsSuccess.ShouldBeTrue();
        c.Title.ShouldBe("Yeni");
        c.UpdateDocument(new ConceptDocument { Title = "Yeni", Scenes = [new ConceptScene { Narration = "n" }] }, "Elle başlık").IsSuccess.ShouldBeTrue();
        c.Document.Title.ShouldBe("Elle başlık");
    }

    /// <summary>Düzeltme kabulü belgeyi değiştirir ve maliyeti ekler; negatif kullanım yok sayılır.</summary>
    [Fact]
    public void Accept_revision_adds_usage()
    {
        var c = NewConcept();
        c.AcceptRevision(new ConceptDocument { Title = "R", Scenes = [new ConceptScene { Narration = "r" }] }, 7, 3, 0.05m).IsSuccess.ShouldBeTrue();
        c.InputTokens.ShouldBe(17);
        c.OutputTokens.ShouldBe(8);
        c.CostUsd.ShouldBe(0.15m);
        c.AddUsage(-5, -5, -1m);
        c.CostUsd.ShouldBe(0.15m);
    }
}
