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
/// Üretilen Videolar, Api tarafı (spec 0007): liste (profil, en az bir tamamlanmış sürüm, ilk sürüm tarihi, 50'şerli), video ayrıntısı (sürümler,
/// değişiklik özeti, maliyet, her sürümün kendi konsept metni), dosya eylemleri (yalnızca sahibin ajanı, dosya yok, ajan yok, 10 sn zaman aşımı)
/// ve sahiplik (BR-1: başkasınınki 404, ajana komut gitmez). Gerçek Production/AgentGateway/Identity; bellek depoları, sahte ajan kanalı.
/// </summary>
public sealed class ProducedVideosTests : IClassFixture<ProductionApiFactory>
{
    private readonly ProductionApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ProducedVideosTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ProducedVideosTests(ProductionApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-1, AC-2: liste yalnızca seçili profilin videoları; öğe başlık, süre, üretim tarihi (ilk tamamlanan sürümün bitişi) ve sürüm sayısı;
    /// en yeni üstte; 51 videolu profilde ilk parça 50 + devamı var, ikinci parça 51. öğe; başka profilin videosu yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Produced_List_ProfileOnly_FirstVersionDate_Paging()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user) = await SetupAsync(ct);
        var other = await ProfileAsync(client, ct);
        var start = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 50; i++) // + iki sürümlü video = 51 video.
        {
            await AddAsync(user, profile, Guid.CreateVersion7(), $"Video {i:00}", start.AddHours(i));
        }

        // İki sürümlü video: ilk sürüm en eski → üretim tarihi ilk sürümün bitişi, başlık/süre en son sürümden.
        var twice = Guid.CreateVersion7();
        await AddAsync(user, profile, twice, "Eski Ad", start.AddHours(-10), duration: 40);
        await AddAsync(user, profile, twice, "Yeni Ad", start.AddHours(100), duration: 75);
        await AddAsync(user, other, Guid.CreateVersion7(), "Başka profil", start.AddHours(200));

        var first = await ListAsync(client, profile, 0, ct);
        var items = first.GetProperty("items").EnumerateArray().ToList();
        items.Count.ShouldBe(50);
        first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        items[0].GetProperty("title").GetString().ShouldBe("Video 49");
        items[0].GetProperty("durationSeconds").GetInt32().ShouldBe(60);
        items[0].GetProperty("versionCount").GetInt32().ShouldBe(1);
        items[0].GetProperty("producedAt").GetDateTimeOffset().ShouldBe(start.AddHours(49));
        items.ShouldNotContain(i => i.GetProperty("title").GetString() == "Başka profil");

        var second = await ListAsync(client, profile, 50, ct);
        second.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        var last = second.GetProperty("items").EnumerateArray().Single();
        last.GetProperty("id").GetGuid().ShouldBe(twice);
        last.GetProperty("title").GetString().ShouldBe("Yeni Ad");
        last.GetProperty("durationSeconds").GetInt32().ShouldBe(75);
        last.GetProperty("versionCount").GetInt32().ShouldBe(2);
        last.GetProperty("producedAt").GetDateTimeOffset().ShouldBe(start.AddHours(-10));

