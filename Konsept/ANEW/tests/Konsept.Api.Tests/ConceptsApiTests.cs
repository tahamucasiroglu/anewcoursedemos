using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Shouldly;
using static Konsept.Api.Tests.Support.ConceptTestKit;

namespace Konsept.Api.Tests;

/// <summary>
/// Konseptler Api yüzeyi (spec 0014 AC-1, AC-2, AC-9, AC-24, AC-25, AC-27, AC-28, AC-30, AC-31): hazır olma, giriş doğrulaması
/// (reddedilen istekte çağrı yok), Üretilenler listesi, detay, onay, sahiplik ve profil silme kuralı. Bellek depoları, sahte sağlayıcı.
/// </summary>
public sealed class ConceptsApiTests : IClassFixture<ConceptsApiFactory>
{
    private readonly ConceptsApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConceptsApiTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ConceptsApiTests(ConceptsApiFactory factory)
    {
        _factory = factory;
        _factory.Provider.Reset();
    }

    /// <summary>
    /// AC-1: rol için model seçilmemişse ya da sağlayıcının key'i yoksa ilgili üretim hazır değildir; düğmeyi atlayan doğrudan istek
    /// sağlayıcıya çağrı yapılmadan reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Readiness_MissingRoleOrKey_ButtonsOff_DirectRequestRejected_NoCall()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        await SelectModelsAsync(client, profile, ct, ConceptModel, null, null);

