using System.Net.Http.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Production.Domain.Entities;
using Shouldly;
using static Konsept.Api.Tests.Support.ConceptTestKit;

namespace Konsept.Api.Tests;

/// <summary>
/// Silinen videonun konu önerisine etkisi (spec 0019 AC-7, AC-8): Silinenler'deki videonun konsept başlığı aynı profilin "Konudan üret"
/// talimatında <c>disliked_topics</c> ("beğenilmedi") olarak yer alır, başka profilde yer almaz; kalıcı silinince bu girdiden çıkar; bağlı konsept
/// her durumda "Video oldu" kalır ve konusu tekrar önerilmez (BR-4). Gerçek Concepts ve Production servisleri, sahte sağlayıcı (gerçek LLM yok),
/// bellek depoları.
/// </summary>
public sealed class VideoTrashSignalTests : IClassFixture<ConceptsApiFactory>
{
    private static readonly ConceptScene[] Scenes =
    [
        new("Açılış", "Surlar görünür.", "Bizans'ın son sabahı başlıyor.", 30),
        new("Son", "Şehir sessizleşir.", "Bir çağ kapanıyor.", 30),
    ];

    private readonly ConceptsApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="VideoTrashSignalTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public VideoTrashSignalTests(ConceptsApiFactory factory)
    {
        _factory = factory;
        _factory.Provider.Reset();
    }

    /// <summary>
    /// AC-8: silinen videonun başlığı aynı profilin konu talimatında <c>disliked_topics</c> içinde (seçilen değil); aynı kullanıcının başka
    /// profilinin talimatında yok; kalıcı silinen videonun başlığı <c>disliked_topics</c>'te yok (blok hiç yazılmaz).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_NegativeSignal_ProfileScoped_LeavesOnPurge()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var trashed = await ReadyProfileAsync(client, ct, name: "Silinen kanal");
        var other = await ReadyProfileAsync(client, ct, name: "Başka kanal");
        var purged = await ReadyProfileAsync(client, ct, name: "Kalıcı kanal");
        var user = UserOf(trashed);
        await VideoAsync(client, user, trashed, "Beğenilmeyen kuşatma", ct);
        var gone = await VideoAsync(client, user, purged, "Kalıcı silinen kuşatma", ct);
        (await _factory.Videos.PurgeAsync(user, gone, ct)).ShouldBeTrue(); // Kalıcı silmenin Api yolu VideoTrashTests'te.

        var inProfile = await TopicSystemAsync(client, trashed, ("Taze konu", "Yeni."), ct);
        Section(inProfile, "disliked_topics").ShouldContain("Beğenilmeyen kuşatma");
        Section(inProfile, "chosen_topics").ShouldNotContain("Beğenilmeyen kuşatma");
        inProfile.ShouldContain("disliked_topics are topics whose videos the user deleted");

        (await TopicSystemAsync(client, other, ("Taze konu", "Yeni."), ct)).ShouldNotContain("Beğenilmeyen kuşatma");

