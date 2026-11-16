using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Shouldly;
using static Konsept.Api.Tests.Support.ConceptTestKit;

namespace Konsept.Api.Tests;

/// <summary>
/// Konsept üretimi uçtan uca (spec 0014 AC-3–AC-8, AC-10–AC-14, AC-16–AC-23; H-3): gerçek Concepts servisleri ve 0013 çağrı kapısı,
/// isteğe göre yanıt veren sahte sağlayıcı (gerçek LLM çağrısı yok), bellek depoları. Her test kendi kullanıcısıyla çalışır.
/// </summary>
public sealed class ConceptGenerationTests : IClassFixture<ConceptsApiFactory>
{
    private readonly ConceptsApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConceptGenerationTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ConceptGenerationTests(ConceptsApiFactory factory)
    {
        _factory = factory;
        _factory.Provider.Reset();
    }

    /// <summary>
    /// AC-3: N=4 için stub 2 cümlelik başlık, 4 cümlelik özet ve sahne içeren öneri döndürünce bunlar elenir, kalan listelenir,
    /// "biçim dışı" sayısı 3'tür ve eksik adet için ek öneri çağrısı yapılmaz; N'den fazla dönen öneri listelenmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Topics_FormatViolations_Hidden_NoExtraCall()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics
            ? Ok(TopicsJson(
                ("Bizans'ın son günü", "Kuşatmanın son saatleri. Surlar düşer."),
                ("İki cümle. Başlık bu.", "Özet."),
                ("Uzun özet", "Bir. İki. Üç. Dört."),
                ("Sahneli öneri", "Sahne 1: surlar görünür."),
                ("Fazladan beşinci", "Listelenmez.")))
            : Duplicates();

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 4 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("status").GetString().ShouldBe("completed");
        job.GetProperty("succeeded").GetInt32().ShouldBe(1);
        job.GetProperty("formatRejected").GetInt32().ShouldBe(3);
        var work = await WorkAsync(client, profile, ct);
        work.GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Bizans'ın son günü"]);
        _factory.Provider.CallsOf(CallRole.Topics).Count.ShouldBe(1);
        _factory.Provider.CallsOf(CallRole.Dedupe).Count.ShouldBe(1);
    }

    /// <summary>
    /// AC-4: üretilmiş (her durum) ve önerilip seçilmemiş başlıklar talimatta "elenen", seçilip üretilenler "seçilen" konular olarak gider;
    /// stub bunlardan birini birebir döndürse de listelenmez; başka profildeki başlık engel olmaz. Ayrıca ince ayar öneri talimatında (AC-14).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Topics_History_ExcludedAndInPrompt_OtherProfileFree()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var other = await ReadyProfileAsync(client, ct, name: "Diğer Kanal");
        var userId = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        Seed(userId, profile, "Seçilen eski konu", ConceptSource.Topic, ConceptStatus.VideoReady);
        Seed(userId, profile, "Fikirden eski konu", ConceptSource.Idea, ConceptStatus.Queued);
        Seed(userId, other, "Başka profilin konusu", ConceptSource.Topic, ConceptStatus.Generated);
        await _factory.Suggestions.AddRangeAsync(
            [new TopicSuggestion { Id = Guid.CreateVersion7(), UserId = userId, ProfileId = profile, Title = "Reddedilen konu", TitleKey = TitleKey.Of("Reddedilen konu", "tr"), Summary = "Özet.", State = SuggestionState.Rejected, CreatedAt = DateTimeOffset.UtcNow }],
            ct);
        (await client.PutAsJsonAsync($"/api/profiles/{profile}/concept-settings", new { fineTune = new { tone = "calm", audience = (string?)null, narration = (string?)null } }, ct)).EnsureSuccessStatusCode();
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics
            ? Ok(TopicsJson(
                ("seçilen eski konu.", "Tekrar."),
                ("Reddedilen Konu", "Tekrar."),
                ("Fikirden eski konu", "Tekrar."),
                ("Başka profilin konusu", "Bu profilde yeni."),
                ("Yepyeni konu", "Yeni.")))
            : Duplicates();

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 5 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("duplicates").GetInt32().ShouldBe(3);
        var titles = (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ToList();
        titles.ShouldBe(["Başka profilin konusu", "Yepyeni konu"]);
        var system = _factory.Provider.CallsOf(CallRole.Topics).Single().System!;
        Section(system, "chosen_topics").ShouldContain("Seçilen eski konu");
        Section(system, "excluded_topics").ShouldContain("Reddedilen konu");
        Section(system, "excluded_topics").ShouldContain("Fikirden eski konu");
        system.ShouldNotContain("Başka profilin konusu");
        system.ShouldContain("Tone: calm");
    }

    /// <summary>AC-5: talimata birebir girmeyen (200'den eski) bir başlığı stub döndürürse öneri yine listelenmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Topics_OldTitle_NotListed()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var userId = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        var start = DateTimeOffset.UtcNow.AddDays(-30);
        for (var i = 0; i < 249; i++) // 249 konsept + 1 reddedilmiş öneri = 250 başlık.
        {
            Seed(userId, profile, $"Geçmiş konu {i}", ConceptSource.Idea, ConceptStatus.Generated, start.AddMinutes(i));
        }

        await _factory.Suggestions.AddRangeAsync(
            [new TopicSuggestion { Id = Guid.CreateVersion7(), UserId = userId, ProfileId = profile, Title = "Reddedilmiş öneri", TitleKey = TitleKey.Of("Reddedilmiş öneri", "tr"), Summary = "Bu özet talimata girmemeli.", State = SuggestionState.Rejected, CreatedAt = DateTimeOffset.UtcNow }],
            ct);

        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Ok(TopicsJson(("Geçmiş konu 3", "Eski."), ("Yeni konu", "Yeni."))) : Duplicates();

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 2 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);

        var system = _factory.Provider.CallsOf(CallRole.Topics).Single().System!;
        system.ShouldContain("<older_topics count=\"50\">");
        system.ShouldNotContain("- Geçmiş konu 3\n");
        system.ShouldContain("Reddedilmiş öneri");
        system.ShouldNotContain("Bu özet talimata girmemeli."); // Talimata yalnızca başlıklar girer (G-17).
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Yeni konu"]);
        _factory.Provider.Calls.Count.ShouldBe(2); // Öneri + denetim; özet için çağrı yok.
    }

    /// <summary>AC-6: denetçi "aynı konu" işaretleyince aday listelenmez; partide denetim rolüyle tam 1 denetim çağrısı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Topics_SemanticDuplicate_Hidden_OneCheckCall()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var userId = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        Seed(userId, profile, "Vikinglerin Ayasofya seferi", ConceptSource.Topic, ConceptStatus.Generated);
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics
            ? Ok(TopicsJson(("Vikingler Konstantinopolis'te", "Aynı konu."), ("Mısır piramitleri", "Farklı.")))
            : Duplicates(1);

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 2 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("dedupeUnavailable").GetBoolean().ShouldBeFalse();
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Mısır piramitleri"]);
        var checks = _factory.Provider.CallsOf(CallRole.Dedupe);
        checks.Count.ShouldBe(1);
        checks[0].Model.Id.ShouldBe(DedupeModel);
        checks[0].Prompt.ShouldContain("Vikinglerin Ayasofya seferi");
        _factory.Spend.Records.Count(r => r.Purpose == CallPurpose.Review && r.ProfileId == profile).ShouldBe(1);
    }

    /// <summary>AC-6: denetim çağrısı tüm denemelerde başarısızsa öneriler yalnızca birebir elemeyle listelenir, bilgi görünür, ek çağrı yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Topics_CheckFails_ExactOnly_Info()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics
            ? Ok(TopicsJson(("Birinci konu", "Özet."), ("İkinci konu", "Özet.")))
            : Task.FromResult(ProviderAttempt.Failed(400));

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 2 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("dedupeUnavailable").GetBoolean().ShouldBeTrue();
        job.GetProperty("succeeded").GetInt32().ShouldBe(2);
        _factory.Provider.CallsOf(CallRole.Dedupe).Count.ShouldBe(1);
    }

    /// <summary>
    /// AC-7: seçili yokken istek reddedilir; k öneri seçilince yalnızca k konsept üretilir ve "Üretildi" düşer; seçilmeyenler "önerilip
    /// seçilmemiş" olur ve listeden kalkar; liste yenilemede korunur; yeni "Konu üret" bekleyen tüm önerileri listeden kaldırır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task FromTopics_OnlySelected_RestRejected_ListPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, role, n) => role switch
        {
            CallRole.Topics when n == 1 => Ok(TopicsJson(("Konu A", "Özet."), ("Konu B", "Özet."), ("Konu C", "Özet."))),
            CallRole.Topics => Ok(TopicsJson(("Konu D", "Özet."))),
            CallRole.Concept => Ok(ConceptJson("Model başlığı", [30, 30])),
            _ => Duplicates(),
        };
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 3 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);

        var reloaded = (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().ToList();
        reloaded.Count.ShouldBe(3); // Sayfa yenilense de liste kalır (G-1).
        (await PostAsync(client, "/api/concepts/from-topics", new { profileId = profile, suggestionIds = Array.Empty<Guid>() }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var picked = new[] { reloaded[0].GetProperty("id").GetGuid(), reloaded[2].GetProperty("id").GetGuid() };
        (await PostAsync(client, "/api/concepts/from-topics", new { profileId = profile, suggestionIds = picked }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("succeeded").GetInt32().ShouldBe(2);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(2);
        var saved = _factory.Concepts.Rows.Where(c => c.ProfileId == profile).ToList();
        saved.Select(c => c.Title).ShouldBe(["Konu A", "Konu C"], ignoreOrder: true);
        saved.ShouldAllBe(c => c.Status == ConceptStatus.Generated && c.Source == ConceptSource.Topic);
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").GetArrayLength().ShouldBe(0);
        _factory.Suggestions.Rows.Single(s => s.Title == "Konu B").State.ShouldBe(SuggestionState.Rejected);

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Ok(TopicsJson(("Konu E", "Özet."))) : Duplicates();
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        _factory.Suggestions.Rows.Single(s => s.Title == "Konu D").State.ShouldBe(SuggestionState.Rejected);
        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ShouldBe(["Konu E"]);
    }

    /// <summary>AC-8: sağlayıcı hatasında iş "başarısız" olur ve mesaj sade Türkçedir (stack trace, key ya da ham gövde yok).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Topics_ProviderError_PlainMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, _, _) => Task.FromResult(ProviderAttempt.Failed(401));

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 3 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("status").GetString().ShouldBe("failed");
        var message = job.GetProperty("message").GetString()!;
        message.ShouldNotBeNullOrWhiteSpace();
        message.ShouldNotContain("sk-ant");
        message.ShouldNotContain("Exception");
        message.ShouldNotContain(" at ");
    }

    /// <summary>AC-10/AC-31: fikirden N=3 üretim öneri adımı olmadan 3 konsepti "Üretildi" ve profile bağlı kaydeder; konsept profilin dil, biçim, süresini taşır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task FromIdea_SavesUpToN_Generated_ProfileBound()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, seconds: 60);
        _factory.Provider.Handler = (_, role, n) => role == CallRole.Concept ? Ok(ConceptJson($"Osmanlı {n}", [20, 20, 20])) : Duplicates();

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Osmanlı'nın kuruluşu, ilk 3 padişah", count = 3 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);

        job.GetProperty("status").GetString().ShouldBe("completed");
        job.GetProperty("processed").GetInt32().ShouldBe(3);
        var saved = _factory.Concepts.Rows.Where(c => c.ProfileId == profile).ToList();
        saved.Count.ShouldBe(3);
        saved.ShouldAllBe(c => c.Status == ConceptStatus.Generated && c.Source == ConceptSource.Idea && c.Language == "tr" && c.Format == "portrait" && c.TargetDurationSeconds == 60);
        _factory.Provider.CallsOf(CallRole.Topics).Count.ShouldBe(0);
        _factory.Provider.CallsOf(CallRole.Concept)[0].Prompt.ShouldContain("<idea>Osmanlı'nın kuruluşu, ilk 3 padişah</idea>");
    }

    /// <summary>AC-11: bazıları sağlayıcı hatasıyla üretilemezse başarılılar kaydedilir ve hatalılar sıra numarasıyla bildirilir; hiçbiri üretilemezse sade hata ve Üretilenler değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task FromIdea_PartialAndTotalFailure_Reported()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, role, n) => role == CallRole.Concept
            ? (n == 2 ? Task.FromResult(ProviderAttempt.Failed(400)) : Ok(ConceptJson($"Konsept {n}", [60])))
            : Duplicates();

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 3 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);

        job.GetProperty("succeeded").GetInt32().ShouldBe(2);
        job.GetProperty("failures").EnumerateArray().Select(f => f.GetProperty("label").GetString()).ShouldBe(["2"]);

        using var client2 = await _factory.CreateUserClientAsync(ct);
        var profile2 = await ReadyProfileAsync(client2, ct);
        _factory.Provider.Handler = (_, _, _) => Task.FromResult(ProviderAttempt.Failed(403));
        (await PostAsync(client2, "/api/concepts/from-idea", new { profileId = profile2, idea = "Fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        var failed = await WaitJobAsync(client2, profile2, "ideaJob", ct);
        failed.GetProperty("status").GetString().ShouldBe("failed");
        failed.GetProperty("message").GetString().ShouldNotBeNullOrWhiteSpace();
        _factory.Concepts.Rows.Count(c => c.ProfileId == profile2).ShouldBe(0);
    }

    /// <summary>
    /// AC-12 (H-3, kasıtlı bozma hedefi): partide aynı başlık iki kez, profilde birebir mevcut başlık ya da denetçinin "aynı konu"
    /// işaretlediği konsept kaydedilmez; yerine yeni konsept çağrısı yapılmaz; parti başına tam 1 denetim çağrısı.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task FromIdea_Duplicates_NotSaved_NoReplacementCall()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var userId = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        Seed(userId, profile, "Mevcut başlık", ConceptSource.Idea, ConceptStatus.Approved);
        string[] titles = ["Yeni bir", "Yeni bir", "Mevcut başlık", "Anlamca aynı", "Yeni iki"];
        _factory.Provider.Handler = (_, role, n) => role == CallRole.Concept ? Ok(ConceptJson(titles[n - 1], [60])) : Duplicates(2);

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 5 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);

        job.GetProperty("duplicates").GetInt32().ShouldBe(3);
        job.GetProperty("succeeded").GetInt32().ShouldBe(2);
        _factory.Concepts.Rows.Where(c => c.ProfileId == profile && c.Source == ConceptSource.Idea && c.Status == ConceptStatus.Generated)
            .Select(c => c.Title).ShouldBe(["Yeni bir", "Yeni iki"], ignoreOrder: true);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(5);
        _factory.Provider.CallsOf(CallRole.Dedupe).Count.ShouldBe(1);
    }

    /// <summary>
    /// AC-13/AC-26: eksik alanlı çıktı kaydedilmez ve hata olarak bildirilir; hedef 60 sn için 50 ve 70 sn "süre uyumsuz" uyarısıyla,
    /// 51 ve 69 sn uyarısız kaydedilir; ek çağrı yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concept_MissingFields_NotSaved_DurationWarning()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, seconds: 60);
        _factory.Provider.Handler = (_, role, n) => role != CallRole.Concept ? Duplicates() : n switch
        {
            1 => Ok("""{"title":"Eksik","scenes":[{"title":"S","visual":"V","durationSeconds":60}]}"""),
            2 => Ok(ConceptJson("Elli", [25, 25])),
            3 => Ok(ConceptJson("Yetmiş", [35, 35])),
            4 => Ok(ConceptJson("Elli bir", [25, 26])),
            _ => Ok(ConceptJson("Altmış dokuz", [34, 35])),
        };

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 5 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);

        job.GetProperty("failures").GetArrayLength().ShouldBe(1);
        var saved = _factory.Concepts.Rows.Where(c => c.ProfileId == profile).ToDictionary(c => c.Title, c => c.DurationMismatchWarning);
        saved.Count.ShouldBe(4);
        saved["Elli"].ShouldBeTrue();
        saved["Yetmiş"].ShouldBeTrue();
        saved["Elli bir"].ShouldBeFalse();
        saved["Altmış dokuz"].ShouldBeFalse();
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(5);
    }

    /// <summary>
    /// AC-16 (BR-15, kasıtlı bozma hedefi): kelime denetimi açıkken ilk çıktı eşiği aşarsa tam 1 yeniden üretim; ikincisi de aşarsa
    /// "kelime tekrarı" uyarısıyla kaydedilir, üçüncü çağrı yok. İkinci çıktı eşiğin altındaysa uyarısız kaydedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task WordCheck_Exceeded_RegeneratesOnce_ThenWarns()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, wordCheck: true);
        const string Repeating = "Işık ışık ışık ışık ışık doğdu sabah.";
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Concept ? Ok(ConceptJson("Işık", [60], Repeating)) : Duplicates();

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Işık", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);

        var calls = _factory.Provider.CallsOf(CallRole.Concept);
        calls.Count.ShouldBe(2);
        calls[1].Prompt.ShouldContain("repeated the same words");
        _factory.Concepts.Rows.Single(c => c.ProfileId == profile).WordRepetitionWarning.ShouldBeTrue();

        using var client2 = await _factory.CreateUserClientAsync(ct);
        var profile2 = await ReadyProfileAsync(client2, ct, wordCheck: true);
        _factory.Provider.Reset();
        _factory.Provider.Handler = (_, role, n) => role == CallRole.Concept
            ? Ok(ConceptJson("Işık", [60], n == 1 ? Repeating : "Sabah doğan güneş vadiyi aydınlattı."))
            : Duplicates();
        (await PostAsync(client2, "/api/concepts/from-idea", new { profileId = profile2, idea = "Işık", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client2, profile2, "ideaJob", ct);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(2);
        _factory.Concepts.Rows.Single(c => c.ProfileId == profile2).WordRepetitionWarning.ShouldBeFalse();
    }

    /// <summary>AC-17: kelime denetimi kapalıyken eşiği aşan konsept yeniden üretilmez ve uyarısız kaydedilir; eşiğin altındaki, kutu açıkken de yeniden üretilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task WordCheck_Off_Or_Below_NoRegeneration()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var off = await ReadyProfileAsync(client, ct, wordCheck: false);
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Concept ? Ok(ConceptJson("Işık", [60], "Işık ışık ışık ışık ışık.")) : Duplicates();
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = off, idea = "Işık", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, off, "ideaJob", ct);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(1);
        _factory.Concepts.Rows.Single(c => c.ProfileId == off).WordRepetitionWarning.ShouldBeFalse();

        using var client2 = await _factory.CreateUserClientAsync(ct);
        var on = await ReadyProfileAsync(client2, ct, wordCheck: true);
        _factory.Provider.Reset();
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Concept ? Ok(ConceptJson("Güneş", [60], "Sabah doğan güneş vadiyi aydınlattı.")) : Duplicates();
        (await PostAsync(client2, "/api/concepts/from-idea", new { profileId = on, idea = "Güneş", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client2, on, "ideaJob", ct);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(1);
    }

    /// <summary>
    /// AC-18 (BR-12, kasıtlı bozma hedefi): öneri, denetim, konsept ve BR-15 yeniden üretimi çağrılarının her biri doğru amaç ve seçili
    /// profille bir harcama kaydı üretir; sağlayıcıya giden çağrı sayısı ile kayıt sayısı eşittir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Spend_EveryCallRecorded_RoleAndProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, wordCheck: true);
        var userId = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        _factory.Provider.Handler = (_, role, _) => role switch
        {
            CallRole.Topics => Ok(TopicsJson(("Konu", "Özet."))),
            CallRole.Concept => Ok(ConceptJson("Işık", [60], "Işık ışık ışık ışık ışık.")),
            _ => Duplicates(),
        };

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        var id = (await WorkAsync(client, profile, ct)).GetProperty("suggestions")[0].GetProperty("id").GetGuid();
        (await PostAsync(client, "/api/concepts/from-topics", new { profileId = profile, suggestionIds = new[] { id } }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);

        var records = _factory.Spend.Records.Where(r => r.UserId == userId).ToList();
        records.Count.ShouldBe(_factory.Provider.Calls.Count);
        records.ShouldAllBe(r => r.ProfileId == profile);
        records.Count(r => r.Purpose == CallPurpose.TopicSuggestion && r.ModelId == TopicModel).ShouldBe(1);
        records.Count(r => r.Purpose == CallPurpose.Review && r.ModelId == DedupeModel).ShouldBe(2);
        records.Count(r => r.Purpose == CallPurpose.ConceptGeneration && r.ModelId == ConceptModel).ShouldBe(4); // 2 konsept × (ilk + BR-15)
    }

    /// <summary>
    /// AC-19 (H-3, kasıtlı bozma hedefi): sürekli 5xx dönen stubla N=2 konsept isteğinde konsept çağrısı N × 3'ü (0013 tekrarları) aşmaz;
    /// 4xx'te konsept başına tam 1 çağrı; bu sayfa kendi tekrarını eklemez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Retries_OnlyGateway_NoPageLevelRetry()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, wordCheck: true);
        _factory.Provider.Handler = (_, _, _) => Task.FromResult(ProviderAttempt.Failed(503));
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(2 * 3);

        _factory.Provider.Reset();
        _factory.Provider.Handler = (_, _, _) => Task.FromResult(ProviderAttempt.Failed(400));
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(2);

        _factory.Provider.Reset();
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Task.FromResult(ProviderAttempt.Failed(500)) : Duplicates();
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 5 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        _factory.Provider.CallsOf(CallRole.Topics).Count.ShouldBeLessThanOrEqualTo(3);
        _factory.Provider.CallsOf(CallRole.Dedupe).Count.ShouldBe(0);
    }

    /// <summary>
    /// AC-20 (H-3, kasıtlı bozma hedefi): "yarım kaldı" ya da ayrıştırılamayan çıktı kaydedilmez, "üretilemedi" bildirilir (yarım kalanda
    /// çıktı sınırı mesajı) ve aynı konsept için otomatik yeni çağrı yapılmaz; istenen çıktı sınırı katalog limitidir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Truncated_Or_Malformed_NotSaved_NoAutoRetry()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, role, n) => role != CallRole.Concept ? Duplicates() : n == 1
            ? Task.FromResult(ProviderAttempt.Completed(ConceptJson("Yarım", [60]), 100, 64000, truncated: true))
            : Ok("Bu bir konsept değil {bozuk");

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);

        var failures = job.GetProperty("failures").EnumerateArray().Select(f => f.GetProperty("message").GetString()).ToList();
        failures.Count.ShouldBe(2);
        failures[0].ShouldBe("Bu süre için seçili modelin çıktı sınırı yetmedi; daha yüksek sınırlı bir model seçin");
        job.GetProperty("status").GetString().ShouldBe("failed");
        _factory.Concepts.Rows.Count(c => c.ProfileId == profile).ShouldBe(0);
        var calls = _factory.Provider.CallsOf(CallRole.Concept);
        calls.Count.ShouldBe(2);
        calls.ShouldAllBe(c => c.MaxOutputTokens == 64000); // Sonnet 4.5 katalog limiti.
    }

    /// <summary>
    /// AC-21 (G-3, kasıtlı bozma hedefi): 10 konseptlik partide 3. konseptin çağrısı sürerken Durdur → 3. konsept tamamlanıp kaydedilir,
    /// 4–10 için çağrı yapılmaz, durum "durduruldu"; Günlük'e "durduruldu" olayı (AC-23).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stop_DuringThird_CompletesThird_NoMoreCalls()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var third = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        _factory.Provider.Handler = async (_, role, n) =>
        {
            if (role == CallRole.Topics)
            {
                return ProviderAttempt.Completed(TopicsJson([.. Enumerable.Range(1, 10).Select(i => ($"Konu {i}", "Özet."))]), 10, 10);
            }

            if (role == CallRole.Concept && n == 3)
            {
                third.TrySetResult();
                await release.Task;
            }

            return role == CallRole.Concept ? ProviderAttempt.Completed(ConceptJson($"K{n}", [60]), 10, 10) : ProviderAttempt.Completed("""{"duplicates":[]}""", 1, 1);
        };
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 10 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        var ids = (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).ToArray();
        ids.Length.ShouldBe(10);

        (await PostAsync(client, "/api/concepts/from-topics", new { profileId = profile, suggestionIds = ids }, ct)).EnsureSuccessStatusCode();
        await third.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        var running = (await WorkAsync(client, profile, ct)).GetProperty("topicJob");
        running.GetProperty("status").GetString().ShouldBe("running");
        running.GetProperty("processed").GetInt32().ShouldBe(2);
        (await PostAsync(client, "/api/concepts/jobs/stop", new { profileId = profile, lane = "topic" }, ct)).EnsureSuccessStatusCode();
        release.SetResult();
        var job = await WaitJobAsync(client, profile, "topicJob", ct);

        job.GetProperty("status").GetString().ShouldBe("stopped");
        job.GetProperty("succeeded").GetInt32().ShouldBe(3);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(3);
        _factory.Concepts.Rows.Count(c => c.ProfileId == profile).ShouldBe(3);
        _factory.Events.Events.ShouldContain(e => e.ProfileId == profile && e.Message == "Konsept üretimi durduruldu: 3/10");

        // Review r1 M-4: üretilmeyen 7 öneri listeye geri döner (otomatik tekrar yok, kullanıcı elle yeniden seçebilir).
        var back = (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ToList();
        back.ShouldBe([.. Enumerable.Range(4, 7).Select(i => $"Konu {i}")]);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(3);
    }

    /// <summary>
    /// Review r1 M-2: kolon sınırını aşan model çıktısı (500+ karakter başlık) işi düşürmez: konsept "üretilemedi", öneri "biçim dışı"
    /// sayılır; diğerleri kaydedilir, ek çağrı yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ReviewR1_M2_OverlongOutput_NotSaved_BatchSurvives()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var longTitle = new string('a', 501);
        _factory.Provider.Handler = (_, role, n) => role switch
        {
            CallRole.Topics => Ok(TopicsJson((longTitle, "Özet."), ("Normal konu", "Özet."))),
            CallRole.Concept => Ok(ConceptJson(n == 1 ? longTitle : "Normal konsept", [60])),
            _ => Duplicates(),
        };

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        var job = await WaitJobAsync(client, profile, "ideaJob", ct);
        job.GetProperty("succeeded").GetInt32().ShouldBe(1);
        job.GetProperty("failures").GetArrayLength().ShouldBe(1);
        _factory.Concepts.Rows.Where(c => c.ProfileId == profile).Select(c => c.Title).ShouldBe(["Normal konsept"]);

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 2 }, ct)).EnsureSuccessStatusCode();
        var topics = await WaitJobAsync(client, profile, "topicJob", ct);
        topics.GetProperty("formatRejected").GetInt32().ShouldBe(1);
        topics.GetProperty("succeeded").GetInt32().ShouldBe(1);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(2);
    }

    /// <summary>
    /// Review r2 (M-4'ün hata yolu): seçilen konunun çağrısı hata verirse ya da üretim istisnayla kesilirse kaydedilmeyen öneriler listeye
    /// döner; ek çağrı yapılmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ReviewR2_FailedOrCrashedTopics_ReturnToList()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        _factory.Provider.Handler = (_, role, n) => role switch
        {
            CallRole.Topics => Ok(TopicsJson(("Konu A", "Özet."), ("Konu B", "Özet."), ("Konu C", "Özet."))),
            CallRole.Concept when n == 1 => Ok(ConceptJson("A", [60])),
            CallRole.Concept when n == 2 => Task.FromResult(ProviderAttempt.Failed(400)),
            CallRole.Concept => throw new InvalidOperationException("Beklenmeyen hata"),
            _ => Duplicates(),
        };
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 3 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        var ids = (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).ToArray();

        (await PostAsync(client, "/api/concepts/from-topics", new { profileId = profile, suggestionIds = ids }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);

        (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("title").GetString())
            .ShouldBe(["Konu B", "Konu C"]);
        _factory.Concepts.Rows.Where(c => c.ProfileId == profile).Select(c => c.Title).ShouldBe(["Konu A"]);
        _factory.Provider.CallsOf(CallRole.Concept).Count.ShouldBe(3);
    }

    /// <summary>Review r1 M-3: üretim sürerken profil (henüz konsepti olmasa da) silinemez; iş bitince silinebilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ReviewR1_M3_RunningJob_ProfileNotDeletable()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var release = new TaskCompletionSource();
        _factory.Provider.Handler = async (_, _, _) =>
        {
            await release.Task;
            return ProviderAttempt.Failed(400);
        };

        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).EnsureSuccessStatusCode();
        (await client.DeleteAsync(new Uri($"/api/profiles/{profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _factory.Profiles.Rows.ShouldContain(p => p.Id == profile);
        release.SetResult();
        await WaitJobAsync(client, profile, "ideaJob", ct);
        (await client.DeleteAsync(new Uri($"/api/profiles/{profile}", UriKind.Relative), ct)).EnsureSuccessStatusCode();
    }

    /// <summary>AC-21: konudan ve fikirden üretim aynı anda çalışır, ilerlemeleri ayrı görünür; aynı şeritte ikinci iş reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lanes_RunConcurrently()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct);
        var release = new TaskCompletionSource();
        _factory.Provider.Handler = async (_, role, _) =>
        {
            await release.Task;
            return role switch
            {
                CallRole.Topics => ProviderAttempt.Completed(TopicsJson(("Konu", "Özet.")), 1, 1),
                CallRole.Concept => ProviderAttempt.Completed(ConceptJson("Fikir konsepti", [60]), 1, 1),
                _ => ProviderAttempt.Completed("""{"duplicates":[]}""", 1, 1),
            };
        };

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 }, ct)).EnsureSuccessStatusCode();
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).EnsureSuccessStatusCode();
        var work = await WorkAsync(client, profile, ct);
        work.GetProperty("topicJob").GetProperty("status").GetString().ShouldBe("running");
        work.GetProperty("ideaJob").GetProperty("status").GetString().ShouldBe("running");
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 1 }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        release.SetResult();

        (await WaitJobAsync(client, profile, "topicJob", ct)).GetProperty("status").GetString().ShouldBe("completed");
        (await WaitJobAsync(client, profile, "ideaJob", ct)).GetProperty("status").GetString().ShouldBe("completed");
    }

    /// <summary>AC-22 (G-4): iş başlatan istek bittikten sonra da sürer; başka bir istemci (sayfa yeniden açıldı) güncel ilerlemeyi görür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Job_ContinuesWithoutClient_ProgressReadable()
    {
        var ct = TestContext.Current.CancellationToken;
        var release = new TaskCompletionSource();
        Guid profile;
        string? token;
        using (var first = await _factory.CreateUserClientAsync(ct))
        {
            profile = await ReadyProfileAsync(first, ct);
            token = first.DefaultRequestHeaders.Authorization!.Parameter;
            _factory.Provider.Handler = async (_, role, n) =>
            {
                if (role == CallRole.Concept && n == 2)
                {
                    await release.Task;
                }

                return role == CallRole.Concept ? ProviderAttempt.Completed(ConceptJson($"K{n}", [60]), 1, 1) : ProviderAttempt.Completed("""{"duplicates":[]}""", 1, 1);
            };
            (await PostAsync(first, "/api/concepts/from-idea", new { profileId = profile, idea = "Fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        }

        using var reopened = _factory.CreateClient();
        reopened.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        JsonElement job;
        do
        {
            job = (await WorkAsync(reopened, profile, ct)).GetProperty("ideaJob");
            await Task.Delay(20, ct);
        }
        while (job.GetProperty("processed").GetInt32() < 1 && DateTimeOffset.UtcNow < deadline);

        job.GetProperty("status").GetString().ShouldBe("running");
        job.GetProperty("processed").GetInt32().ShouldBe(1);
        release.SetResult();
        (await WaitJobAsync(reopened, profile, "ideaJob", ct)).GetProperty("succeeded").GetInt32().ShouldBe(2);
    }

    /// <summary>
    /// AC-23 (Ç-7): parti bitince, kısmi hatada ve tamamen başarısız olunca Günlük'e tür, profil adı ve sayıları içeren, hiçbir başlık ya da
    /// metin içermeyen bir olay yazılır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_EventOnFinishStopFail_NoTitles()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ReadyProfileAsync(client, ct, name: "Günlük Kanalı");
        _factory.Provider.Handler = (_, role, n) => role switch
        {
            CallRole.Topics => Ok(TopicsJson(("Gizli başlık bir", "Gizli özet."), ("Gizli başlık iki", "Gizli özet."))),
            CallRole.Concept => n == 1 ? Task.FromResult(ProviderAttempt.Failed(400)) : Ok(ConceptJson($"Gizli konsept {n}", [60])),
            _ => Duplicates(),
        };

        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 2 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "topicJob", ct);
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Gizli fikir", count = 3 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);
        _factory.Provider.Handler = (_, _, _) => Task.FromResult(ProviderAttempt.Failed(400));
        (await PostAsync(client, "/api/concepts/from-idea", new { profileId = profile, idea = "Gizli fikir", count = 2 }, ct)).EnsureSuccessStatusCode();
        await WaitJobAsync(client, profile, "ideaJob", ct);

        var events = _factory.Events.Events.Where(e => e.ProfileId == profile).OrderBy(e => e.OccurredAt).ToList();
        events.Select(e => e.Message).ShouldBe(["Konu önerisi: 2 öneri", "Konsept üretimi: 2/3 tamamlandı, 1 hata", "Konsept üretimi başarısız: 0/2"]);
        events.ShouldAllBe(e => e.ProfileName == "Günlük Kanalı" && e.StartedAt != null && e.EndedAt != null);
        events.ShouldAllBe(e => !e.Message.Contains("Gizli", StringComparison.Ordinal));
        events.Select(e => e.Type).ShouldBe(["concepts.topics", "concepts.generation", "concepts.generation"]);
    }

    private static string Section(string system, string tag)
    {
        var start = system.IndexOf("<" + tag + ">", StringComparison.Ordinal);
        var end = system.IndexOf("</" + tag + ">", StringComparison.Ordinal);
        return start < 0 || end < start ? string.Empty : system[start..end];
    }

    private void Seed(Guid userId, Guid profile, string title, ConceptSource source, ConceptStatus status, DateTimeOffset? at = null) =>
        _factory.Concepts.AddAsync(
            new Concept
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ProfileId = profile,
                Title = title,
                TitleKey = TitleKey.Of(title, "tr"),
                Source = source,
                Status = status,
                Language = "tr",
                Format = "portrait",
                TargetDurationSeconds = 60,
                Scenes = [new ConceptScene("S", "V", "N", 60)],
                CreatedAt = at ?? DateTimeOffset.UtcNow,
                UpdatedAt = at ?? DateTimeOffset.UtcNow,
            },
            CancellationToken.None).GetAwaiter().GetResult();
}