        var readiness = (await WorkAsync(client, profile, ct)).GetProperty("readiness");
        readiness.GetProperty("topics").GetBoolean().ShouldBeFalse();
        readiness.GetProperty("fromTopics").GetBoolean().ShouldBeTrue();
        readiness.GetProperty("idea").GetBoolean().ShouldBeFalse();
        readiness.GetProperty("missingRoles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["dedupe", "topic"]);

        using (var topics = await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 3 }, ct))
        {
            topics.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await topics.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString().ShouldBe("concepts.not_ready");
        }

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        await SelectModelsAsync(client, profile, ct, ConceptModel, DedupeModel, TopicModel);
        (await client.DeleteAsync(new Uri("/api/ai/keys/anthropic", UriKind.Relative), ct)).EnsureSuccessStatusCode();
        var noKey = (await WorkAsync(client, profile, ct)).GetProperty("readiness");
        noKey.GetProperty("topics").GetBoolean().ShouldBeFalse();
        noKey.GetProperty("fromTopics").GetBoolean().ShouldBeFalse();
        noKey.GetProperty("idea").GetBoolean().ShouldBeFalse();
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _factory.Provider.Calls.Count.ShouldBe(0);
    }

    /// <summary>AC-2: adet 1 ve 100 kabul; 0, 101, boş, ondalık, metin reddedilir (alan hatası "Count"); reddedilen istekte çağrı yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Count_Validation_RejectsWithoutCall()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);

        foreach (var count in new object?[] { 0, 101, null, 1.5, "abc", "5", -1 })
        {
            using var response = await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count }, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"{count}");
            var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("errors");
            HasField(errors, "Count").ShouldBeTrue($"{count}");
        }

        _factory.Provider.Calls.Count.ShouldBe(0);

        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Ok(TopicsJson(("Konu", "Özet."))) : Duplicates();
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 100 }, ct)).EnsureSuccessStatusCode();
        (await WaitJobAsync(client, profile, "topicJob", ct)).GetProperty("total").GetInt32().ShouldBe(100);
    }

    /// <summary>AC-9: boş, yalnızca boşluk ya da 2.000 karakteri aşan fikir ve geçersiz adet reddedilir (çağrı yok); 2.000 karakter kabul edilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Idea_Validation_RejectsWithoutCall()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);

        foreach (var (idea, count, field) in new (string?, object?, string)[] { (string.Empty, 1, "Idea"), ("   ", 1, "Idea"), (new string('a', 2001), 1, "Idea"), (null, 1, "Idea"), ("Fikir", 0, "Count"), ("Fikir", 1.5, "Count") })
        {
            using var response = await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea, count }, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            HasField((await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("errors"), field).ShouldBeTrue(field);
        }

        _factory.Provider.Calls.Count.ShouldBe(0);
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Concept ? Ok(ConceptJson("Uzun fikir", [60])) : Duplicates();
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = new string('a', 2000), count = 1 }, ct)).EnsureSuccessStatusCode();
        (await WaitJobAsync(client, profile, "ideaJob", ct)).GetProperty("succeeded").GetInt32().ShouldBe(1);
    }

    /// <summary>
    /// AC-24: yalnızca seçili profilin silinmemiş konseptleri, en yeni üstte; varsayılan filtre Üretildi + Onaylandı; filtreyle diğer
    /// durumlar; 51. konsept ikinci sayfada; geçersiz filtre reddedilir; başka profil kendi listesini görür.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task List_FilterPagingNewestFirst_ProfileScoped()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var other = await ReadyProfileAsync(client, ct, name: "İkinci Kanal");
        var userId = UserOf(profile);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        for (var i = 0; i < 51; i++)
        {
            Seed(userId, profile, $"Konsept {i}", ConceptStatus.Generated, start.AddMinutes(i));
        }

        Seed(userId, profile, "Onaylı", ConceptStatus.Approved, start.AddMinutes(100));
        Seed(userId, profile, "Sırada", ConceptStatus.Queued, start.AddMinutes(101));
        Seed(userId, profile, "Üretiliyor", ConceptStatus.Producing, start.AddMinutes(102));
        Seed(userId, profile, "Video", ConceptStatus.VideoReady, start.AddMinutes(103));
        Seed(userId, profile, "Silinmiş", ConceptStatus.Generated, start.AddMinutes(104), deleted: true);
        Seed(userId, other, "Diğer profil", ConceptStatus.Generated, start);

        var first = await ListAsync(client, $"profileId={profile}", ct);
        first.GetProperty("total").GetInt32().ShouldBe(52);
        first.GetProperty("pageSize").GetInt32().ShouldBe(50);
        var items = first.GetProperty("items").EnumerateArray().ToList();
        items.Count.ShouldBe(50);
        items[0].GetProperty("title").GetString().ShouldBe("Onaylı");
        items[0].GetProperty("status").GetString().ShouldBe("approved");
        items[1].GetProperty("title").GetString().ShouldBe("Konsept 50");
        var second = await ListAsync(client, $"profileId={profile}&page=2", ct);
        second.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Konsept 1", "Konsept 0"]);

        var later = await ListAsync(client, $"profileId={profile}&statuses=queued,producing,video_ready", ct);
        later.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("status").GetString()).ShouldBe(["video_ready", "producing", "queued"]);
        (await client.GetAsync(new Uri($"/api/concepts?profileId={profile}&statuses=deleted", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var otherList = await ListAsync(client, $"profileId={other}", ct);
        otherList.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Diğer profil"]);
    }

    /// <summary>AC-25/AC-26: detay tüm sahneleri (başlık, ekran tarifi, seslendirme, süre) ve uyarıları döner; HTML/script metin olarak kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Detail_AllScenes_PlainText()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var id = Seed(UserOf(profile), profile, "<b>Başlık</b>", ConceptStatus.Generated, DateTimeOffset.UtcNow, warn: true);

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/{id}", ct);

        detail.GetProperty("title").GetString().ShouldBe("<b>Başlık</b>");
        var scenes = detail.GetProperty("scenes").EnumerateArray().ToList();
        scenes.Count.ShouldBe(2);
        scenes[0].GetProperty("title").GetString().ShouldBe("Açılış");
        scenes[0].GetProperty("visual").GetString().ShouldBe("<script>alert(1)</script>");
        scenes[1].GetProperty("narration").GetString().ShouldBe("Kapanış metni.");
        scenes[1].GetProperty("durationSeconds").GetInt32().ShouldBe(30);
        detail.GetProperty("wordRepetitionWarning").GetBoolean().ShouldBeTrue();
        detail.GetProperty("durationMismatchWarning").GetBoolean().ShouldBeTrue();
    }

    /// <summary>
    /// AC-27 (BR-16, S-1): ✓ ile tek konsept, toplu eylemle k konsept onaylanır; onaylı listede yalnızca "Onaylandı"lar; geri alma
    /// "Üretildi"ye döndürür; "Onaylandı" dışındaki durumlarda onay/geri alma reddedilir ve hiçbiri değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Approve_Unapprove_BulkAndStatusRules()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var userId = UserOf(profile);
        var a = Seed(userId, profile, "A", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        var b = Seed(userId, profile, "B", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        var c = Seed(userId, profile, "C", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        var queued = Seed(userId, profile, "Q", ConceptStatus.Queued, DateTimeOffset.UtcNow);

        using (var one = await PostAsync(client, "/api/concepts/approve", new { ids = new[] { a } }, ct))
        {
            one.EnsureSuccessStatusCode();
            (await one.Content.ReadFromJsonAsync<JsonElement>(ct))[0].GetProperty("status").GetString().ShouldBe("approved");
        }

        (await PostAsync(client, "/api/concepts/approve", new { ids = new[] { b, c } }, ct)).EnsureSuccessStatusCode();
        var approved = await ListAsync(client, $"profileId={profile}&statuses=approved", ct);
        approved.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["A", "B", "C"], ignoreOrder: true);

        (await PostAsync(client, "/api/concepts/approve", new { ids = new[] { a } }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await PostAsync(client, "/api/concepts/approve", new { ids = new[] { queued } }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await PostAsync(client, "/api/concepts/unapprove", new { ids = new[] { queued } }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await PostAsync(client, "/api/concepts/unapprove", new { ids = new[] { a, queued } }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        StatusOf(a).ShouldBe(ConceptStatus.Approved); // Karışık istekte hiçbiri değişmez.

        (await PostAsync(client, "/api/concepts/unapprove", new { ids = new[] { a } }, ct)).EnsureSuccessStatusCode();
        StatusOf(a).ShouldBe(ConceptStatus.Generated);
        StatusOf(queued).ShouldBe(ConceptStatus.Queued);
        (await ListAsync(client, $"profileId={profile}&statuses=approved", ct)).GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("title").GetString()).ShouldBe(["B", "C"], ignoreOrder: true);
    }

    /// <summary>
    /// AC-28 (BR-1): A, B'nin profiliyle üretme, B'nin konseptini görüntüleme/onaylama/geri alma ve B'nin önerisiyle üretme girişiminde
    /// "bulunamadı" alır; çağrı yapılmaz, B'nin verisi değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ownership_OtherUser_NotFound_NoCall_Unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var owner = await _factory.CreateUserClientAsync(ct);
        var bProfile = await ReadyProfileAsync(owner, ct);
        var bUser = UserOf(bProfile);
        var bConcept = Seed(bUser, bProfile, "B'nin konsepti", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        var bApproved = Seed(bUser, bProfile, "B'nin onaylısı", ConceptStatus.Approved, DateTimeOffset.UtcNow);
        var suggestion = new TopicSuggestion { Id = Guid.CreateVersion7(), UserId = bUser, ProfileId = bProfile, Title = "B'nin önerisi", TitleKey = "b'nin önerisi", Summary = "Özet.", State = SuggestionState.Pending, CreatedAt = DateTimeOffset.UtcNow };
        await _factory.Suggestions.AddRangeAsync([suggestion], ct);

        using var intruder = await _factory.CreateUserClientAsync(ct);
        var aProfile = await ReadyProfileAsync(intruder, ct, name: "A Kanalı");
        (await intruder.GetAsync(new Uri($"/api/concepts/work?profileId={bProfile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.GetAsync(new Uri($"/api/concepts?profileId={bProfile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.GetAsync(new Uri($"/api/concepts/{bConcept}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/topics", new { profileId = bProfile, count = 1 }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/from-idea", new { profileId = bProfile, idea = "Fikir", count = 1 }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/from-topics", new { profileId = bProfile, suggestionIds = new[] { suggestion.Id } }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/from-topics", new { profileId = aProfile, suggestionIds = new[] { suggestion.Id } }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/approve", new { ids = new[] { bConcept } }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/unapprove", new { ids = new[] { bApproved } }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/jobs/stop", new { profileId = bProfile, lane = "topic" }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        _factory.Provider.Calls.Count.ShouldBe(0);
        StatusOf(bConcept).ShouldBe(ConceptStatus.Generated);
        StatusOf(bApproved).ShouldBe(ConceptStatus.Approved);
        _factory.Suggestions.Rows.Single(s => s.Id == suggestion.Id).State.ShouldBe(SuggestionState.Pending);
    }

    /// <summary>AC-28: girişsiz istekler reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concepts_Anonymous_Unauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = _factory.CreateClient();
        (await anonymous.GetAsync(new Uri($"/api/concepts/work?profileId={Guid.NewGuid()}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(new Uri($"/api/concepts?profileId={Guid.NewGuid()}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await PostAsync(anonymous, "/api/concepts/topics", new { profileId = Guid.NewGuid(), count = 1 }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await PostAsync(anonymous, "/api/concepts/approve", new { ids = new[] { Guid.NewGuid() } }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// AC-30 (0003 Req 14): konsepti olan profil (durumdan bağımsız) listede silinemez işaretlidir, doğrudan silme isteği reddedilir ve
    /// profil ile konseptleri yerinde kalır; konsepti olmayan profil silinebilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProfileWithConcept_DeleteRejected_CanDeleteFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var used = await ReadyProfileAsync(client, ct, name: "Kullanılan");
        var empty = await ReadyProfileAsync(client, ct, name: "Boş");
        Seed(UserOf(used), used, "Onaylı konsept", ConceptStatus.Approved, DateTimeOffset.UtcNow);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/profiles", ct);
        var flags = list.GetProperty("items").EnumerateArray().ToDictionary(p => p.GetProperty("id").GetGuid(), p => p.GetProperty("canDelete").GetBoolean());
        flags[used].ShouldBeFalse();
        flags[empty].ShouldBeTrue();

        using (var rejected = await client.DeleteAsync(new Uri($"/api/profiles/{used}", UriKind.Relative), ct))
        {
            rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await rejected.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Bu profilin konseptleri var; silinemez");
        }

        _factory.Profiles.Rows.ShouldContain(p => p.Id == used);
        _factory.Concepts.Rows.ShouldContain(c => c.ProfileId == used);
        (await client.DeleteAsync(new Uri($"/api/profiles/{empty}", UriKind.Relative), ct)).EnsureSuccessStatusCode();
        _factory.Profiles.Rows.ShouldNotContain(p => p.Id == empty);
    }

    /// <summary>
    /// AC-31 (0003 Req 13): Türkçe / dikey / 60 sn profilde üretilen konsept bu değerleri taşır; profil İngilizce / yatay / 120 sn olunca
    /// mevcut konsept değişmez, sonraki konsept yeni değerleri taşır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concept_KeepsProfileValues_AfterProfileEdit()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, name: "Değişen Kanal", seconds: 60);
        _factory.Provider.Handler = (_, role, n) => role == CallRole.Concept ? Ok(ConceptJson($"Konsept {n}", [30, 30])) : Duplicates();
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);
        var before = _factory.Concepts.Rows.Single(c => c.ProfileId == profile);

        (await client.PutAsJsonAsync($"/api/profiles/{profile}", ProfileApiTests.Body("Değişen Kanal", seconds: 120, language: "en", format: "landscape"), ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);

        var after = _factory.Concepts.Rows.Single(c => c.Id == before.Id);
        (after.Language, after.Format, after.TargetDurationSeconds, after.Title).ShouldBe(("tr", "portrait", 60, "Konsept 1"));
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/{before.Id}", ct);
        detail.GetProperty("language").GetString().ShouldBe("tr");
        var next = _factory.Concepts.Rows.Single(c => c.ProfileId == profile && c.Id != before.Id);
        (next.Language, next.Format, next.TargetDurationSeconds).ShouldBe(("en", "landscape", 120));
        next.DurationMismatchWarning.ShouldBeTrue(); // 60 sn sahneler, 120 sn hedef.
    }

    private static bool HasField(JsonElement errors, string field) =>
        errors.EnumerateObject().Any(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase));

    private static async Task<JsonElement> ListAsync(HttpClient client, string query, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>("/api/concepts?" + query, ct);

    private Guid UserOf(Guid profile) => _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;

    private ConceptStatus StatusOf(Guid id) => _factory.Concepts.Rows.Single(c => c.Id == id).Status;

    private Guid Seed(Guid userId, Guid profile, string title, ConceptStatus status, DateTimeOffset at, bool deleted = false, bool warn = false)
    {
        var concept = new Concept
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ProfileId = profile,
            Title = title,
            TitleKey = TitleKey.Of(title, "tr"),
            Source = ConceptSource.Idea,
            Status = status,
            Language = "tr",
            Format = "portrait",
            TargetDurationSeconds = 60,
            Scenes = [new ConceptScene("Açılış", "<script>alert(1)</script>", "Açılış metni.", 30), new ConceptScene("Kapanış", "Harita", "Kapanış metni.", 30)],
            WordRepetitionWarning = warn,
            DurationMismatchWarning = warn,
            CreatedAt = at,
            UpdatedAt = at,
            DeletedAt = deleted ? at : null,
        };
        _factory.Concepts.AddAsync(concept, CancellationToken.None).GetAwaiter().GetResult();
        return concept.Id;
    }
}
