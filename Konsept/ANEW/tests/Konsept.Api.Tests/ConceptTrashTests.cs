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
/// Silinenler ve önerileri sıfırla uçtan uca (spec 0016 AC-1, AC-2, AC-4–AC-9, AC-11, AC-13): gerçek Concepts servisleri, sahte sağlayıcı
/// (gerçek LLM çağrısı yok), bellek depoları. Her test kendi kullanıcısıyla çalışır.
/// </summary>
public sealed class ConceptTrashTests : IClassFixture<ConceptsApiFactory>
{
    private static readonly ConceptScene[] Scenes =
    [
        new("Açılış", "Surlar görünür.", "Bizans'ın son sabahı başlıyor.", 30),
        new("Son", "Şehir sessizleşir.", "Bir çağ kapanıyor.", 30),
    ];

    private readonly ConceptsApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConceptTrashTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ConceptTrashTests(ConceptsApiFactory factory)
    {
        _factory = factory;
        _factory.Provider.Reset();
    }

    /// <summary>
    /// AC-1: Üretildi ve Onaylandı konsept silinince Üretilenler'den (ve onaylı listeden) kalkar, Silinenler'de görünür ve kayıt süresiz
    /// durur (yeniden okumada da orada).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Delete_MovesToDeleted_Persists_ApprovedListToo()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var generated = Seed(UserOf(profile), profile, "Üretilmiş", ConceptStatus.Generated);
        var approved = Seed(UserOf(profile), profile, "Onaylı", ConceptStatus.Approved);

