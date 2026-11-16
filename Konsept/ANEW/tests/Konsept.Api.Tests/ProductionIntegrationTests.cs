using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Infrastructure.Persistence;
using Konsept.Modules.Concepts.Infrastructure.Persistence.Concrete;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Infrastructure.Persistence;
using Konsept.Modules.Production.Infrastructure.Persistence.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Production, gerçek PostgreSQL 17 ile (full profil, plan 0006): migration, uçtan uca sıra (ayar upsert'i, sıraya alma, Yukarı/Aşağı, başlat,
/// <c>jsonb</c> sabitlenen ayarlar, durdur, devam, çıkar), kullanıcı başına tek çalışan iş kısmi tekil dizini, sahiplik ve sıradaki konseptin
/// sahnelerinin koşullu kaydı (B-99).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProductionIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ProductionIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public ProductionIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Gerçek depolar ve uçlar: sıra yaşam döngüsü, tekil çalışan iş ve sahiplik.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Production_RealDatabase_QueueStartStop()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var profile = await ProfileApiTests.CreateAsync(client, "Üretim Gerçek", ct);
        var userId = (Guid)(await _factory.Database.ScalarAsync($"SELECT id FROM identity.users WHERE email = '{email}'"))!;
        var settings = new { subtitles = true, progressBar = true, codeModel = "anthropic/claude-sonnet-4-5", codeEffort = (string?)null, engine = "local/hyperframes", imageModel = (string?)null, audioModel = "local/supertonic-3" };
        (await client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", settings, ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", settings with { progressBar = false }, ct)).EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var concepts = new ConceptRepository(scope.ServiceProvider.GetRequiredService<ConceptsDbContext>());
        var ids = new List<Guid>();
        foreach (var title in new[] { "G1", "G2", "G3" })
        {
            var concept = Concept(userId, profile, title);
            ids.Add(concept.Id);
            await concepts.AddAsync(concept, ct);
        }

        (await client.PostAsJsonAsync("/api/production/queue", new { profileId = profile, conceptIds = ids }, ct)).EnsureSuccessStatusCode();
        var queued = await State(client, profile, ct);
        var jobIds = queued.GetProperty("jobs").EnumerateArray().Select(j => j.GetProperty("id").GetGuid()).ToList();
        (await client.PostAsJsonAsync($"/api/production/jobs/{jobIds[2]}/move", new { direction = "up" }, ct)).EnsureSuccessStatusCode();
        Titles(await State(client, profile, ct)).ShouldBe(["G1", "G3", "G2"]);
        using var blocked = await client.PostAsJsonAsync("/api/production/start", new { profileId = profile }, ct);
        blocked.StatusCode.ShouldBe(HttpStatusCode.Conflict); // Gerçek katalog + key deposu: key yok.
        (await blocked.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("API key yok — Ayarlar");

        // Başlatma deposu doğrudan (key'siz ortam): jsonb sabitlenen ayar gidiş-dönüşü ve kullanıcı başına tek çalışan iş dizini.
        var jobs = new ProductionJobRepository(scope.ServiceProvider.GetRequiredService<ProductionDbContext>());
        var fixedSettings = new FixedJobSettings
        {
            Subtitles = true,
            ProgressBar = false,
            CodeModel = "anthropic/claude-sonnet-4-5",
            Engine = "local/hyperframes",
            AudioModel = "local/supertonic-3",
            Language = "tr",
            Format = "portrait",
            TargetDurationSeconds = 60,
            FixedAt = DateTimeOffset.UtcNow,
        };
        (await jobs.StartAsync(userId, jobIds[0], [ProductionJobStatus.Queued], fixedSettings, DateTimeOffset.UtcNow, ct)).ShouldBeTrue();
        (await jobs.StartAsync(userId, jobIds[1], [ProductionJobStatus.Queued], fixedSettings, DateTimeOffset.UtcNow, ct)).ShouldBeFalse(); // Tekil dizin.

        var running = await State(client, profile, ct);
        running.GetProperty("jobs")[0].GetProperty("status").GetString().ShouldBe("producing");
        running.GetProperty("startBlock").GetString().ShouldBe("running_here");
        var fix = running.GetProperty("jobs")[0].GetProperty("fixed");
        fix.GetProperty("settings").GetProperty("codeModel").GetString().ShouldBe("anthropic/claude-sonnet-4-5");
        (fix.GetProperty("language").GetString(), fix.GetProperty("format").GetString(), fix.GetProperty("targetDurationSeconds").GetInt32()).ShouldBe(("tr", "portrait", 60));
        (await State(client, profile, ct)).GetProperty("settings").GetProperty("progressBar").GetBoolean().ShouldBeFalse(); // Upsert.
        (await concepts.UpdateScenesAsync(userId, ids[1], [ConceptStatus.Generated, ConceptStatus.Approved], [new("X", "Y", "Z", 60)], false, false, DateTimeOffset.UtcNow, ct))
            .ShouldBeFalse(); // B-99: sıradaki konseptin sahneleri yazılmaz.
        (await concepts.GetManyAsync(userId, [ids[1]], ct)).Single().Scenes.Count.ShouldBe(2);

        (await jobs.ListActiveAsync(Guid.NewGuid(), ct)).ShouldBeEmpty();
        (await jobs.GetAsync(Guid.NewGuid(), jobIds[0], ct)).ShouldBeNull();
        (await jobs.StopAsync(Guid.NewGuid(), null, DateTimeOffset.UtcNow, ct)).ShouldBe(0);

        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/production/jobs/{jobIds[0]}/resume", new { }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/production/jobs/{jobIds[0]}/remove", new { }, ct)).EnsureSuccessStatusCode();
        Titles(await State(client, profile, ct)).ShouldBe(["G3", "G2"]);
        (await concepts.GetManyAsync(userId, [ids[0]], ct)).Single().Status.ShouldBe(ConceptStatus.Approved);
        (await jobs.ListActiveAsync(userId, ct)).Select(j => j.Status).ShouldBe([ProductionJobStatus.Queued, ProductionJobStatus.Queued]);

        using var stranger = await AuthenticatedClient.CreateAsync(_factory, ct);
        (await stranger.GetAsync(new Uri($"/api/production/state?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.PostAsJsonAsync($"/api/production/jobs/{jobIds[1]}/remove", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<JsonElement> State(HttpClient client, Guid profile, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/production/state?profileId={profile}", ct);

    private static List<string?> Titles(JsonElement state) => [.. state.GetProperty("jobs").EnumerateArray().Select(j => j.GetProperty("title").GetString())];

    private static Concept Concept(Guid userId, Guid profile, string title)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-5);
        return new Concept
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ProfileId = profile,
            Title = title,
            TitleKey = title.ToLowerInvariant(),
            Source = ConceptSource.Idea,
            Status = ConceptStatus.Approved,
            Language = "tr",
            Format = "portrait",
            TargetDurationSeconds = 60,
            Scenes = [new("Açılış", "Harita.", "Başlıyor.", 30), new("Son", "Kapanış.", "Bitti.", 30)],
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
