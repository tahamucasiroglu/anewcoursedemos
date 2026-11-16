using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Infrastructure.Catalog;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Konseptten Video uçtan uca (spec 0006): gerçek Production, Concepts, AiGateway ve Identity servisleri; bellek depoları, sahte sağlayıcı
/// (gerçek LLM çağrısı yok; bu spec hiç çağrı yapmaz). Her test kendi kullanıcısıyla çalışır.
/// </summary>
public sealed class ProductionApiTests : IClassFixture<ProductionApiFactory>
{
    private const string Sonnet = "anthropic/claude-sonnet-4-5";
    private const string Opus = "anthropic/claude-opus-4-5";

    private static readonly ConceptScene[] Scenes =
    [
        new("Açılış", "Surlar görünür.", "Bizans'ın son sabahı başlıyor.", 30),
        new("Son", "Şehir sessizleşir.", "Bir çağ kapanıyor.", 30),
    ];

    private readonly ProductionApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ProductionApiTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ProductionApiTests(ProductionApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-1, AC-3: motor listesi yalnızca HyperFrames (varsayılan) — Remotion ajan kurulu bildirmedikçe yok; görsel listesi Z-Image-Turbo,
    /// FLUX.2 klein 4B, gpt-image; ses listesi Supertonic 3, Chatterbox; video kodu listesi yalnızca online metin modelleri; her seçenekte
    /// yerel/online bilgisi. Ajan Remotion'u bildirince "Lisans uyarısı" etiketiyle listelenir ama seçili gelmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Settings_Models_EnginesAndRemotionByComponent()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);

        var state = await StateAsync(client, profile, ct);

        Ids(state, "video_engine").ShouldBe(["local/hyperframes"]);
        Ids(state, "image").Order(StringComparer.Ordinal).ShouldBe(["local/flux2-klein-4b", "local/z-image-turbo", "openai/gpt-image-1"]);
        Ids(state, "audio").Order(StringComparer.Ordinal).ShouldBe(["local/chatterbox-multilingual-v3", "local/supertonic-3"]);
        Ids(state, "text").ShouldNotBeEmpty();
        Models(state).Where(m => m.GetProperty("kind").GetString() == "text").ShouldAllBe(m => !m.GetProperty("local").GetBoolean());
        Models(state).Single(m => m.GetProperty("id").GetString() == "openai/gpt-image-1").GetProperty("local").GetBoolean().ShouldBeFalse();
        Models(state).Single(m => m.GetProperty("id").GetString() == "local/supertonic-3").GetProperty("local").GetBoolean().ShouldBeTrue();
        Models(state).ShouldAllBe(m => !m.GetProperty("name").GetString()!.Contains("Revideo") && !m.GetProperty("name").GetString()!.Contains("Motion Canvas"));
        state.GetProperty("settings").GetProperty("engine").GetString().ShouldBe("local/hyperframes");

        await RegisterAgentAsync(UserOf(profile), ct, "remotion");
        var after = await StateAsync(client, profile, ct);

        Ids(after, "video_engine").ShouldBe(["local/hyperframes", "local/remotion"]);
        Models(after).Single(m => m.GetProperty("id").GetString() == "local/remotion").GetProperty("licenseWarning").GetBoolean().ShouldBeTrue();
        after.GetProperty("settings").GetProperty("engine").GetString().ShouldBe("local/hyperframes");
        using var saved = await SaveAsync(client, profile, Settings(engine: "local/remotion"), ct);
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StateAsync(client, profile, ct)).GetProperty("settings").GetProperty("engine").GetString().ShouldBe("local/remotion");
    }

    /// <summary>
    /// AC-3, AC-24: ajan hiç bağlanmadıysa yeni profilde görsel "Kapalı — koddan çiz", ses Supertonic 3; ajan Z-Image-Turbo'yu bildirince yeni
    /// profilin görseli Z-Image-Turbo. Bildirim kullanıcı başına saklanır: başka kullanıcının ajanının bildirimi bu kullanıcıyı etkilemez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task InstalledComponents_StoredPerUser_DriveRemotionAndDefault()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        using var other = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        var otherProfile = await ProfileAsync(other, ct);

        var fresh = (await StateAsync(client, profile, ct)).GetProperty("settings");
        fresh.GetProperty("imageModel").ValueKind.ShouldBe(JsonValueKind.Null);
        fresh.GetProperty("audioModel").GetString().ShouldBe("local/supertonic-3");
        fresh.GetProperty("codeModel").ValueKind.ShouldBe(JsonValueKind.Null);

        await RegisterAgentAsync(UserOf(otherProfile), ct, "z-image-turbo", "remotion");

        (await StateAsync(client, profile, ct)).GetProperty("settings").GetProperty("imageModel").ValueKind.ShouldBe(JsonValueKind.Null);
        Ids(await StateAsync(client, profile, ct), "video_engine").ShouldBe(["local/hyperframes"]);
        (await StateAsync(other, otherProfile, ct)).GetProperty("settings").GetProperty("imageModel").GetString().ShouldBe("local/z-image-turbo");

        await RegisterAgentAsync(UserOf(profile), ct, "z-image-turbo");
        var newProfile = await ProfileAsync(client, ct, "Yeni Kanal");
        (await StateAsync(client, newProfile, ct)).GetProperty("settings").GetProperty("imageModel").GetString().ShouldBe("local/z-image-turbo");
    }

    /// <summary>AC-2: efor yalnızca modelin desteklediği seviyelerden; desteklenmeyen efor modelin varsayılanına, eforsuz modelde boş.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Settings_Effort_FallsBackToModelDefault()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        var opus = Models(await StateAsync(client, profile, ct)).Single(m => m.GetProperty("id").GetString() == Opus);
        opus.GetProperty("effortLevels").EnumerateArray().Select(e => e.GetString()).ShouldBe(["low", "medium", "high"]);

        (await SaveAsync(client, profile, Settings(code: Opus, effort: "low"), ct)).Dispose();
        (await StateAsync(client, profile, ct)).GetProperty("settings").GetProperty("codeEffort").GetString().ShouldBe("low");

        (await SaveAsync(client, profile, Settings(code: Opus, effort: "max"), ct)).Dispose();
        (await StateAsync(client, profile, ct)).GetProperty("settings").GetProperty("codeEffort").GetString().ShouldBe("high");

        (await SaveAsync(client, profile, Settings(code: Sonnet, effort: "low"), ct)).Dispose();
        (await StateAsync(client, profile, ct)).GetProperty("settings").GetProperty("codeEffort").ValueKind.ShouldBe(JsonValueKind.Null);

        using var wrongKind = await SaveAsync(client, profile, Settings(code: "local/supertonic-3"), ct);
        wrongKind.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// AC-4: yetkisiz model seçilemez (istek reddedilir, ayar değişmez) ve listede "no_permission" etiketlidir; key'i olmayan online model
    /// "no_key" etiketlidir ve seçiliyken Sırayla üret pasif ("API key yok — Ayarlar"); yetkisi sonradan kalkan seçili model "Model yetkiniz yok".
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Settings_NoPermission_Rejected_Reasons()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ProductionApiFactory
        {
            UserModelPermissions = "openai/gpt-5-mini,local/hyperframes,local/supertonic-3,local/z-image-turbo",
        };
        using var client = await factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        var user = factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        var concept = SeedIn(factory, user, profile, "Sıradaki", ConceptStatus.Approved);
        (await EnqueueAsync(client, profile, [concept], ct)).Dispose();

        var state = await StateAsync(client, profile, ct);
        Availability(state, Sonnet).ShouldBe("no_permission");
        Availability(state, "openai/gpt-5-mini").ShouldBe("no_key");
        using var denied = await SaveAsync(client, profile, Settings(code: Sonnet), ct);
        denied.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await denied.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Bu modeli kullanma yetkiniz yok");
        (await StateAsync(client, profile, ct)).GetProperty("settings").GetProperty("codeModel").ValueKind.ShouldBe(JsonValueKind.Null);

        (await SaveAsync(client, profile, Settings(code: "openai/gpt-5-mini"), ct)).EnsureSuccessStatusCode();
        var noKey = await StateAsync(client, profile, ct);
        noKey.GetProperty("startBlock").GetString().ShouldBe("no_key");
        noKey.GetProperty("startBlockMessage").GetString().ShouldBe("API key yok — Ayarlar");
        using var refused = await PostAsync(client, "/api/production/start", new { profileId = profile }, ct);
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("API key yok — Ayarlar");

        var stored = factory.ProductionSettings.Rows.Single(s => s.ProfileId == profile);
        await factory.ProductionSettings.UpsertAsync(stored with { CodeModel = Sonnet }, ct); // Yetkisi kalkmış model seçili kaldı.
        var revoked = await StateAsync(client, profile, ct);
        revoked.GetProperty("startBlock").GetString().ShouldBe("no_permission");
        revoked.GetProperty("startBlockMessage").GetString().ShouldBe("Model yetkiniz yok");
        (await SaveAsync(client, profile, Settings(code: Sonnet, progressBar: true), ct)).StatusCode.ShouldBe(HttpStatusCode.OK); // Kayıtlı seçim korunur.
        factory.Jobs.Rows.ShouldAllBe(j => j.Status == ProductionJobStatus.Queued);
    }

    /// <summary>
    /// AC-6, AC-25: dil kontrolü konseptin kaydındaki dille yapılır — İngilizce konsept sıradayken "Yalnız Türkçe" ses modeli uyarı verir ve
    /// Sırayla üret pasif; profil Türkçe yapılsa da sürer; iş çıkarılınca kalkar. Türkçe/dikey/60 sn konsept, profil İngilizce/yatay/120 sn
    /// yapıldıktan sonra başlarsa işe konseptin değerleri sabitlenir ve "Yalnız Türkçe" model uyarı vermez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task LanguageCheck_UsesConceptLanguage_NotProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = JsonNode.Parse(EmbeddedCatalogSource.ReadEmbeddedJson())!;
        seed["models"]!.AsArray().Single(m => (string?)m!["id"] == "local/supertonic-3")!["languageOnly"] = "tr";
        await using var factory = new ProductionApiFactory { Catalog = EmbeddedCatalogSource.Parse(seed.ToJsonString()) };
        using var client = await factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct, language: "en");
        var user = factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        await KeyAsync(client, ct);
        (await SaveAsync(client, profile, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        var english = SeedIn(factory, user, profile, "English one", ConceptStatus.Approved, language: "en");
        (await EnqueueAsync(client, profile, [english], ct)).EnsureSuccessStatusCode();

        var warned = await StateAsync(client, profile, ct);
        warned.GetProperty("languageWarnings").EnumerateArray().Select(w => w.GetString()).ShouldBe(["Seçili ses modeli konsept dilini desteklemiyor"]);
        warned.GetProperty("startBlock").GetString().ShouldBe("audio_language");
        (await client.PutAsJsonAsync($"/api/profiles/{profile}", ProfileApiTests.Body("Dil Kanalı", language: "tr"), ct)).EnsureSuccessStatusCode();
        (await StateAsync(client, profile, ct)).GetProperty("startBlock").GetString().ShouldBe("audio_language");

        (await PostAsync(client, $"/api/production/jobs/{JobOf(factory, english)}/remove", new { }, ct)).EnsureSuccessStatusCode();
        var cleared = await StateAsync(client, profile, ct);
        cleared.GetProperty("languageWarnings").GetArrayLength().ShouldBe(0);
        cleared.GetProperty("startBlock").GetString().ShouldBe("empty");

        // Profil sıraya almadan önce de sonra da İngilizce/yatay: işin değerleri yine konseptin kaydındakilerdir.
        (await client.PutAsJsonAsync($"/api/profiles/{profile}", ProfileApiTests.Body("Dil Kanalı", seconds: 120, language: "en", format: "landscape"), ct))
            .EnsureSuccessStatusCode();
        var turkish = SeedIn(factory, user, profile, "Türkçe konsept", ConceptStatus.Approved, language: "tr", format: "portrait", seconds: 60);
        (await EnqueueAsync(client, profile, [turkish], ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/profiles/{profile}", ProfileApiTests.Body("Dil Kanalı", seconds: 150, language: "en", format: "landscape"), ct))
            .EnsureSuccessStatusCode();
        var ready = await StateAsync(client, profile, ct);
        ready.GetProperty("languageWarnings").GetArrayLength().ShouldBe(0);
        ready.GetProperty("startBlock").ValueKind.ShouldBe(JsonValueKind.Null);

        (await PostAsync(client, "/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        var fix = factory.Jobs.Rows.Single(j => j.ConceptId == turkish).FixedSettings!;
        (fix.Language, fix.Format, fix.TargetDurationSeconds).ShouldBe(("tr", "portrait", 60));
        fix.AudioModel.ShouldBe("local/supertonic-3");
    }

    /// <summary>
    /// AC-7: ayarlar profil başına; yeniden okumada korunur; hiç seçim yapılmamış profilde varsayılanlar. İş başlarken ayar işe sabitlenir;
    /// sonradan değişen ayar o işi değil, sonra başlayan işi etkiler.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Settings_PerProfile_And_FixedOnStart()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var a = await ProfileAsync(client, ct, "Alfa");
        var b = await ProfileAsync(client, ct, "Beta");
        await KeyAsync(client, ct);
        (await SaveAsync(client, a, Settings(code: Sonnet, progressBar: true, subtitles: false), ct)).EnsureSuccessStatusCode();

        var stateA = (await StateAsync(client, a, ct)).GetProperty("settings");
        (stateA.GetProperty("progressBar").GetBoolean(), stateA.GetProperty("subtitles").GetBoolean(), stateA.GetProperty("codeModel").GetString())
            .ShouldBe((true, false, Sonnet));
        var stateB = (await StateAsync(client, b, ct)).GetProperty("settings");
        (stateB.GetProperty("progressBar").GetBoolean(), stateB.GetProperty("subtitles").GetBoolean()).ShouldBe((false, true));
        stateB.GetProperty("codeModel").ValueKind.ShouldBe(JsonValueKind.Null);

        var first = Seed(a, "Birinci", ConceptStatus.Approved);
        var second = Seed(a, "İkinci", ConceptStatus.Approved);
        (await EnqueueAsync(client, a, [first, second], ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/production/start", new { profileId = a }, ct)).EnsureSuccessStatusCode();
        (await SaveAsync(client, a, Settings(code: Sonnet, progressBar: false, subtitles: true), ct)).EnsureSuccessStatusCode();

        _factory.Jobs.Rows.Single(j => j.ConceptId == first).FixedSettings!.ProgressBar.ShouldBeTrue();
        _factory.Jobs.Rows.Single(j => j.ConceptId == first).FixedSettings!.Subtitles.ShouldBeFalse();
        (await PostAsync(client, "/api/production/stop", new { profileId = a }, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, $"/api/production/jobs/{JobOf(_factory, first)}/remove", new { }, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/production/start", new { profileId = a }, ct)).EnsureSuccessStatusCode();
        var later = _factory.Jobs.Rows.Single(j => j.ConceptId == second).FixedSettings!;
        (later.ProgressBar, later.Subtitles, later.CodeModel).ShouldBe((false, true, Sonnet));
    }

    /// <summary>
    /// AC-10: üç konsept görünüş sırasıyla sıranın sonuna eklenir, iş ve konsept durumları Sırada, onaylı listeden kalkar; boş seçim reddedilir;
    /// Onaylandı olmayan (Üretildi, zaten Sırada, Video oldu) ya da silinmiş konsept reddedilir ve hiçbir şey değişmez; aynı konsept iki kez giremez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Enqueue_OrderAndStatus_RejectsInvalid()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        var ids = new[] { Seed(profile, "Üç", ConceptStatus.Approved), Seed(profile, "Bir", ConceptStatus.Approved), Seed(profile, "İki", ConceptStatus.Approved) };

        using var queued = await EnqueueAsync(client, profile, ids, ct);

        queued.StatusCode.ShouldBe(HttpStatusCode.OK);
        var jobs = (await queued.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("jobs").EnumerateArray().ToList();
        jobs.Select(j => j.GetProperty("title").GetString()).ShouldBe(["Üç", "Bir", "İki"]);
        jobs.Select(j => j.GetProperty("number").GetInt32()).ShouldBe([1, 2, 3]);
        jobs.ShouldAllBe(j => j.GetProperty("status").GetString() == "queued");
        _factory.Concepts.Rows.Where(c => ids.Contains(c.Id)).ShouldAllBe(c => c.Status == ConceptStatus.Queued);
        (await client.GetFromJsonAsync<JsonElement>($"/api/concepts?profileId={profile}&statuses=approved", ct)).GetProperty("total").GetInt32().ShouldBe(0);

        (await EnqueueAsync(client, profile, [], ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var approved = Seed(profile, "Onaylı", ConceptStatus.Approved);
        var deleted = Seed(profile, "Silinmiş", ConceptStatus.Approved, deletedAt: DateTimeOffset.UtcNow);
        foreach (var bad in new[] { Seed(profile, "Üretilmiş", ConceptStatus.Generated), ids[0], Seed(profile, "Videolu", ConceptStatus.VideoReady), deleted })
        {
            using var rejected = await EnqueueAsync(client, profile, [approved, bad], ct);
            rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await rejected.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Yalnızca onaylanmış konseptler sıraya alınabilir.");
        }

        _factory.Concepts.Rows.Single(c => c.Id == approved).Status.ShouldBe(ConceptStatus.Approved);
        _factory.Jobs.Rows.Count(j => j.ProfileId == profile).ShouldBe(3);
        _factory.Jobs.Rows.Count(j => j.ConceptId == ids[0]).ShouldBe(1);
    }

    /// <summary>AC-11: Yukarı/Aşağı bir sıra taşır ve yeniden okumada korunur; uçlarda reddedilir; profillerin sıraları ayrıdır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Move_UpDown_PerProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var a = await ProfileAsync(client, ct, "Alfa");
        var b = await ProfileAsync(client, ct, "Beta");
        var ids = new[] { Seed(a, "A1", ConceptStatus.Approved), Seed(a, "A2", ConceptStatus.Approved), Seed(a, "A3", ConceptStatus.Approved) };
        (await EnqueueAsync(client, a, ids, ct)).EnsureSuccessStatusCode();
        (await EnqueueAsync(client, b, [Seed(b, "B1", ConceptStatus.Approved), Seed(b, "B2", ConceptStatus.Approved)], ct)).EnsureSuccessStatusCode();

        (await MoveAsync(client, JobOf(_factory, ids[2]), "up", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        Titles(await StateAsync(client, a, ct)).ShouldBe(["A1", "A3", "A2"]);
        (await MoveAsync(client, JobOf(_factory, ids[0]), "down", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        Titles(await StateAsync(client, a, ct)).ShouldBe(["A3", "A1", "A2"]);

        (await MoveAsync(client, JobOf(_factory, ids[2]), "up", ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await MoveAsync(client, JobOf(_factory, ids[1]), "down", ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        Titles(await StateAsync(client, a, ct)).ShouldBe(["A3", "A1", "A2"]);
        Titles(await StateAsync(client, b, ct)).ShouldBe(["B1", "B2"]);
    }

    /// <summary>AC-12: × ile çıkarılan Sırada, Durduruldu ve Hata işinin konsepti Onaylandı'ya döner; diğerlerinin göreli sırası değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Remove_ReturnsConceptToApproved()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        await KeyAsync(client, ct);
        (await SaveAsync(client, profile, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        var ids = new[] { Seed(profile, "R1", ConceptStatus.Approved), Seed(profile, "R2", ConceptStatus.Approved), Seed(profile, "R3", ConceptStatus.Approved), Seed(profile, "R4", ConceptStatus.Approved) };
        (await EnqueueAsync(client, profile, ids, ct)).EnsureSuccessStatusCode();

        (await PostAsync(client, $"/api/production/jobs/{JobOf(_factory, ids[1])}/remove", new { }, ct)).EnsureSuccessStatusCode();
        Titles(await StateAsync(client, profile, ct)).ShouldBe(["R1", "R3", "R4"]);
        _factory.Concepts.Rows.Single(c => c.Id == ids[1]).Status.ShouldBe(ConceptStatus.Approved);

        (await PostAsync(client, "/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        _factory.Concepts.Rows.Single(c => c.Id == ids[0]).Status.ShouldBe(ConceptStatus.Producing);
        (await PostAsync(client, "/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, $"/api/production/jobs/{JobOf(_factory, ids[0])}/remove", new { }, ct)).EnsureSuccessStatusCode();
        _factory.Concepts.Rows.Single(c => c.Id == ids[0]).Status.ShouldBe(ConceptStatus.Approved);

        _factory.Jobs.SetStatus(JobOf(_factory, ids[3]), ProductionJobStatus.Failed);
        (await PostAsync(client, $"/api/production/jobs/{JobOf(_factory, ids[3])}/remove", new { }, ct)).EnsureSuccessStatusCode();
        Titles(await StateAsync(client, profile, ct)).ShouldBe(["R3"]);
        _factory.Concepts.Rows.Single(c => c.Id == ids[3]).Status.ShouldBe(ConceptStatus.Approved);
    }

    /// <summary>AC-13: Üretiliyor ya da Ajan bekleniyor işin taşınması/çıkarılması API'de reddedilir ve sıra değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Running_CannotMoveOrRemove()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        await KeyAsync(client, ct);
        (await SaveAsync(client, profile, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        var ids = new[] { Seed(profile, "K1", ConceptStatus.Approved), Seed(profile, "K2", ConceptStatus.Approved) };
        (await EnqueueAsync(client, profile, ids, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        var running = JobOf(_factory, ids[0]);

        (await MoveAsync(client, running, "down", ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await MoveAsync(client, JobOf(_factory, ids[1]), "up", ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        using var removed = await PostAsync(client, $"/api/production/jobs/{running}/remove", new { }, ct);
        removed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await removed.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Üretimdeki iş taşınamaz ve çıkarılamaz; önce durdurun.");
        _factory.Jobs.SetStatus(running, ProductionJobStatus.WaitingForAgent);
        (await PostAsync(client, $"/api/production/jobs/{running}/remove", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        Titles(await StateAsync(client, profile, ct)).ShouldBe(["K1", "K2"]);
        _factory.Concepts.Rows.Single(c => c.Id == ids[0]).Status.ShouldBe(ConceptStatus.Producing);
    }

    /// <summary>AC-14: sıradaki işin konsepti düzenlenemez ve silinemez (API reddeder, konsept değişmez); sıradan çıkarılınca yeniden açılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Queued_ConceptEditDelete_Rejected_ThenAllowed()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        var id = Seed(profile, "Kilitli", ConceptStatus.Approved);
        (await EnqueueAsync(client, profile, [id], ct)).EnsureSuccessStatusCode();
        var edited = new object[]
        {
            new { title = "Açılış", visual = "Surlar.", narration = "Yeni anlatım.", durationSeconds = 30 },
            new { title = "Son", visual = "Sessizlik.", narration = "Bitti.", durationSeconds = 30 },
        };

        using var save = await PostAsync(client, $"/api/concepts/{id}/scenes", new { scenes = edited }, ct);
        save.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await save.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Önce üretim sırasından çıkarın.");
        (await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _factory.Concepts.Rows.Single(c => c.Id == id).Scenes.ShouldBe(Scenes);
        _factory.Concepts.Rows.Single(c => c.Id == id).DeletedAt.ShouldBeNull();

        (await PostAsync(client, $"/api/production/jobs/{JobOf(_factory, id)}/remove", new { }, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, $"/api/concepts/{id}/scenes", new { scenes = edited }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// AC-17: Sırayla üret en üstteki Sırada işi (ve konseptini) Üretiliyor yapar; kullanıcının hiçbir zaman iki çalışan işi yoktur; bu profilde
    /// "Üretim sürüyor", diğerinde "Başka bir profilde üretim sürüyor"; görünen durumlar yalnızca tek listedendir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_OneRunningPerUser_OtherProfileBlocked()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var a = await ProfileAsync(client, ct, "Alfa");
        var b = await ProfileAsync(client, ct, "Beta");
        await KeyAsync(client, ct);
        foreach (var p in new[] { a, b })
        {
            (await SaveAsync(client, p, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        }

        var a1 = Seed(a, "A1", ConceptStatus.Approved);
        var a2 = Seed(a, "A2", ConceptStatus.Approved);
        (await EnqueueAsync(client, a, [a1, a2], ct)).EnsureSuccessStatusCode();
        (await EnqueueAsync(client, b, [Seed(b, "B1", ConceptStatus.Approved)], ct)).EnsureSuccessStatusCode();

        using var started = await PostAsync(client, "/api/production/start", new { profileId = a }, ct);

        started.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stateA = await started.Content.ReadFromJsonAsync<JsonElement>(ct);
        stateA.GetProperty("jobs").EnumerateArray().Select(j => j.GetProperty("status").GetString()).ShouldBe(["producing", "queued"]);
        stateA.GetProperty("startBlock").GetString().ShouldBe("running_here");
        stateA.GetProperty("startBlockMessage").GetString().ShouldBe("Üretim sürüyor");
        _factory.Concepts.Rows.Single(c => c.Id == a1).Status.ShouldBe(ConceptStatus.Producing);
        _factory.Concepts.Rows.Single(c => c.Id == a2).Status.ShouldBe(ConceptStatus.Queued);
        var stateB = await StateAsync(client, b, ct);
        stateB.GetProperty("startBlockMessage").GetString().ShouldBe("Başka bir profilde üretim sürüyor");
        (await PostAsync(client, "/api/production/start", new { profileId = b }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await PostAsync(client, "/api/production/start", new { profileId = a }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var user = UserOf(a);
        _factory.Jobs.Rows.Count(j => j.UserId == user && j.Status == ProductionJobStatus.Producing).ShouldBe(1);
        var allowed = new[] { "queued", "producing", "stopped", "waiting_for_agent", "failed", "completed" };
        stateA.GetProperty("jobs").EnumerateArray().Concat(stateB.GetProperty("jobs").EnumerateArray())
            .ShouldAllBe(j => allowed.Contains(j.GetProperty("status").GetString()));
    }

    /// <summary>AC-17 (0003 Req 11): üst barda profil değiştirmek çalışan ya da sıradaki işlerin durumunu ve sırasını değiştirmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_ProfileSwitch_DoesNotAffectJobs()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var a = await ProfileAsync(client, ct, "Alfa");
        var b = await ProfileAsync(client, ct, "Beta");
        await KeyAsync(client, ct);
        (await SaveAsync(client, a, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        (await EnqueueAsync(client, a, [Seed(a, "A1", ConceptStatus.Approved), Seed(a, "A2", ConceptStatus.Approved)], ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/production/start", new { profileId = a }, ct)).EnsureSuccessStatusCode();
        var before = (await StateAsync(client, a, ct)).GetProperty("jobs").GetRawText();

        (await client.PutAsJsonAsync("/api/profiles/selected", new { profileId = b }, ct)).EnsureSuccessStatusCode();

        (await StateAsync(client, a, ct)).GetProperty("jobs").GetRawText().ShouldBe(before);
    }

    /// <summary>AC-18: Durdur → Durduruldu ve sıradaki hiçbir iş başlamaz; devam → aynı iş Üretiliyor; Hata durumundaki iş de devam eder.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stop_Resume_Failed_Resume()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(client, ct);
        await KeyAsync(client, ct);
        (await SaveAsync(client, profile, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        var ids = new[] { Seed(profile, "S1", ConceptStatus.Approved), Seed(profile, "S2", ConceptStatus.Approved) };
        (await EnqueueAsync(client, profile, ids, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();

        using var stopped = await PostAsync(client, "/api/production/stop", new { profileId = profile }, ct);
        Statuses(await stopped.Content.ReadFromJsonAsync<JsonElement>(ct)).ShouldBe(["stopped", "queued"]);
        Statuses(await StateAsync(client, profile, ct)).ShouldBe(["stopped", "queued"]);
        (await PostAsync(client, "/api/production/stop", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var job = JobOf(_factory, ids[0]);
        var fixedAt = _factory.Jobs.Rows.Single(j => j.Id == job).FixedSettings!.FixedAt;
        using var resumed = await PostAsync(client, $"/api/production/jobs/{job}/resume", new { }, ct);
        Statuses(await resumed.Content.ReadFromJsonAsync<JsonElement>(ct)).ShouldBe(["producing", "queued"]);
        _factory.Jobs.Rows.Single(j => j.Id == job).FixedSettings!.FixedAt.ShouldBe(fixedAt);
        (await PostAsync(client, $"/api/production/jobs/{JobOf(_factory, ids[1])}/resume", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        _factory.Jobs.SetStatus(job, ProductionJobStatus.Failed);
        (await PostAsync(client, $"/api/production/jobs/{job}/resume", new { }, ct)).EnsureSuccessStatusCode();
        Statuses(await StateAsync(client, profile, ct)).ShouldBe(["producing", "queued"]);
    }

    /// <summary>AC-19, AC-26 (çıkış): iş Üretiliyor iken çıkış → Durduruldu, Sırada işler başlamaz; yeniden girişte aynı durum ve devam çalışır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_StopsRunningJob()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var profile = await ProfileAsync(client, ct);
        await KeyAsync(client, ct);
        (await SaveAsync(client, profile, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        var ids = new[] { Seed(profile, "L1", ConceptStatus.Approved), Seed(profile, "L2", ConceptStatus.Approved) };
        (await EnqueueAsync(client, profile, ids, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();

        (await client.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct)).EnsureSuccessStatusCode();

        _factory.Jobs.Rows.Where(j => j.ProfileId == profile).OrderBy(j => j.Position).Select(j => j.Status)
            .ShouldBe([ProductionJobStatus.Stopped, ProductionJobStatus.Queued]);
        using var again = _factory.CreateClient();
        var session = await AuthenticatedClient.LoginAsync(again, _factory.Services, email, AuthenticatedClient.Password, ct);
        again.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        Statuses(await StateAsync(again, profile, ct)).ShouldBe(["stopped", "queued"]);
        (await PostAsync(again, $"/api/production/jobs/{JobOf(_factory, ids[0])}/resume", new { }, ct)).EnsureSuccessStatusCode();
        Statuses(await StateAsync(again, profile, ct)).ShouldBe(["producing", "queued"]);
    }

    /// <summary>AC-20: başka kullanıcının profiline/işine/konseptine yönelik tüm istekler reddedilir ve hiçbir veri değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Production_OtherUser_Rejected_Unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var owner = await _factory.CreateUserClientAsync(ct);
        using var intruder = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileAsync(owner, ct);
        var own = await ProfileAsync(intruder, ct, "Davetsiz");
        await KeyAsync(owner, ct);
        (await SaveAsync(owner, profile, Settings(code: Sonnet), ct)).EnsureSuccessStatusCode();
        var queuedConcept = Seed(profile, "Sahipli", ConceptStatus.Approved);
        var approved = Seed(profile, "Onaylı", ConceptStatus.Approved);
        (await EnqueueAsync(owner, profile, [queuedConcept], ct)).EnsureSuccessStatusCode();
        var job = JobOf(_factory, queuedConcept);
        var before = (await StateAsync(owner, profile, ct)).GetRawText();

        (await intruder.GetAsync(new Uri($"/api/production/state?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SaveAsync(intruder, profile, Settings(progressBar: true), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await EnqueueAsync(intruder, profile, [approved], ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await EnqueueAsync(intruder, own, [approved], ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await MoveAsync(intruder, job, "down", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, $"/api/production/jobs/{job}/remove", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, $"/api/production/jobs/{job}/resume", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/production/start", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/production/stop", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await StateAsync(owner, profile, ct)).GetRawText().ShouldBe(before);
        _factory.Concepts.Rows.Single(c => c.Id == approved).Status.ShouldBe(ConceptStatus.Approved);
        (await StateAsync(intruder, own, ct)).GetProperty("jobs").GetArrayLength().ShouldBe(0);
    }

    /// <summary>AC-22 (Api): oturumsuz istek 401.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Production_Anonymous_Unauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = _factory.CreateClient();

        (await anonymous.GetAsync(new Uri($"/api/production/state?profileId={Guid.NewGuid()}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/production/start", new { profileId = Guid.NewGuid() }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static object Settings(
        string? code = null, string? effort = null, string engine = "local/hyperframes", string? image = null, string? audio = "local/supertonic-3", bool progressBar = false, bool subtitles = true) =>
        new { subtitles, progressBar, codeModel = code, codeEffort = effort, engine, imageModel = image, audioModel = audio };

    private static IEnumerable<JsonElement> Models(JsonElement state) => state.GetProperty("models").EnumerateArray();

    private static List<string> Ids(JsonElement state, string kind) =>
        [.. Models(state).Where(m => m.GetProperty("kind").GetString() == kind).Select(m => m.GetProperty("id").GetString()!)];

    private static string? Availability(JsonElement state, string id) =>
        Models(state).Single(m => m.GetProperty("id").GetString() == id).GetProperty("availability").GetString();

    private static List<string?> Titles(JsonElement state) => [.. state.GetProperty("jobs").EnumerateArray().Select(j => j.GetProperty("title").GetString())];

    private static List<string?> Statuses(JsonElement state) => [.. state.GetProperty("jobs").EnumerateArray().Select(j => j.GetProperty("status").GetString())];

    private static Guid JobOf(ProductionApiFactory factory, Guid concept) => factory.Jobs.Rows.Single(j => j.ConceptId == concept).Id;

    private static async Task<Guid> ProfileAsync(HttpClient client, CancellationToken ct, string name = "Üretim Kanalı", string language = "tr")
    {
        using var created = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body(name, language: language), ct);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
    }

    private static async Task KeyAsync(HttpClient client, CancellationToken ct) =>
        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = "sk-ant-test-" + Guid.NewGuid().ToString("N") }, ct)).EnsureSuccessStatusCode();

    private static async Task<JsonElement> StateAsync(HttpClient client, Guid profile, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/production/state?profileId={profile}", ct);

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, Guid profile, object body, CancellationToken ct) =>
        client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", body, ct);

    private static Task<HttpResponseMessage> EnqueueAsync(HttpClient client, Guid profile, IReadOnlyList<Guid> ids, CancellationToken ct) =>
        client.PostAsJsonAsync("/api/production/queue", new { profileId = profile, conceptIds = ids }, ct);

    private static Task<HttpResponseMessage> MoveAsync(HttpClient client, Guid job, string direction, CancellationToken ct) =>
        client.PostAsJsonAsync($"/api/production/jobs/{job}/move", new { direction }, ct);

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object body, CancellationToken ct) =>
        client.PostAsJsonAsync(path, body, ct);

    private static Guid SeedIn(
        ProductionApiFactory factory,
        Guid userId,
        Guid profile,
        string title,
        ConceptStatus status,
        DateTimeOffset? deletedAt = null,
        string language = "tr",
        string format = "portrait",
        int seconds = 60)
    {
        var id = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow.AddHours(-1);
        factory.Concepts.AddAsync(
            new Concept
            {
                Id = id,
                UserId = userId,
                ProfileId = profile,
                Title = title,
                TitleKey = TitleKey.Of(title, language),
                Source = ConceptSource.Idea,
                Status = status,
                Language = language,
                Format = format,
                TargetDurationSeconds = seconds,
                Scenes = Scenes,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = deletedAt,
            },
            CancellationToken.None).GetAwaiter().GetResult();
        return id;
    }

    private Guid UserOf(Guid profile) => _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;

    private Guid Seed(Guid profile, string title, ConceptStatus status, DateTimeOffset? deletedAt = null) =>
        SeedIn(_factory, UserOf(profile), profile, title, status, deletedAt);

    private async Task RegisterAgentAsync(Guid userId, CancellationToken ct, params string[] components)
    {
        // Hub'ın Register metodunun çağırdığı servis (bağlantı kimliği hub'dan, sahip ajan oturumundan).
        await using var scope = _factory.Services.CreateAsyncScope();
        var presence = scope.ServiceProvider.GetRequiredService<IAgentPresenceService>();
        await presence.RegisterAsync("test-" + Guid.NewGuid().ToString("N"), new AgentOwner(userId, Guid.NewGuid()), new AgentRegistration("PC", "1.0.0", components), ct);
    }
}
