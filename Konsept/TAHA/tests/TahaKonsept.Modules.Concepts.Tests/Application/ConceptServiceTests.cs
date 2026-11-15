using Shouldly;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Concepts.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Konsept servisi: listeler, düzenleme (kanal alanları korunur), onay, sil → Silinenler → geri al / kalıcı sil, konu hafızası etkisi.
/// </summary>
public sealed class ConceptServiceTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    /// <summary>Sil → Silinenler'de, hafızada negatif; geri al → Üretilenler; kalıcı sil yalnız silinmişte ve hafıza izini siler.</summary>
    [Fact]
    public async Task Delete_restore_purge_flow()
    {
        var service = _ctx.CreateConceptService();
        var concept = await _ctx.SeedConceptAsync();

        (await service.PurgeAsync(concept.Id, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.NotDeleted);
        (await service.DeleteAsync(concept.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await service.ListAsync(ConceptStatus.Generated, TestContext.Current.CancellationToken)).Value.ShouldBeEmpty();
        (await service.ListDeletedAsync(TestContext.Current.CancellationToken)).Value.Single().DeletedAt.ShouldNotBeNull();
        (await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken)).Items.Single().Kind.ShouldBe(TopicMemoryKind.Deleted);

        (await service.RestoreAsync(concept.Id, TestContext.Current.CancellationToken)).Value.Status.ShouldBe(ConceptStatus.Generated);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken)).Items.Single().Kind.ShouldBe(TopicMemoryKind.Generated);

        await service.DeleteAsync(concept.Id, TestContext.Current.CancellationToken);
        (await service.PurgeAsync(concept.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await service.GetAsync(concept.Id, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.NotFound);
        (await _ctx.Memory.LoadAsync(_ctx.UserId, _ctx.Profile.Id, TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
    }

    /// <summary>Onaylanan Approved listesine geçer; durum verilmeyen liste silinmemişlerin hepsi.</summary>
    [Fact]
    public async Task Approve_moves_to_approved_list()
    {
        var service = _ctx.CreateConceptService();
        var a = await _ctx.SeedConceptAsync("A");
        await _ctx.SeedConceptAsync("B");
        (await service.ApproveAsync(a.Id, TestContext.Current.CancellationToken)).Value.ApprovedAt.ShouldNotBeNull();
        (await service.ListAsync(ConceptStatus.Approved, TestContext.Current.CancellationToken)).Value.Single().Title.ShouldBe("A");
        (await service.ListAsync(ConceptStatus.Generated, TestContext.Current.CancellationToken)).Value.Single().Title.ShouldBe("B");
        (await service.ListAsync(null, TestContext.Current.CancellationToken)).Value.Count.ShouldBe(2);
    }

    /// <summary>Düzenleme belgeyi değiştirir; dil/biçim/hedef süre korunur; başkasının konsepti bulunamaz.</summary>
    [Fact]
    public async Task Update_preserves_channel_fields()
    {
        var service = _ctx.CreateConceptService();
        var concept = await _ctx.SeedConceptAsync();
        var edited = new ConceptDocument
        {
            Title = "Düzenlendi",
            Language = "en",
            Format = VideoFormat.Horizontal,
            TargetDurationSec = 999,
            Scenes = [new ConceptScene { Title = "Yeni", Narration = "Yeni anlatım", VisualPrompt = "New prompt" }],
        };

        var result = await service.UpdateAsync(concept.Id, new UpdateConceptRequest { Document = edited }, TestContext.Current.CancellationToken);

        result.Value.Title.ShouldBe("Düzenlendi");
        result.Value.Document.Language.ShouldBe("tr");
        result.Value.Document.Format.ShouldBe(VideoFormat.Vertical);
        result.Value.Document.TargetDurationSec.ShouldBe(60);
        result.Value.Document.Scenes.Single().VisualPrompt.ShouldBe("New prompt");

        var foreign = await _ctx.SeedConceptAsync("X", Guid.NewGuid());
        (await service.UpdateAsync(foreign.Id, new UpdateConceptRequest { Document = edited }, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.NotFound);
    }

    /// <summary>Okuyucu (Production): kullanıcı filtresi; okunuş kaynağı belge okunuşlarını verir.</summary>
    [Fact]
    public async Task Reader_filters_by_user()
    {
        var concept = await _ctx.SeedConceptAsync();
        var reader = new TahaKonsept.Modules.Concepts.Application.Concrete.ConceptReader(_ctx.Concepts);
        (await reader.GetAsync(_ctx.UserId, concept.Id, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        (await reader.GetAsync(Guid.NewGuid(), concept.Id, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await reader.GetPronunciationsAsync(Guid.NewGuid(), concept.Id, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    /// <summary>
    /// Kullanıcı ayrımı: B'nin konsepti (silinmiş olanı dahil) A'nın listelerinde görünmez; getir/onayla/sil/geri al/kalıcı sil
    /// NotFound döner (varlığı sızdırılmaz) ve B'nin kaydı değişmeden kalır.
    /// </summary>
    [Fact]
    public async Task Other_users_concepts_are_invisible_and_untouchable()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = _ctx.CreateConceptService();
        var userB = Guid.CreateVersion7();
        var active = await _ctx.SeedConceptAsync("B aktif", userB);
        var deleted = await _ctx.SeedConceptAsync("B silinmiş", userB);
        var tracked = await _ctx.Db.Concepts.FindAsync([deleted.Id], ct);
        tracked!.MarkDeleted(_ctx.Clock.UtcNow);
        await _ctx.Db.SaveChangesAsync(ct);
        _ctx.Db.ChangeTracker.Clear();

        (await service.ListAsync(null, ct)).Value.ShouldBeEmpty();
        (await service.ListDeletedAsync(ct)).Value.ShouldBeEmpty();
        (await service.GetAsync(active.Id, ct)).Error.ShouldBe(ConceptErrors.NotFound);
        (await service.ApproveAsync(active.Id, ct)).Error.ShouldBe(ConceptErrors.NotFound);
        (await service.DeleteAsync(active.Id, ct)).Error.ShouldBe(ConceptErrors.NotFound);
        (await service.RestoreAsync(deleted.Id, ct)).Error.ShouldBe(ConceptErrors.NotFound);
        (await service.PurgeAsync(deleted.Id, ct)).Error.ShouldBe(ConceptErrors.NotFound);

        (await _ctx.Concepts.GetForUserAsync(active.Id, userB, ct))!.Status.ShouldBe(ConceptStatus.Generated);
        (await _ctx.Concepts.GetForUserAsync(deleted.Id, userB, ct))!.Status.ShouldBe(ConceptStatus.Deleted);
    }

    /// <summary>Bağlam kurucu başka kullanıcının profil kimliğini kullanmaz: okuyucu ona <c>null</c> verir, kendi aktif profiline düşer.</summary>
    [Fact]
    public async Task Context_ignores_profile_id_of_other_user()
    {
        var ct = TestContext.Current.CancellationToken;
        var userB = Guid.CreateVersion7();

        var context = await _ctx.Contexts.ResolveAsync(userB, _ctx.Profile.Id, ct);

        context.Error.ShouldBe(ConceptErrors.NoProfile);
        (await _ctx.Contexts.ResolveAsync(_ctx.UserId, _ctx.Profile.Id, ct)).Value.ProfileId.ShouldBe(_ctx.Profile.Id);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
