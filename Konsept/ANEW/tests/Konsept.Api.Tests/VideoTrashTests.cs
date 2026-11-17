using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.Production.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Video silme ve Silinenler — Videolar, Api tarafı (spec 0019 AC-1–AC-6, AC-9): Sil yalnızca veritabanıdır (ajana komut yok, dosya kalır),
/// silinen video Üretilen Videolar'dan kalkar ve Silinenler'de görünür; Kalıcı sil ajana tüm sürüm dosyalarını sildirir, sonra kaydı kaldırır
/// (klip izi kalır — B-130); ajan yokken ya da dosya silinemezse kayıt kalır. Gerçek Production/AgentGateway/Identity; bellek depoları, sahte
/// ajan kanalı (gerçek ajan/dosya yok).
/// </summary>
public sealed class VideoTrashTests : IClassFixture<ProductionApiFactory>
{
    private readonly ProductionApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="VideoTrashTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public VideoTrashTests(ProductionApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-1: Sil videoyu tüm sürümleriyle Üretilen Videolar listesinden, ayrıntısından, akışından ve dosya eylemlerinden kaldırır; Silinenler'de
    /// başlık, silinme anı ve sürüm sayısıyla görünür; kayıtlar (ve dosyalar) yerinde durur, ajana silme komutu gitmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_Delete_AllVersionsToDeleted_FilesUntouched()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (client, profile, user) = await SetupAsync(ct);
        await RegisterAgentAsync(user, ct);
        var video = Guid.CreateVersion7();
        var at = DateTimeOffset.UtcNow.AddDays(-1);
        var v1 = await AddAsync(user, profile, video, "Eski Ad", at);
        var v2 = await AddAsync(user, profile, video, "Kuşatma", at.AddHours(1));
        var kept = await AddAsync(user, profile, Guid.CreateVersion7(), "Kalan", at);
        var before = DateTimeOffset.UtcNow;

