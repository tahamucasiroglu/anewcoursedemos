using System.Net;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Infrastructure.Persistence;
using Konsept.Modules.Concepts.Infrastructure.Persistence.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Concepts, gerçek PostgreSQL 17 ile (full profil, spec 0014 AC-24, AC-27, AC-28, AC-30): migration, sahnelerin <c>jsonb</c> gidiş-dönüşü,
/// filtre + sayfalama + en yeni üstte, durum değişimi, kullanıcıyla süzme, silinmişler dahil geçmiş, öneri durumları ve konsepti olan
/// profilin silinememesi.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConceptsIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConceptsIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public ConceptsIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Gerçek depolar: kalıcılık, sahiplik, durum, sayfalama ve profil silme kuralı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concepts_RealDatabase_PersistOwnershipStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var profile = await ProfileApiTests.CreateAsync(client, "Gerçek Kanal", ct);
        var userId = (Guid)(await _factory.Database.ScalarAsync($"SELECT id FROM identity.users WHERE email = '{email}'"))!;
        var stranger = Guid.NewGuid();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConceptsDbContext>();
        var concepts = new ConceptRepository(context);
        var suggestions = new TopicSuggestionRepository(context);
        var start = DateTimeOffset.UtcNow.AddHours(-1);
        var ids = new List<Guid>();
        for (var i = 0; i < 52; i++)
        {
            var c = Concept(userId, profile, $"Konsept {i}", start.AddSeconds(i), i == 51 ? start : null);
            ids.Add(c.Id);
            await concepts.AddAsync(c, ct);
        }

        var first = await concepts.ListAsync(userId, profile, [ConceptStatus.Generated, ConceptStatus.Approved], 0, 50, ct);
        var second = await concepts.ListAsync(userId, profile, [ConceptStatus.Generated, ConceptStatus.Approved], 50, 50, ct);
        first.Total.ShouldBe(51); // Silinmiş olan listelenmez.
        first.Items[0].Title.ShouldBe("Konsept 50");
        first.Items[0].Scenes.Select(s => (s.Title, s.Narration, s.DurationSeconds)).ShouldBe([("Açılış", "<b>metin</b>", 20), ("Kapanış", "Son.", 40)]);
        second.Items.Single().Title.ShouldBe("Konsept 0");
        (await concepts.ListAsync(stranger, profile, [ConceptStatus.Generated], 0, 50, ct)).Total.ShouldBe(0);
        (await concepts.GetManyAsync(stranger, [ids[0]], ct)).ShouldBeEmpty();
        (await concepts.ChangeStatusAsync(stranger, [ids[0]], ConceptStatus.Generated, ConceptStatus.Approved, DateTimeOffset.UtcNow, ct)).ShouldBe(0);
        (await concepts.ChangeStatusAsync(userId, [ids[0], ids[1]], ConceptStatus.Generated, ConceptStatus.Approved, DateTimeOffset.UtcNow, ct)).ShouldBe(2);
        (await concepts.ListAsync(userId, profile, [ConceptStatus.Approved], 0, 50, ct)).Total.ShouldBe(2);
        (await concepts.ListTitlesAsync(userId, profile, ct)).Count.ShouldBe(52); // Silinmiş dahil (BR-4).
        (await concepts.ProfileIdsAsync(userId, ct)).ShouldBe([profile]);
        (await concepts.ProfileIdsAsync(stranger, ct)).ShouldBeEmpty();

        var pending = new TopicSuggestion { Id = Guid.CreateVersion7(), UserId = userId, ProfileId = profile, Title = "Öneri", TitleKey = "öneri", Summary = "Özet.", State = SuggestionState.Pending, CreatedAt = DateTimeOffset.UtcNow };
        await suggestions.AddRangeAsync([pending], ct);
        await suggestions.SetPendingStateAsync(stranger, profile, null, SuggestionState.Rejected, ct);
        (await suggestions.ListAsync(userId, profile, ct)).Single().State.ShouldBe(SuggestionState.Pending);
        await suggestions.SetPendingStateAsync(userId, profile, null, SuggestionState.Rejected, ct);
        (await suggestions.ListAsync(userId, profile, ct)).Single().State.ShouldBe(SuggestionState.Rejected);
        (await suggestions.ListAsync(stranger, profile, ct)).ShouldBeEmpty();

        using var delete = await client.DeleteAsync(new Uri($"/api/profiles/{profile}", UriKind.Relative), ct);
        delete.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Spec 0015 (AC-2, AC-14): gerçek depoda sahne değişikliği <c>jsonb</c>'ye yazılır, durum ve başlık aynı kalır; başkasının ya da izin
    /// verilmeyen durumdaki konsepti değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concepts_RealDatabase_UpdateScenes_OwnedEditableOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var profile = await ProfileApiTests.CreateAsync(client, "Düzenleme Kanalı", ct);
        var userId = (Guid)(await _factory.Database.ScalarAsync($"SELECT id FROM identity.users WHERE email = '{email}'"))!;
        await using var scope = _factory.Services.CreateAsyncScope();
        var concepts = new ConceptRepository(scope.ServiceProvider.GetRequiredService<ConceptsDbContext>());
        var concept = Concept(userId, profile, "Düzenlenecek", DateTimeOffset.UtcNow, null);
        await concepts.AddAsync(concept, ct);
        ConceptScene[] edited = [new("Yeni", string.Empty, "Yeni metin.", 60)];

        (await concepts.UpdateScenesAsync(Guid.NewGuid(), concept.Id, [ConceptStatus.Generated], edited, true, false, DateTimeOffset.UtcNow, ct)).ShouldBeFalse();
        (await concepts.UpdateScenesAsync(userId, concept.Id, [ConceptStatus.Approved], edited, true, false, DateTimeOffset.UtcNow, ct)).ShouldBeFalse();
        (await concepts.UpdateScenesAsync(userId, concept.Id, [ConceptStatus.Generated, ConceptStatus.Approved], edited, true, false, DateTimeOffset.UtcNow, ct)).ShouldBeTrue();

        await using var fresh = _factory.Services.CreateAsyncScope();
        var stored = (await new ConceptRepository(fresh.ServiceProvider.GetRequiredService<ConceptsDbContext>()).GetManyAsync(userId, [concept.Id], ct)).Single();
        stored.Scenes.ShouldBe(edited);
        stored.WordRepetitionWarning.ShouldBeTrue();
        stored.Status.ShouldBe(ConceptStatus.Generated);
        stored.Title.ShouldBe("Düzenlenecek");
    }

    /// <summary>
    /// Spec 0016 (AC-1, AC-6, AC-7, AC-9, AC-11): gerçek depoda soft delete, Silinenler listesi (en yeni silinen üstte), silinmişin "elenen"
    /// geçmişte olması, kalıcı silme ve yalnızca seçilmemiş önerilerin sıfırlanması; başkasının kaydı değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concepts_RealDatabase_TrashPurgeReset_Owned()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var profile = await ProfileApiTests.CreateAsync(client, "Çöp Kanalı", ct);
        var userId = (Guid)(await _factory.Database.ScalarAsync($"SELECT id FROM identity.users WHERE email = '{email}'"))!;
        var stranger = Guid.NewGuid();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConceptsDbContext>();
        var concepts = new ConceptRepository(context);
        var suggestions = new TopicSuggestionRepository(context);
        var older = Concept(userId, profile, "Önce silinen", DateTimeOffset.UtcNow.AddHours(-2), null);
        var newer = Concept(userId, profile, "Sonra silinen", DateTimeOffset.UtcNow.AddHours(-2), null);
        await concepts.AddAsync(older, ct);
        await concepts.AddAsync(newer, ct);
        var now = DateTimeOffset.UtcNow;

        (await concepts.SoftDeleteAsync(stranger, older.Id, [ConceptStatus.Generated], now, ct)).ShouldBeFalse();
        (await concepts.SoftDeleteAsync(userId, older.Id, [ConceptStatus.Approved], now, ct)).ShouldBeFalse();
        (await concepts.SoftDeleteAsync(userId, older.Id, [ConceptStatus.Generated], now.AddMinutes(-1), ct)).ShouldBeTrue();
        (await concepts.SoftDeleteAsync(userId, newer.Id, [ConceptStatus.Generated], now, ct)).ShouldBeTrue();

        var page = await concepts.ListDeletedAsync(userId, profile, 0, 50, ct);
        page.Items.Select(c => c.Title).ShouldBe(["Sonra silinen", "Önce silinen"]);
        (await concepts.ListDeletedAsync(stranger, profile, 0, 50, ct)).Total.ShouldBe(0);
        (await concepts.GetManyAsync(userId, [older.Id], ct)).ShouldBeEmpty();
        (await concepts.GetDeletedAsync(userId, older.Id, ct))!.Scenes.Count.ShouldBe(2);
        (await concepts.ListTitlesAsync(userId, profile, ct)).ShouldAllBe(t => !t.Chosen);

        (await concepts.PurgeAsync(stranger, older.Id, ct)).ShouldBeFalse();
        (await concepts.PurgeAsync(userId, older.Id, ct)).ShouldBeTrue();
        (await concepts.GetDeletedAsync(userId, older.Id, ct)).ShouldBeNull();
        (await concepts.ListTitlesAsync(userId, profile, ct)).Select(t => t.Title).ShouldBe(["Sonra silinen"]);

        TopicSuggestion Suggestion(string title, SuggestionState state) =>
            new() { Id = Guid.CreateVersion7(), UserId = userId, ProfileId = profile, Title = title, TitleKey = title, Summary = "Özet.", State = state, CreatedAt = DateTimeOffset.UtcNow };
        await suggestions.AddRangeAsync([Suggestion("red", SuggestionState.Rejected), Suggestion("seçili", SuggestionState.Selected), Suggestion("bekleyen", SuggestionState.Pending)], ct);
        (await suggestions.DeleteRejectedAsync(stranger, profile, ct)).ShouldBe(0);
        (await suggestions.DeleteRejectedAsync(userId, profile, ct)).ShouldBe(1);
        await suggestions.DeleteSelectedAsync(userId, profile, "seçili", ct);
        (await suggestions.ListAsync(userId, profile, ct)).Select(s => s.Title).ShouldBe(["bekleyen"]);
    }

    private static Concept Concept(Guid userId, Guid profile, string title, DateTimeOffset at, DateTimeOffset? deletedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        ProfileId = profile,
        Title = title,
        TitleKey = title.ToLowerInvariant(),
        Source = ConceptSource.Idea,
        Status = ConceptStatus.Generated,
        Language = "tr",
        Format = "landscape",
        TargetDurationSeconds = 60,
        Scenes = [new ConceptScene("Açılış", "Harita", "<b>metin</b>", 20), new ConceptScene("Kapanış", "Kale", "Son.", 40)],
        CreatedAt = at,
        UpdatedAt = at,
        DeletedAt = deletedAt,
    };
}