        (await ListAsync(client, other, 0, ct)).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Başka profil"]);
        var empty = await ProfileAsync(client, ct);
        (await ListAsync(client, empty, 0, ct)).GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    /// <summary>
    /// AC-4, AC-9, AC-10: ayrıntı tüm sürümleri eskiden yeniye numaralar; durum "completed" (Tamamlandı); değişiklik özeti ("İlk sürüm", farklar);
    /// sürüm maliyeti ve toplam; modeller katalog adıyla, kapalı olan <c>null</c>; altyazı/çubuk, süre, biçim, başlangıç/bitiş, AI ses beyanı;
    /// her sürüm kendi konsept metnini (sahne başlığı + seslendirme) gösterir; HTML içeren LLM metni olduğu gibi (düz metin) döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Produced_Detail_Versions_Cost_ConceptPerVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user) = await SetupAsync(ct);
        var video = Guid.CreateVersion7();
        var at = new DateTimeOffset(2026, 11, 2, 7, 5, 0, TimeSpan.Zero);
        await AddAsync(user, profile, video, "Kuşatma", at, narration: "<script>alert(1)</script> İlk metin", cost: 0.004m, audio: null);
        await AddAsync(user, profile, video, "Kuşatma", at.AddDays(1), narration: "İkinci metin", cost: 1.236m, audio: "local/supertonic-3", notice: true, format: "portrait");

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct);
        detail.GetProperty("title").GetString().ShouldBe("Kuşatma");
        detail.GetProperty("totalCostUsd").GetDecimal().ShouldBe(1.24m);
        var versions = detail.GetProperty("versions").EnumerateArray().ToList();
        versions.Select(v => v.GetProperty("number").GetInt32()).ShouldBe([1, 2]);
        versions.ShouldAllBe(v => v.GetProperty("status").GetString() == "completed");
        versions[0].GetProperty("changes").GetString().ShouldBe("İlk sürüm");
        versions[1].GetProperty("changes").GetString().ShouldBe("Değişti: konsept metni, ses motoru, altyazı, ilerleme çubuğu, biçim");
        versions[0].GetProperty("costUsd").GetDecimal().ShouldBe(0.004m);
        versions[0].GetProperty("audioModel").ValueKind.ShouldBe(JsonValueKind.Null);
        versions[0].GetProperty("imageModel").ValueKind.ShouldBe(JsonValueKind.Null);
        versions[0].GetProperty("codeModel").GetString().ShouldNotBe("anthropic/claude-sonnet-4-5"); // Katalog adı.
        versions[0].GetProperty("codeModel").GetString().ShouldNotBeNullOrWhiteSpace();
        versions[0].GetProperty("engine").GetString().ShouldNotBeNullOrWhiteSpace();
        versions[1].GetProperty("audioModel").GetString().ShouldNotBeNullOrWhiteSpace();
        versions[1].GetProperty("aiVoiceNotice").GetBoolean().ShouldBeTrue();
        versions[1].GetProperty("format").GetString().ShouldBe("portrait");
        versions[1].GetProperty("subtitles").GetBoolean().ShouldBeTrue();
        versions[1].GetProperty("progressBar").GetBoolean().ShouldBeFalse();
        versions[1].GetProperty("durationSeconds").GetInt32().ShouldBe(60);
        versions[1].GetProperty("startedAt").GetDateTimeOffset().ShouldBe(at.AddDays(1).AddMinutes(-4));
        versions[1].GetProperty("endedAt").GetDateTimeOffset().ShouldBe(at.AddDays(1));
        var firstScenes = versions[0].GetProperty("scenes").EnumerateArray().ToList();
        firstScenes.Select(s => s.GetProperty("title").GetString()).ShouldBe(["Açılış", "Son"]);
        firstScenes[0].GetProperty("narration").GetString().ShouldBe("<script>alert(1)</script> İlk metin");
        versions[1].GetProperty("scenes")[0].GetProperty("narration").GetString().ShouldBe("İkinci metin");
        versions[1].GetProperty("conceptTitle").GetString().ShouldBe("Kuşatma");

        (await client.GetAsync(new Uri($"/api/produced/{Guid.NewGuid()}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// AC-7, AC-8: Klasörde aç ve Sistem oynatıcısında aç yalnızca sahibin ajanına dosya adı ve eylemle gider; dosya yoksa "Video dosyası
    /// bulunamadı"; ajan 10 sn içinde cevap vermezse "Zaman aşımı" (504); ajan yokken "Videoyu izlemek için ajan bağlı olmalı"; geçersiz eylem 400.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Produced_FileActions_Owner_Missing_Offline_Timeout()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (client, profile, user) = await SetupAsync(ct);
        var conn = await RegisterAgentAsync(user, ct);
        var version = await AddAsync(user, profile, Guid.CreateVersion7(), "Açılacak", DateTimeOffset.UtcNow);
        var fileName = _factory.Videos.Rows.Single(v => v.Id == version).FileName;

        (await OpenAsync(client, version, "folder", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await OpenAsync(client, version, "player", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var sent = _factory.Agent.Sent.Where(s => s.Method == "OpenFile").ToList();
        sent.Count.ShouldBe(2);
        sent.ShouldAllBe(s => s.ConnectionId == conn && s.Payload.Contains(fileName, StringComparison.Ordinal));
        sent[0].Payload.ShouldContain("\"action\":\"folder\"");
        sent[1].Payload.ShouldContain("\"action\":\"player\"");
        (await OpenAsync(client, version, "delete", ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        _factory.Agent.FileAction = (_, _) => Task.FromResult(new AgentFileActionReply(false, AgentFileActionReply.NotFound));
        var missing = await OpenAsync(client, version, "folder", ct);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await missing.Content.ReadAsStringAsync(ct)).ShouldContain("Video dosyası bulunamadı");
        _factory.Videos.Rows.ShouldContain(v => v.Id == version); // Kayıt kalır, otomatik temizleme yok (AC-7).

        _factory.Agent.FileAction = async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new AgentFileActionReply(true, null);
        };
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var timeout = await OpenAsync(client, version, "player", ct);
        watch.Stop();
        timeout.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        (await timeout.Content.ReadAsStringAsync(ct)).ShouldContain("Zaman aşımı");
        watch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(9.5));
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(20));

        _factory.Agent.FileAction = FakeAgentChannel.DefaultFileAction;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>().DisconnectAsync(conn, CancellationToken.None);
        }

        var offline = await OpenAsync(client, version, "folder", ct);
        offline.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await offline.Content.ReadAsStringAsync(ct)).ShouldContain("Videoyu izlemek için ajan bağlı olmalı");
    }

    /// <summary>
    /// AC-11: başka kullanıcının videosu için liste (profil), ayrıntı, akış ve dosya eylemi "bulunamadı" döner ve ajana hiçbir komut gitmez
    /// (sahibin ajanı bağlıyken bile); oturumsuz istek 401.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Produced_OtherUser_NotFound_NoAgentCommand()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Agent.Reset();
        var (_, profile, owner) = await SetupAsync(ct);
        var video = Guid.CreateVersion7();
        var version = await AddAsync(owner, profile, video, "Gizli", DateTimeOffset.UtcNow);
        var (intruder, _, intruderId) = await SetupAsync(ct);
        await RegisterAgentAsync(owner, ct);
        await RegisterAgentAsync(intruderId, ct);

        (await intruder.GetAsync(new Uri($"/api/produced?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.GetAsync(new Uri($"/api/produced/{video}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await OpenAsync(intruder, version, "folder", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await OpenAsync(intruder, version, "player", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.PostAsJsonAsync($"/api/production/videos/{version}/stream", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _factory.Agent.Sent.ShouldNotContain(s => s.Method == "OpenFile" || s.Method == "OpenStream");

        using var anonymous = _factory.CreateClient();
        (await anonymous.GetAsync(new Uri($"/api/produced?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync($"/api/produced/versions/{version}/open", new { action = "folder" }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static Task<HttpResponseMessage> OpenAsync(HttpClient client, Guid version, string action, CancellationToken ct) =>
        client.PostAsJsonAsync($"/api/produced/versions/{version}/open", new { action }, ct);

    private static Task<JsonElement> ListAsync(HttpClient client, Guid profile, int skip, CancellationToken ct) =>
        client.GetFromJsonAsync<JsonElement>($"/api/produced?profileId={profile}&skip={skip}", ct);

    private static async Task<Guid> ProfileAsync(HttpClient client, CancellationToken ct)
    {
        using var created = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body("Üretilen " + Guid.NewGuid().ToString("N")[..6]), ct);
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
        var conn = "produced-" + Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>()
            .RegisterAsync(conn, new AgentOwner(userId, Guid.NewGuid()), new AgentRegistration("Ofis-PC", "1.0.0"), ct);
        return conn;
    }

    private async Task<Guid> AddAsync(
        Guid user,
        Guid profile,
        Guid concept,
        string title,
        DateTimeOffset endedAt,
        int duration = 60,
        string narration = "Bizans'ın son sabahı başlıyor.",
        decimal cost = 0m,
        string? audio = null,
        bool notice = false,
        string format = "landscape")
    {
        var id = Guid.CreateVersion7();
        var text = JsonSerializer.Serialize(
            new
            {
                id = concept,
                profileId = profile,
                title,
                language = "tr",
                format,
                scenes = new[]
                {
                    new { title = "Açılış", visual = "Surlar görünür.", narration, durationSeconds = 30 },
                    new { title = "Son", visual = "Şehir sessizleşir.", narration = "Bir çağ kapanıyor.", durationSeconds = 30 },
                },
            });
        await _factory.Videos.AddAsync(
            new ProducedVideo
            {
                Id = id,
                UserId = user,
                ProfileId = profile,
                ConceptId = concept,
                JobId = Guid.CreateVersion7(),
                Title = title,
                ConceptText = text,
                CodeModel = "anthropic/claude-sonnet-4-5",
                AudioModel = audio,
                Engine = "local/hyperframes",
                Subtitles = audio is not null,
                ProgressBar = audio is null,
                AiVoiceNotice = notice,
                FileName = "video-" + id.ToString("N") + ".mp4",
                StartedAt = endedAt.AddMinutes(-4),
                EndedAt = endedAt,
                DurationSeconds = duration,
                Format = format,
                CostUsd = cost,
            },
            CancellationToken.None);
        return id;
    }
}
