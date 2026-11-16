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