        using var deleted = await client.PostAsJsonAsync($"/api/produced/{video}/delete", new { }, ct);
        deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
        var item = await deleted.Content.ReadFromJsonAsync<JsonElement>(ct);
        item.GetProperty("id").GetGuid().ShouldBe(video);
        item.GetProperty("title").GetString().ShouldBe("Kuşatma");
        item.GetProperty("versionCount").GetInt32().ShouldBe(2);

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/produced?profileId={profile}", ct);
        list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Kalan"]);
        (await client.GetAsync(new Uri($"/api/produced/{video}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/production/videos/{v2}/stream", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/produced/versions/{v1}/open", new { action = "folder" }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/produced/{video}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound); // Zaten silinmiş.

        var trash = await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={profile}", ct);
        var row = trash.GetProperty("items").EnumerateArray().Single();
        row.GetProperty("id").GetGuid().ShouldBe(video);
        row.GetProperty("title").GetString().ShouldBe("Kuşatma");
        row.GetProperty("versionCount").GetInt32().ShouldBe(2);
        row.GetProperty("deletedAt").GetDateTimeOffset().ShouldBeGreaterThanOrEqualTo(before);

        var rows = _factory.Videos.Rows.Where(v => v.ConceptId == video).ToList();
        rows.Count.ShouldBe(2); // Kayıtlar durur (soft delete), içerik aynı.
        rows.ShouldAllBe(v => v.DeletedAt != null);
        rows.Select(v => v.DeletedAt).Distinct().Count().ShouldBe(1);
        _factory.Videos.Rows.Single(v => v.Id == kept).DeletedAt.ShouldBeNull();
        _factory.Agent.Sent.ShouldNotContain(s => s.Method == "DeleteFiles"); // Dosyalar diskte kalır.
    }

    /// <summary>AC-2: ajan çevrimdışıyken (ve dosya diskte yokken — Sil dosyaya bakmaz) Sil başarılı olur ve ajana komut gönderilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_Delete_AgentOffline_NoCommand()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (client, profile, user) = await SetupAsync(ct);
        var video = Guid.CreateVersion7();
        await AddAsync(user, profile, video, "Dosyası yok", DateTimeOffset.UtcNow);

        (await client.PostAsJsonAsync($"/api/produced/{video}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        _factory.Agent.Sent.ShouldBeEmpty();
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={profile}", ct)).GetProperty("total").GetInt32().ShouldBe(1);
    }

    /// <summary>
    /// AC-3: Silinenler — Videolar en yeni silinen üstte, yalnızca seçili profilin silinmiş videoları; 50'şerli sayfa; boş profil boş liste;
    /// başka kullanıcının profili 404.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_DeletedList_NewestFirst_ProfileScoped()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user) = await SetupAsync(ct);
        var other = await ProfileAsync(client, ct);
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var elsewhere = Guid.CreateVersion7();
        await AddAsync(user, profile, first, "Önce silinen", DateTimeOffset.UtcNow.AddDays(-2));
        await AddAsync(user, profile, second, "Sonra silinen", DateTimeOffset.UtcNow.AddDays(-3));
        await AddAsync(user, other, elsewhere, "Başka profil", DateTimeOffset.UtcNow);
        await AddAsync(user, profile, Guid.CreateVersion7(), "Silinmedi", DateTimeOffset.UtcNow);
        foreach (var id in new[] { first, second, elsewhere })
        {
            (await client.PostAsJsonAsync($"/api/produced/{id}/delete", new { }, ct)).EnsureSuccessStatusCode();
            await Task.Delay(5, ct); // Silinme anları farklı olsun.
        }

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={profile}&page=1", ct);
        list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Sonra silinen", "Önce silinen"]);
        list.GetProperty("pageSize").GetInt32().ShouldBe(50);
        list.GetProperty("total").GetInt32().ShouldBe(2);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={other}", ct))
            .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Başka profil"]);
        var empty = await ProfileAsync(client, ct);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={empty}", ct)).GetProperty("items").GetArrayLength().ShouldBe(0);

        var (intruder, _, _) = await SetupAsync(ct);
        (await intruder.GetAsync(new Uri($"/api/produced/deleted?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// AC-4: Kalıcı sil tüm sürüm dosya adlarını yalnızca sahibin ajanına gönderir; hepsi silinince kayıt hiçbir yerde görünmez (Silinenler,
    /// liste, ayrıntı) ve klip izi kalır (admin klip sayısı düşmez — B-130); ikinci istek 404.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_Purge_DeletesFilesAndRecord()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (client, profile, user) = await SetupAsync(ct);
        var conn = await RegisterAgentAsync(user, ct);
        var video = Guid.CreateVersion7();
        var at = DateTimeOffset.UtcNow.AddDays(-1);
        var v1 = await AddAsync(user, profile, video, "Kalıcı", at);
        var v2 = await AddAsync(user, profile, video, "Kalıcı", at.AddHours(2));
        var names = _factory.Videos.Rows.Where(v => v.ConceptId == video).Select(v => v.FileName).ToList();
        (await client.PostAsJsonAsync($"/api/produced/{video}/delete", new { }, ct)).EnsureSuccessStatusCode();

        using var purged = await client.PostAsJsonAsync($"/api/produced/deleted/{video}/purge", new { }, ct);
        purged.StatusCode.ShouldBe(HttpStatusCode.OK);

        var sent = _factory.Agent.Sent.Where(s => s.Method == "DeleteFiles").ToList();
        sent.Count.ShouldBe(1);
        sent[0].ConnectionId.ShouldBe(conn);
        names.ShouldAllBe(n => sent[0].Payload.Contains(n, StringComparison.Ordinal));
        _factory.Videos.Rows.ShouldNotContain(v => v.ConceptId == video);
        _factory.Videos.Tombstones[video].ShouldBe(at);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={profile}", ct)).GetProperty("total").GetInt32().ShouldBe(0);
        (await client.GetAsync(new Uri($"/api/produced/{video}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/production/videos/{v1}/stream", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/production/videos/{v2}/stream", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/produced/deleted/{video}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>AC-5: ajan çevrimdışıyken kalıcı sil "Ajan bağlı değil" (409) döner; kayıt Silinenler'de kalır, ajana komut gitmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_Purge_AgentOffline_RecordKept()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (client, profile, user) = await SetupAsync(ct);
        var video = Guid.CreateVersion7();
        await AddAsync(user, profile, video, "Bekleyen", DateTimeOffset.UtcNow);
        (await client.PostAsJsonAsync($"/api/produced/{video}/delete", new { }, ct)).EnsureSuccessStatusCode();

        using var offline = await client.PostAsJsonAsync($"/api/produced/deleted/{video}/purge", new { }, ct);
        offline.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await offline.Content.ReadAsStringAsync(ct)).ShouldContain("Ajan bağlı değil");

        _factory.Agent.Sent.ShouldBeEmpty();
        _factory.Videos.Rows.Single(v => v.ConceptId == video).DeletedAt.ShouldNotBeNull();
        _factory.Videos.Tombstones.ContainsKey(video).ShouldBeFalse();
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={profile}", ct)).GetProperty("total").GetInt32().ShouldBe(1);
    }

    /// <summary>
    /// AC-6: ajan bir dosyayı silemezse "Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin" (409), kayıt Silinenler'de kalır; tekrar
    /// denendiğinde (kalan silinir, olmayan silinmiş sayılır — ajan tarafı <c>VideoFileDeleterTests</c>) kayıt gider; yetim kayıt kalmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_Purge_PartialFailure_ThenRetry()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (client, profile, user) = await SetupAsync(ct);
        await RegisterAgentAsync(user, ct);
        var video = Guid.CreateVersion7();
        await AddAsync(user, profile, video, "Açık dosya", DateTimeOffset.UtcNow.AddHours(-2));
        await AddAsync(user, profile, video, "Açık dosya", DateTimeOffset.UtcNow.AddHours(-1));
        var locked = _factory.Videos.Rows.Where(v => v.ConceptId == video).Select(v => v.FileName).First();
        (await client.PostAsJsonAsync($"/api/produced/{video}/delete", new { }, ct)).EnsureSuccessStatusCode();
        _factory.Agent.DeleteFiles = (_, _) => Task.FromResult(new AgentDeleteFilesReply(null!)); // Bozuk cevap: silindi sayılmaz (review r1 L-2).
        (await client.PostAsJsonAsync($"/api/produced/deleted/{video}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _factory.Videos.Rows.Count(v => v.ConceptId == video).ShouldBe(2);
        _factory.Agent.DeleteFiles = (_, _) => Task.FromResult(new AgentDeleteFilesReply([locked]));

        using var partial = await client.PostAsJsonAsync($"/api/produced/deleted/{video}/purge", new { }, ct);
        partial.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await partial.Content.ReadAsStringAsync(ct)).ShouldContain("Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin");
        _factory.Videos.Rows.Count(v => v.ConceptId == video).ShouldBe(2);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/deleted?profileId={profile}", ct)).GetProperty("total").GetInt32().ShouldBe(1);

        _factory.Agent.DeleteFiles = FakeAgentChannel.DefaultDeleteFiles;
        (await client.PostAsJsonAsync($"/api/produced/deleted/{video}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.Videos.Rows.ShouldNotContain(v => v.ConceptId == video);
        _factory.Agent.Sent.Count(s => s.Method == "DeleteFiles").ShouldBe(3);
    }

    /// <summary>
    /// AC-9: başka kullanıcının videosu için Sil ve Kalıcı sil "bulunamadı" döner, hiçbir kayıt değişmez ve ajana komut gitmez (sahibin ajanı
    /// bağlıyken bile); oturumsuz istek 401.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_OtherUser_NotFound_NoAgentCommand()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (owner, profile, ownerId) = await SetupAsync(ct);
        var (intruder, _, intruderId) = await SetupAsync(ct);
        await RegisterAgentAsync(ownerId, ct);
        await RegisterAgentAsync(intruderId, ct);
        var live = Guid.CreateVersion7();
        var trashed = Guid.CreateVersion7();
        await AddAsync(ownerId, profile, live, "Canlı", DateTimeOffset.UtcNow);
        await AddAsync(ownerId, profile, trashed, "Çöpte", DateTimeOffset.UtcNow);
        (await owner.PostAsJsonAsync($"/api/produced/{trashed}/delete", new { }, ct)).EnsureSuccessStatusCode();
        var snapshot = _factory.Videos.Rows.Where(v => v.UserId == ownerId).OrderBy(v => v.Id).ToList();

        (await intruder.PostAsJsonAsync($"/api/produced/{live}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.PostAsJsonAsync($"/api/produced/deleted/{trashed}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.PostAsJsonAsync($"/api/produced/deleted/{live}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        _factory.Videos.Rows.Where(v => v.UserId == ownerId).OrderBy(v => v.Id).ToList().ShouldBe(snapshot);
        _factory.Agent.Sent.ShouldNotContain(s => s.Method == "DeleteFiles");

        using var anonymous = _factory.CreateClient();
        (await anonymous.PostAsJsonAsync($"/api/produced/{live}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync($"/api/produced/deleted/{trashed}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(new Uri($"/api/produced/deleted?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<Guid> ProfileAsync(HttpClient client, CancellationToken ct)
    {
        using var created = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body("Çöp " + Guid.NewGuid().ToString("N")[..6]), ct);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
    }

    private async Task<(HttpClient Client, Guid Profile, Guid User)> SetupAsync(CancellationToken ct)
    {
        var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        return (client, profile, _factory.Profiles.Rows.Single(p => p.Id == profile).UserId);
    }

    private async Task<string> RegisterAgentAsync(Guid userId, CancellationToken ct)
    {
        var conn = "trash-" + Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>()
            .RegisterAsync(conn, new AgentOwner(userId, Guid.NewGuid()), new AgentRegistration("Ofis-PC", "1.0.0"), ct);
        return conn;
    }

    private async Task<Guid> AddAsync(Guid user, Guid profile, Guid concept, string title, DateTimeOffset endedAt)
    {
        var id = Guid.CreateVersion7();
        await _factory.Videos.AddAsync(
            new ProducedVideo
            {
                Id = id,
                UserId = user,
                ProfileId = profile,
                ConceptId = concept,
                JobId = Guid.CreateVersion7(),
                Title = title,
                ConceptText = "{}",
                CodeModel = "anthropic/claude-sonnet-4-5",
                Engine = "local/hyperframes",
                FileName = "video-" + id.ToString("N") + ".mp4",
                StartedAt = endedAt.AddMinutes(-4),
                EndedAt = endedAt,
                DurationSeconds = 60,
            },
            CancellationToken.None);
        return id;
    }
}