        var afterPurge = await TopicSystemAsync(client, purged, ("Taze konu", "Yeni."), ct);
        afterPurge.ShouldNotContain("<disliked_topics>");
        Section(afterPurge, "chosen_topics").ShouldContain("Kalıcı silinen kuşatma"); // BR-4 konsept üzerinden sürer.
    }

    /// <summary>
    /// AC-7: video silindikten ve kalıcı silindikten sonra bağlı konsept "Video oldu" kalır ve aynı profilde konusu (model önerse bile)
    /// tekrar önerilmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_ConceptStaysVideoDone_TopicNotSuggested()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var deletedProfile = await ReadyProfileAsync(client, ct, name: "Sil kanal");
        var purgedProfile = await ReadyProfileAsync(client, ct, name: "Kalıcı kanal");
        var user = UserOf(deletedProfile);
        var deleted = await VideoAsync(client, user, deletedProfile, "Silinen konu", ct);
        var purged = await VideoAsync(client, user, purgedProfile, "Kalıcı konu", ct);
        (await _factory.Videos.PurgeAsync(user, purged, ct)).ShouldBeTrue();

        foreach (var (profile, title) in new[] { (deletedProfile, "Silinen konu"), (purgedProfile, "Kalıcı konu") })
        {
            await TopicSystemAsync(client, profile, (title, "Tekrar."), ct, ("Başka taze konu", "Yeni."));
            var job = await WaitJobAsync(client, profile, "topicJob", ct);
            job.GetProperty("duplicates").GetInt32().ShouldBe(1);
            (await WorkAsync(client, profile, ct)).GetProperty("suggestions").EnumerateArray()
                .Select(s => s.GetProperty("title").GetString()).ShouldBe(["Başka taze konu"]);
        }

        _factory.Concepts.Rows.Single(c => c.Id == deleted).Status.ShouldBe(ConceptStatus.VideoReady);
        _factory.Concepts.Rows.Single(c => c.Id == purged).Status.ShouldBe(ConceptStatus.VideoReady);
    }

    private static string Section(string system, string tag)
    {
        var start = system.IndexOf("<" + tag + ">", StringComparison.Ordinal);
        var end = system.IndexOf("</" + tag + ">", StringComparison.Ordinal);
        return start < 0 || end < start ? string.Empty : system[start..end];
    }

    // Konu önerisi işini başlatır, bitmesini bekler ve modele giden sistem talimatını döndürür.
    private async Task<string> TopicSystemAsync(
        HttpClient client, Guid profile, (string Title, string Summary) topic, CancellationToken ct, params (string Title, string Summary)[] more)
    {
        var before = _factory.Provider.CallsOf(CallRole.Topics).Count;
        _factory.Provider.Handler = (_, role, _) => role == CallRole.Topics ? Ok(TopicsJson([topic, .. more])) : Duplicates();
        (await PostAsync(client, "/api/concepts/topics", new { profileId = profile, count = 1 + more.Length }, ct)).EnsureSuccessStatusCode();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (_factory.Provider.CallsOf(CallRole.Topics).Count == before && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(25, ct);
        }

        await WaitJobAsync(client, profile, "topicJob", ct);
        return _factory.Provider.CallsOf(CallRole.Topics)[before].System!;
    }

    // Konudan gelmiş, "Video oldu" konsept + seçildi öneri kaydı + tamamlanmış video; video Api'den silinir (spec 0019 Sil).
    private async Task<Guid> VideoAsync(HttpClient client, Guid user, Guid profile, string title, CancellationToken ct)
    {
        var id = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow.AddHours(-2);
        await _factory.Concepts.AddAsync(
            new Concept
            {
                Id = id,
                UserId = user,
                ProfileId = profile,
                Title = title,
                TitleKey = TitleKey.Of(title, "tr"),
                Source = ConceptSource.Topic,
                Status = ConceptStatus.VideoReady,
                Language = "tr",
                Format = "portrait",
                TargetDurationSeconds = 60,
                Scenes = Scenes,
                CreatedAt = now,
                UpdatedAt = now,
            },
            ct);
        await _factory.Suggestions.AddRangeAsync(
            [new TopicSuggestion { Id = Guid.CreateVersion7(), UserId = user, ProfileId = profile, Title = title, TitleKey = TitleKey.Of(title, "tr"), Summary = "Özet.", State = SuggestionState.Selected, CreatedAt = now }],
            ct);
        var version = Guid.CreateVersion7();
        await _factory.Videos.AddAsync(
            new ProducedVideo
            {
                Id = version,
                UserId = user,
                ProfileId = profile,
                ConceptId = id,
                JobId = Guid.CreateVersion7(),
                Title = title,
                ConceptText = "{}",
                Engine = "local/hyperframes",
                FileName = "video-" + version.ToString("N") + ".mp4",
                StartedAt = now,
                EndedAt = now.AddMinutes(5),
            },
            ct);
        (await client.PostAsJsonAsync($"/api/produced/{id}/delete", new { }, ct)).EnsureSuccessStatusCode();
        return id;
    }

    private Guid UserOf(Guid profile) => _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
}