        foreach (var id in new[] { generated, approved })
        {
            using var deleted = await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct);
            deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await deleted.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid().ShouldBe(id);
        }

        (await client.GetFromJsonAsync<JsonElement>($"/api/concepts?profileId={profile}&statuses=generated,approved", ct)).GetProperty("total").GetInt32().ShouldBe(0);
        (await client.GetFromJsonAsync<JsonElement>($"/api/concepts?profileId={profile}&statuses=approved", ct)).GetProperty("total").GetInt32().ShouldBe(0);
        var list = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/deleted?profileId={profile}", ct);
        list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Onaylı", "Üretilmiş"]);
        _factory.Concepts.Rows.Where(c => c.ProfileId == profile).ShouldAllBe(c => c.DeletedAt != null);
        (await client.GetAsync(new Uri($"/api/concepts/{generated}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _factory.Provider.Calls.Count.ShouldBe(0);
    }

    /// <summary>AC-2: Sırada, Üretiliyor, Video oldu konseptte doğrudan silme isteği açıklayıcı mesajla reddedilir; konsept değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Delete_LockedStatuses_Rejected_Unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        foreach (var (status, message) in new[]
        {
            (ConceptStatus.Queued, "Önce üretim sırasından çıkarın."),
            (ConceptStatus.Producing, "Önce üretim sırasından çıkarın."),
            (ConceptStatus.VideoReady, "Videosu olan konsept burada silinemez."),
        })
        {
            var id = Seed(UserOf(profile), profile, "Kilitli", status);
            using var response = await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe(message);
            var row = _factory.Concepts.Rows.Single(c => c.Id == id);
            row.DeletedAt.ShouldBeNull();
            row.Status.ShouldBe(status);
        }
    }

    /// <summary>AC-4: liste yalnızca seçili profilin silinmişleridir, en yeni silinen üstte, 51. kayıt ikinci sayfada.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Deleted_List_ProfileScoped_NewestFirst_Paged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var other = await ReadyProfileAsync(client, ct, name: "Diğer Kanal");
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        for (var i = 0; i < 51; i++)
        {
            Seed(UserOf(profile), profile, $"Silinen {i}", ConceptStatus.Generated, start.AddMinutes(i));
        }

        Seed(UserOf(profile), profile, "Silinmemiş", ConceptStatus.Generated);
        Seed(UserOf(other), other, "Diğer profilin silineni", ConceptStatus.Generated, start);

        var first = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/deleted?profileId={profile}&page=1", ct);
        first.GetProperty("total").GetInt32().ShouldBe(51);
        var titles = first.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ToList();
        titles.Count.ShouldBe(50);
        titles[0].ShouldBe("Silinen 50");
        titles.ShouldNotContain("Silinmemiş");
        titles.ShouldNotContain("Diğer profilin silineni");
        var second = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/deleted?profileId={profile}&page=2", ct);
        second.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Silinen 0"]);
        var otherList = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/deleted?profileId={other}", ct);
        otherList.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ShouldBe(["Diğer profilin silineni"]);
    }

    /// <summary>AC-5: silinmiş konseptin sahneleri okunur; düzenleme, onay ve yeniden üretim istekleri reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Deleted_Detail_ReadOnly_EditRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var id = Seed(UserOf(profile), profile, "Silinmiş konu", ConceptStatus.Generated, DateTimeOffset.UtcNow);

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/concepts/deleted/{id}", ct);
        detail.GetProperty("title").GetString().ShouldBe("Silinmiş konu");
        detail.GetProperty("scenes").GetArrayLength().ShouldBe(2);
        (await PostAsync(client, $"/api/concepts/{id}/scenes", new { scenes = Array.Empty<object>() }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(client, $"/api/concepts/{id}/regenerate", new { instruction = "Kısalt" }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(client, "/api/concepts/approve", new { ids = new[] { id } }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _factory.Concepts.Rows.Single(c => c.Id == id).Scenes.ShouldBe(Scenes);
        _factory.Provider.Calls.Count.ShouldBe(0);
        var live = Seed(UserOf(profile), profile, "Canlı", ConceptStatus.Generated);
        (await client.GetAsync(new Uri($"/api/concepts/deleted/{live}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>AC-6: kalıcı silme kaydı tamamen kaldırır; Silinenler'de ve Üretilenler'de görünmez; silinmemiş konsept kalıcı silinemez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Purge_RemovesForever()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var id = Seed(UserOf(profile), profile, "Kalıcı gidecek", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        var live = Seed(UserOf(profile), profile, "Canlı", ConceptStatus.Generated);

        (await PostAsync(client, $"/api/concepts/deleted/{live}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _factory.Concepts.Rows.ShouldContain(c => c.Id == live);
        (await PostAsync(client, $"/api/concepts/deleted/{id}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        _factory.Concepts.Rows.ShouldNotContain(c => c.Id == id);
        (await client.GetFromJsonAsync<JsonElement>($"/api/concepts/deleted?profileId={profile}", ct)).GetProperty("total").GetInt32().ShouldBe(0);
        (await client.GetAsync(new Uri($"/api/concepts/deleted/{id}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(client, $"/api/concepts/deleted/{id}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// AC-7: Silinenler'deki konseptin başlığı (konudan gelmiş olsa da) öneri talimatında "elenen" konulardadır, anlamca denetim
    /// kümesindedir ve stub birebir döndürse de listelenmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trash_DeletedTitle_ExcludedInPrompt_NotListed()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var userId = UserOf(profile);
        var id = Seed(userId, profile, "Beğenilmeyen konu", ConceptStatus.Approved, source: ConceptSource.Topic);
        await SelectedSuggestionAsync(userId, profile, "Beğenilmeyen konu", ct);
        Seed(userId, profile, "Yeni bir başka konu", ConceptStatus.Generated); // Denetimde aday için kıyas kümesi boş olmasın.
        (await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct)).EnsureSuccessStatusCode();
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics
            ? Ok(TopicsJson(("Beğenilmeyen konu", "Tekrar."), ("Taze konu", "Yeni.")))
            : Duplicates();

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 2 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("duplicates").GetInt32().ShouldBe(1);
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Taze konu"]);
        var system = _factory.Provider.CallsOf(CallRole.Topics).Single().System!;
        Section(system, "excluded_topics").ShouldContain("Beğenilmeyen konu");
        Section(system, "chosen_topics").ShouldNotContain("Beğenilmeyen konu");
        _factory.Provider.CallsOf(CallRole.Dedupe).Single().Prompt.ShouldContain("Beğenilmeyen konu");
    }

    /// <summary>AC-7: fikirden üretimde Silinenler'deki başlıkla aynı başlıklı konsept kaydedilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trash_DeletedTitle_BlocksIdeaDuplicate()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        Seed(UserOf(profile), profile, "Osmanlı 1", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        _factory.Provider.Handler = (_, role, n) => role == CallRole.Concept ? Ok(ConceptJson($"Osmanlı {n}", [20, 20, 20])) : Duplicates();

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Osmanlı", count = 2 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);

        job.GetProperty("duplicates").GetInt32().ShouldBe(1);
        _factory.Concepts.Rows.Where(c => c.ProfileId == profile && c.DeletedAt == null).Select(c => c.Title).ShouldBe(["Osmanlı 2"]);
    }

    /// <summary>
    /// AC-8: kalıcı silinen konseptin başlığı (seçildi öneri hafızasıyla birlikte) sonraki talimatta yer almaz ve stub döndürünce öneri
    /// listelenir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Purge_TitleLeavesPrompt_SuggestionListed()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var userId = UserOf(profile);
        var id = Seed(userId, profile, "Geri dönecek konu", ConceptStatus.Generated, DateTimeOffset.UtcNow, ConceptSource.Topic);
        await SelectedSuggestionAsync(userId, profile, "Geri dönecek konu", ct);
        (await PostAsync(client, $"/api/concepts/deleted/{id}/purge", new { }, ct)).EnsureSuccessStatusCode();
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Ok(TopicsJson(("Geri dönecek konu", "Yeniden."))) : Duplicates();

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);

        _factory.Provider.CallsOf(CallRole.Topics).Single().System!.ShouldNotContain("Geri dönecek konu");
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Geri dönecek konu"]);
    }

    /// <summary>
    /// AC-9: sıfırlama yalnızca seçili profilin önerilip seçilmemiş başlıklarını serbest bırakır; aynı profilin üretilmiş ve Silinenler'deki
    /// konsept başlıkları ile başka profilin seçilmemiş başlığı engel olmaya devam eder.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Reset_OnlyRejectedOfSelectedProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var other = await ReadyProfileAsync(client, ct, name: "Diğer Kanal");
        var userId = UserOf(profile);
        await SuggestionAsync(userId, profile, "Seçilmemiş konu", SuggestionState.Rejected, ct);
        await SuggestionAsync(userId, profile, "Seçilmiş konu", SuggestionState.Selected, ct);
        await SuggestionAsync(userId, other, "Öteki seçilmemiş", SuggestionState.Rejected, ct);
        Seed(userId, profile, "Üretilmiş konu", ConceptStatus.Generated);
        Seed(userId, profile, "Silinmiş konu", ConceptStatus.Generated, DateTimeOffset.UtcNow);

        using var reset = await PostAsync(client, "/api/concepts/suggestions/reset", new { profileId = profile }, ct);
        reset.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await reset.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("cleared").GetInt32().ShouldBe(1);
        _factory.Suggestions.Rows.ShouldNotContain(s => s.Title == "Seçilmemiş konu");
        _factory.Suggestions.Rows.ShouldContain(s => s.Title == "Seçilmiş konu");
        _factory.Suggestions.Rows.ShouldContain(s => s.Title == "Öteki seçilmemiş");

        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics
            ? Ok(TopicsJson(("Seçilmemiş konu", "Yeniden."), ("Üretilmiş konu", "X."), ("Silinmiş konu", "X."), ("Seçilmiş konu", "X.")))
            : Duplicates();
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 4 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Seçilmemiş konu"]);

        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Ok(TopicsJson(("Öteki seçilmemiş", "X."), ("Ötekinde yeni", "Y."))) : Duplicates();
        (await PostAsync(client, "/api/concepts/topics", new { profileId = other, count = 2 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, other, "topicJob", ct);
        (await WorkAsync(client, other, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Ötekinde yeni"]);
    }

    /// <summary>AC-11: başka kullanıcının konseptini silme, Silinenler kaydını görme/kalıcı silme ve önerilerini sıfırlama "bulunamadı"dır; B'nin verisi değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trash_OtherUser_NotFound_Unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var owner = await _factory.CreateUserClientAsync(ct);
        using var intruder = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(owner, ct);
        var userId = UserOf(profile);
        var live = Seed(userId, profile, "B'nin konsepti", ConceptStatus.Generated);
        var deleted = Seed(userId, profile, "B'nin silineni", ConceptStatus.Generated, DateTimeOffset.UtcNow);
        await SuggestionAsync(userId, profile, "B'nin seçilmemişi", SuggestionState.Rejected, ct);

        (await PostAsync(intruder, $"/api/concepts/{live}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.GetAsync(new Uri($"/api/concepts/deleted/{deleted}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await intruder.GetAsync(new Uri($"/api/concepts/deleted?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, $"/api/concepts/deleted/{deleted}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(intruder, "/api/concepts/suggestions/reset", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        _factory.Concepts.Rows.Single(c => c.Id == live).DeletedAt.ShouldBeNull();
        _factory.Concepts.Rows.ShouldContain(c => c.Id == deleted);
        _factory.Suggestions.Rows.ShouldContain(s => s.Title == "B'nin seçilmemişi");
    }

    /// <summary>AC-11: girişsiz istekler reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trash_Anonymous_Unauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = _factory.CreateClient();
        var id = Guid.NewGuid();
        (await PostAsync(anonymous, $"/api/concepts/{id}/delete", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(new Uri($"/api/concepts/deleted?profileId={id}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(new Uri($"/api/concepts/deleted/{id}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await PostAsync(anonymous, $"/api/concepts/deleted/{id}/purge", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await PostAsync(anonymous, "/api/concepts/suggestions/reset", new { profileId = id }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// AC-13: tek konsepti Silinenler'de olan profil silinemez ("Bu profilin konseptleri var; silinemez"), profil ve konsept yerinde kalır;
    /// konsept kalıcı silinince profil silinebilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trash_ProfileDelete_BlockedUntilPurged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var id = Seed(UserOf(profile), profile, "Tek konsept", ConceptStatus.Generated);
        (await PostAsync(client, $"/api/concepts/{id}/delete", new { }, ct)).EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<JsonElement>("/api/profiles", ct);
        list.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == profile).GetProperty("canDelete").GetBoolean().ShouldBeFalse();
        using (var rejected = await client.DeleteAsync(new Uri($"/api/profiles/{profile}", UriKind.Relative), ct))
        {
            rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await rejected.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Bu profilin konseptleri var; silinemez");
        }

        _factory.Profiles.Rows.ShouldContain(p => p.Id == profile);
        _factory.Concepts.Rows.ShouldContain(c => c.Id == id);
        (await PostAsync(client, $"/api/concepts/deleted/{id}/purge", new { }, ct)).EnsureSuccessStatusCode();
        (await client.DeleteAsync(new Uri($"/api/profiles/{profile}", UriKind.Relative), ct)).EnsureSuccessStatusCode();
        _factory.Profiles.Rows.ShouldNotContain(p => p.Id == profile);
    }

    private static string Section(string system, string tag)
    {
        var start = system.IndexOf("<" + tag + ">", StringComparison.Ordinal);
        var end = system.IndexOf("</" + tag + ">", StringComparison.Ordinal);
        return start < 0 || end < start ? string.Empty : system[start..end];
    }

    private Task SelectedSuggestionAsync(Guid userId, Guid profile, string title, CancellationToken ct) =>
        SuggestionAsync(userId, profile, title, SuggestionState.Selected, ct, DateTimeOffset.UtcNow.AddHours(-1));

    private Task SuggestionAsync(Guid userId, Guid profile, string title, SuggestionState state, CancellationToken ct, DateTimeOffset? at = null) =>
        _factory.Suggestions.AddRangeAsync(
            [new TopicSuggestion { Id = Guid.CreateVersion7(), UserId = userId, ProfileId = profile, Title = title, TitleKey = TitleKey.Of(title, "tr"), Summary = "Özet.", State = state, CreatedAt = at ?? DateTimeOffset.UtcNow }],
            ct);

    private Guid UserOf(Guid profile) => _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;

    private Guid Seed(Guid userId, Guid profile, string title, ConceptStatus status, DateTimeOffset? deletedAt = null, ConceptSource source = ConceptSource.Idea)
    {
        var id = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow.AddHours(-2);
        _factory.Concepts.AddAsync(
            new Concept
            {
                Id = id,
                UserId = userId,
                ProfileId = profile,
                Title = title,
                TitleKey = TitleKey.Of(title, "tr"),
                Source = source,
                Status = status,
                Language = "tr",
                Format = "portrait",
                TargetDurationSeconds = 60,
                Scenes = Scenes,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = deletedAt,
            },
            CancellationToken.None).GetAwaiter().GetResult();
        return id;
    }
}
