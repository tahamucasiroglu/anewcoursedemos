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

    /// <summary>
    /// Plan 0017 (gerçek veritabanı, AddPipeline migration'ı): iş ilerlemesi jsonb gidiş-dönüşü, koşullu durum geçişi (tekil çalışan iş
    /// kısıtı dahil), tüm kullanıcıların çalışan işleri, konsept başına aşama çıktıları (başkasının satırı değişmez), video kaydı (en yeni
    /// üstte, sahibine göre) ve sıra durumu upsert'i.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_RealDatabase_Repositories()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductionDbContext>();
        var jobs = new ProductionJobRepository(db);
        var (userId, profileId) = (Guid.CreateVersion7(), Guid.CreateVersion7());
        var now = DateTimeOffset.UtcNow;
        var ids = new[] { Guid.CreateVersion7(), Guid.CreateVersion7() };
        for (var i = 0; i < ids.Length; i++)
        {
            await jobs.AddAsync(
                new ProductionJob
                {
                    Id = ids[i],
                    UserId = userId,
                    ProfileId = profileId,
                    ConceptId = Guid.CreateVersion7(),
                    Title = "P" + i,
                    Language = "tr",
                    Format = "landscape",
                    TargetDurationSeconds = 60,
                    Position = i + 1,
                    Status = ProductionJobStatus.Queued,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                ct);
        }

        var fixedSettings = new FixedJobSettings { Engine = "local/hyperframes", Language = "tr", Format = "landscape", FixedAt = now };
        (await jobs.StartAsync(userId, ids[0], [ProductionJobStatus.Queued], fixedSettings, now, ct)).ShouldBeTrue();
        var progress = new JobProgress
        {
            Stages = [new StageProgress(PipelineStage.Analysis, StageState.Done), new StageProgress(PipelineStage.Render, StageState.Running, "gpu")],
            CostUsd = 0.012345m,
            Error = "Ajan kurulumu eksik: HyperFrames",
            FixUsed = true,
            StartedAt = now,
        };
        await jobs.SetProgressAsync(userId, ids[0], progress, now, ct);
        var read = (await jobs.GetAsync(userId, ids[0], ct))!.Progress!;
        (read.CostUsd, read.Error, read.FixUsed, read.StateOf(PipelineStage.Render), read.Stages[1].Device)
            .ShouldBe((0.012345m, "Ajan kurulumu eksik: HyperFrames", true, StageState.Running, "gpu"));
        (await jobs.ListRunningAsync(ct)).ShouldContain(j => j.Id == ids[0]);
        (await jobs.TransitionAsync(userId, ids[0], [ProductionJobStatus.Producing], ProductionJobStatus.WaitingForAgent, now, ct)).ShouldBeTrue();
        (await jobs.TransitionAsync(userId, ids[1], [ProductionJobStatus.Queued], ProductionJobStatus.Producing, now, ct)).ShouldBeFalse(); // Tekil çalışan iş.
        (await jobs.TransitionAsync(Guid.NewGuid(), ids[0], [ProductionJobStatus.WaitingForAgent], ProductionJobStatus.Failed, now, ct)).ShouldBeFalse();
        (await jobs.TransitionAsync(userId, ids[0], [ProductionJobStatus.WaitingForAgent], ProductionJobStatus.Completed, now, ct)).ShouldBeTrue();

        var outputs = new StageOutputRepository(db);
        var concept = Guid.CreateVersion7();
        var row = new StageOutputs
        {
            UserId = userId,
            ConceptId = concept,
            TextHash = new string('a', 64),
            Plan = [new ScenePlan(0, 5, "n", "p")],
            Codes = ["<html></html>"],
            LocalDevice = Guid.NewGuid(),
            LocalDone = [PipelineStage.Images],
            UpdatedAt = now,
        };
        await outputs.UpsertAsync(row, ct);
        await outputs.UpsertAsync(row with { Codes = ["<html>2</html>"], LocalDone = [PipelineStage.Images, PipelineStage.Render] }, ct);
        await outputs.UpsertAsync(row with { UserId = Guid.NewGuid(), Codes = null }, ct); // Başkası yazamaz.
        var stored = (await outputs.GetAsync(userId, concept, ct))!;
        (stored.Codes!.Single(), stored.LocalDone.Count, stored.Plan!.Single().ImagePrompt).ShouldBe(("<html>2</html>", 2, "p"));
        (await outputs.GetAsync(Guid.NewGuid(), concept, ct)).ShouldBeNull();

        var videos = new ProducedVideoRepository(db);
        foreach (var (title, minutes) in new[] { ("Eski", -10), ("Yeni", -1) })
        {
            await videos.AddAsync(
                new ProducedVideo
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    ProfileId = profileId,
                    ConceptId = concept,
                    JobId = ids[0],
                    Title = title,
                    ConceptText = "{\"title\":\"" + title + "\"}",
                    Engine = "local/hyperframes",
                    FileName = title + ".mp4",
                    StartedAt = now.AddMinutes(minutes - 1),
                    EndedAt = now.AddMinutes(minutes),
                },
                ct);
        }

        (await videos.ListAsync(userId, profileId, ct)).Select(v => v.Title).ShouldBe(["Yeni", "Eski"]);
        (await videos.ListAsync(Guid.NewGuid(), profileId, ct)).ShouldBeEmpty();

        var queue = new QueueStateRepository(db);
        await queue.SetAsync(new QueueState { UserId = userId, ConsecutiveFailures = 3, Halted = true, UpdatedAt = now }, ct);
        await queue.SetAsync(new QueueState { UserId = userId, ConsecutiveFailures = 0, Halted = false, UpdatedAt = now }, ct);
        (await queue.GetAsync(userId, ct))!.Halted.ShouldBeFalse();
    }

    /// <summary>
    /// Spec 0007 AC-11 (gerçek PostgreSQL, kasıtlı bozma hedefi): sürüm kaydının yeni alanları (süre, biçim, maliyet) yazılıp okunur; konseptin
    /// sürümleri eskiden yeniye gelir; başka kullanıcı ne konseptin sürümlerini ne tek sürümü okuyabilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Produced_RealDatabase_OwnerFilter()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = _factory.Services.CreateAsyncScope();
        var videos = new ProducedVideoRepository(scope.ServiceProvider.GetRequiredService<ProductionDbContext>());
        var (userId, profileId, concept) = (Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        var now = DateTimeOffset.UtcNow;
        var ids = new List<Guid>();
        foreach (var (title, minutes) in new[] { ("Yeni", -1), ("Eski", -10) })
        {
            var id = Guid.CreateVersion7();
            ids.Add(id);
            await videos.AddAsync(
                new ProducedVideo
                {
                    Id = id,
                    UserId = userId,
                    ProfileId = profileId,
                    ConceptId = concept,
                    JobId = Guid.CreateVersion7(),
                    Title = title,
                    ConceptText = "{\"title\":\"" + title + "\"}",
                    Engine = "local/hyperframes",
                    FileName = title + ".mp4",
                    StartedAt = now.AddMinutes(minutes - 1),
                    EndedAt = now.AddMinutes(minutes),
                    DurationSeconds = 95,
                    Format = "portrait",
                    CostUsd = 0.004321m,
                },
                ct);
        }

        var versions = await videos.ListByConceptAsync(userId, concept, ct);
        versions.Select(v => v.Title).ShouldBe(["Eski", "Yeni"]);
        (versions[0].DurationSeconds, versions[0].Format, versions[0].CostUsd).ShouldBe((95, "portrait", 0.004321m));
        (await videos.ListByConceptAsync(Guid.NewGuid(), concept, ct)).ShouldBeEmpty();
        (await videos.GetAsync(Guid.NewGuid(), ids[0], ct)).ShouldBeNull();
        (await videos.GetAsync(userId, ids[0], ct))!.Title.ShouldBe("Yeni");
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
