using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Production.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Üretim hattı uçtan uca (spec 0017): gerçek Production, Concepts, AiGateway, AgentGateway (kayıt + komut seçimi) ve Identity; bellek
/// depoları, sahte sağlayıcı (gerçek LLM yok) ve sahte ajan kanalı (gerçek ajan/render yok). Testler sırayla koşar; her test kendi
/// kullanıcısıyla çalışır ve sahteleri sıfırlar.
/// </summary>
public sealed partial class ProductionPipelineTests : IClassFixture<PipelineApiFactory>
{
    private const string Sonnet = "anthropic/claude-sonnet-4-5";
    private const string Html = "<!doctype html><html><body><div>sahne</div></body></html>";

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);

    private static readonly ConceptScene[] Scenes =
    [
        new("Açılış", "Surlar görünür.", "Bizans'ın son sabahı başlıyor.", 30),
        new("Son", "Şehir sessizleşir.", "Bir çağ kapanıyor.", 30),
    ];

    private readonly PipelineApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ProductionPipelineTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ProductionPipelineTests(PipelineApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-3, AC-4, AC-6, AC-8, AC-18, AC-21, AC-23, AC-29: görsel ve ses kapalı iki iş sırayla tamamlanır — işler Tamamlandı, konseptler Video
    /// oldu; her işte tam 1 analiz + sahne başına 1 kod çağrısı, görsel çağrısı ve görsel/ses komutu yok (aşamalar "atlandı"); aynı anda hiçbir
    /// zaman iki yerel aşama çalışmaz; iş maliyeti harcama kayıtlarına eşit; Günlük'te başladı/hazırlandı olayları (başlık, süre, saat aralığı);
    /// sürüm kaydı ve Üretilenler (en yeni üstte, başka profilinki yok).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_Completes_ConceptVideoReady_NextStarts()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        var concurrent = 0;
        var maxConcurrent = 0;
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            var now = Interlocked.Increment(ref concurrent);
            InterlockedMax(ref maxConcurrent, now);
            await Task.Delay(30, token);
            Interlocked.Decrement(ref concurrent);
            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        var first = Seed(profile, user, "Kuşatma");
        var second = Seed(profile, user, "Düşüş");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);

        await Eventually.WithinAsync(() => Task.FromResult(_factory.Jobs.Rows.Count(j => j.UserId == user && j.Status == ProductionJobStatus.Completed) == 2), Limit, ct);

        Concept(first).Status.ShouldBe(ConceptStatus.VideoReady);
        Concept(second).Status.ShouldBe(ConceptStatus.VideoReady);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(2);
        _factory.Provider.CallsOf(CallRole.Code).Count.ShouldBe(4);
        _factory.Provider.CallsOf(CallRole.Image).Count.ShouldBe(0);
        _factory.Agent.Count("RunStage", AgentStageCommand.Images).ShouldBe(0);
        _factory.Agent.Count("RunStage", AgentStageCommand.Voice).ShouldBe(0);
        _factory.Agent.Count("RunStage", AgentStageCommand.Render).ShouldBe(2);
        _factory.Agent.Count("RunStage", AgentStageCommand.Merge).ShouldBe(2);
        maxConcurrent.ShouldBe(1);
        var job = _factory.Jobs.Rows.Single(j => j.ConceptId == first);
        job.Progress!.Stages.Select(s => s.State).ShouldBe(
            [StageState.Done, StageState.Done, StageState.Skipped, StageState.Skipped, StageState.Done, StageState.Done]);
        job.Progress.Stages.Single(s => s.Stage == PipelineStage.Render).Device.ShouldBe("gpu");

        var spend = _factory.Spend.Records.Where(r => r.UserId == user).Sum(r => r.CostUsd);
        spend.ShouldBeGreaterThan(0m);
        _factory.Jobs.Rows.Where(j => j.UserId == user).Sum(j => j.Progress!.CostUsd).ShouldBe(spend);

        var events = _factory.Events.Events.Where(e => e.UserId == user && e.Type == "production.video").Select(e => e.Message).ToList();
        events.Count(m => m.StartsWith("Video üretimi başladı: Kuşatma", StringComparison.Ordinal)).ShouldBe(1);
        events.ShouldContain(m => m.StartsWith("Video hazırlandı: Kuşatma — ", StringComparison.Ordinal) && TimeRange().IsMatch(m) && m.Contains(" sn", StringComparison.Ordinal));

        var videos = await client.GetFromJsonAsync<JsonElement>($"/api/production/videos?profileId={profile}", ct);
        videos.EnumerateArray().Select(v => v.GetProperty("title").GetString()).ShouldBe(["Düşüş", "Kuşatma"]);
        var record = _factory.Videos.Rows.Single(v => v.ConceptId == first);
        record.AudioModel.ShouldBeNull();
        record.ImageModel.ShouldBeNull();
        record.Subtitles.ShouldBeFalse(); // Ses kapalıyken altyazı uygulanmaz.
        record.ProgressBar.ShouldBeTrue();
        record.CodeModel.ShouldBe(Sonnet);
        record.Engine.ShouldBe("local/hyperframes");
        record.AiVoiceNotice.ShouldBeFalse();
        record.ConceptText.ShouldContain("Bizans'ın son sabahı başlıyor.");
        record.EndedAt.ShouldBeGreaterThanOrEqualTo(record.StartedAt);

        var (otherClient, otherProfile, _, _) = await SetupAsync(ct, reset: false);
        (await otherClient.GetFromJsonAsync<JsonElement>($"/api/production/videos?profileId={otherProfile}", ct)).GetArrayLength().ShouldBe(0);
        (await otherClient.GetAsync(new Uri($"/api/production/videos?profileId={profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// AC-1, AC-5, AC-22: işe sabitlenen ayar kullanılır — render sırasında profil ayarı (ilerleme çubuğu) değişse de çalışan işin birleştirmesi
    /// eski ayarla, sıradaki iş yeni ayarla üretilir; ses açıkken altyazı birleştirme komutundadır (LLM çağrısı yok: yalnızca analiz/kod);
    /// Supertonic ile AI ses notu kaydedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_SettingChangedWhileRunning_UsesFixed()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: "local/supertonic-3", progressBar: true);
        var gate = new TaskCompletionSource();
        var renders = 0;
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Render && Interlocked.Increment(ref renders) == 1)
            {
                await gate.Task.WaitAsync(token);
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        var first = Seed(profile, user, "Birinci");
        var second = Seed(profile, user, "İkinci");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Volatile.Read(ref renders) == 1), Limit, ct);

        (await client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", SettingsBody(audio: "local/supertonic-3", progressBar: false), ct)).EnsureSuccessStatusCode();
        gate.SetResult();
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Videos.Rows.Count(v => v.UserId == user) == 2), Limit, ct);

        var merges = _factory.Agent.Sent.Where(s => s.Method == "RunStage" && s.Payload.Contains("\"stage\":\"merge\"", StringComparison.Ordinal)).Select(s => s.Payload).ToList();
        merges.Count.ShouldBe(2);
        merges[0].ShouldContain("\"progressBar\":true");
        merges[0].ShouldContain("\"subtitles\":true");
        merges[1].ShouldContain("\"progressBar\":false");
        _factory.Provider.Calls.Select(ScriptedProviderClient.RoleOf).Distinct().Order().ShouldBe([CallRole.Analysis, CallRole.Code]);
        _factory.Agent.Count("RunStage", AgentStageCommand.Voice).ShouldBe(2);
        _factory.Videos.Rows.Single(v => v.ConceptId == first).ProgressBar.ShouldBeTrue();
        _factory.Videos.Rows.Single(v => v.ConceptId == second).ProgressBar.ShouldBeFalse();
        _factory.Videos.Rows.ShouldAllBe(v => v.UserId != user || (v.AiVoiceNotice && v.Subtitles));
    }

    /// <summary>
    /// AC-9: görsel aşamasında Durdur → iş Durduruldu, ajandaki aşama kesilir, sıradaki iş başlamaz; Devam et → analiz ve kod yeniden çalışmaz
    /// (online çağrı sayısı aynı), görsel aşaması baştan, kalanlar sırayla ve ardından sıradaki iş.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_StopInImages_ResumeRunsOnlyRemaining()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, image: "local/z-image-turbo");
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Images)
            {
                await Task.Delay(Timeout.Infinite, token);
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        var first = Seed(profile, user, "Görselli");
        var second = Seed(profile, user, "Sonraki");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Agent.Count("RunStage", AgentStageCommand.Images) == 1), Limit, ct);

        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Agent.Count("CancelStage") == 1), Limit, ct);
        await Eventually.WithinAsync(() => Task.FromResult(Stage(first, PipelineStage.Images) == StageState.Pending), Limit, ct);
        Job(first).Status.ShouldBe(ProductionJobStatus.Stopped);
        Job(second).Status.ShouldBe(ProductionJobStatus.Queued);
        var online = _factory.Provider.Calls.Count;
        online.ShouldBe(3); // 1 analiz + 2 kod.

        _factory.Agent.Stage = FakeAgentChannel.DefaultStage;
        (await client.PostAsJsonAsync($"/api/production/jobs/{Job(first).Id}/resume", new { }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Job(second).Status == ProductionJobStatus.Completed), Limit, ct);

        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(2); // Yalnızca ikinci işin analizi.
        _factory.Provider.CallsOf(CallRole.Code).Count.ShouldBe(4);
        _factory.Agent.Count("RunStage", AgentStageCommand.Images).ShouldBe(3); // Kesilen + baştan + ikinci iş.
        Job(first).Status.ShouldBe(ProductionJobStatus.Completed);
    }

    /// <summary>
    /// AC-10: bir online çağrı sürerken Durdur → çağrı tamamlanır ve harcaması kaydedilir (işin maliyetine de eklenir), ama durdurmadan sonra
    /// yeni hiçbir online çağrı başlamaz (sahte sağlayıcının çağrı sayısı).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_StopDuringOnlineCall_CallCompletes_NoNewCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        var gate = new TaskCompletionSource();
        _factory.Provider.Handler = async (call, role, index) =>
        {
            if (role == CallRole.Analysis)
            {
                await gate.Task;
            }

            return await Reply(call, role, index);
        };
        var concept = Seed(profile, user, "Yarıda");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Provider.CallsOf(CallRole.Analysis).Count == 1), Limit, ct);

        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        gate.SetResult();
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Spend.Records.Count(r => r.UserId == user) == 1), Limit, ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Progress?.CostUsd > 0m), Limit, ct);
        await Task.Delay(500, ct);

        _factory.Provider.Calls.Count.ShouldBe(1);
        Job(concept).Status.ShouldBe(ProductionJobStatus.Stopped);
    }

    /// <summary>
    /// AC-11, AC-14: ajan çevrimdışıyken online aşamalar tamamlanır ve iş ilk yerel aşamada Ajan bekleniyor olur ("Ajan bağlı değil": ajan
    /// bağlı değil bilgisi); sıradaki iş başlamaz (online çağrısı yok). Durdurulup × ile çıkarılan iş, metni değişmeden yeniden sıraya alınınca
    /// analiz/kod yeniden çalışmaz; metni değiştirilen konsept baştan çalışır. Ajan bağlanınca iş kendiliğinden sürer.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_Requeue_ReusesOutputs_UnlessTextChanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, agent: false);
        var same = Seed(profile, user, "Aynı Metin");
        var changed = Seed(profile, user, "Değişen Metin");
        await EnqueueAndStartAsync(client, profile, [same, changed], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(same).Status == ProductionJobStatus.WaitingForAgent), Limit, ct);

        var state = await StateAsync(client, profile, ct);
        state.GetProperty("agentConnected").GetBoolean().ShouldBeFalse();
        Statuses(state).ShouldBe(["waiting_for_agent", "queued"]);
        _factory.Provider.Calls.Count.ShouldBe(3); // Yalnızca ilk iş: 1 analiz + 2 kod.
        Stage(same, PipelineStage.Code).ShouldBe(StageState.Done);
        Stage(same, PipelineStage.Render).ShouldBe(StageState.Pending);
        _factory.Agent.Sent.ShouldBeEmpty();

        // Durdur, çıkar, aynı metinle yeniden sıraya al.
        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/production/jobs/{Job(same).Id}/remove", new { }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/production/jobs/{Job(changed).Id}/remove", new { }, ct)).EnsureSuccessStatusCode();
        await RegisterAgentAsync(user, ct);
        await EnqueueAndStartAsync(client, profile, [same], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Concept(same).Status == ConceptStatus.VideoReady), Limit, ct);
        _factory.Provider.Calls.Count.ShouldBe(3);

        // Sıradan çıkarılan ikinci konsept de ajanla tamamlanır; metin değişikliği üçüncü konseptle: çıktılar oluşur (yerel aşama hata verir),
        // iş çıkarılır, metin 0015 ile değiştirilir, yeniden sıraya alınınca analiz baştan çalışır.
        await EnqueueAndStartAsync(client, profile, [changed], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Concept(changed).Status == ConceptStatus.VideoReady), Limit, ct);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(2);
        var newProfileConcept = Seed(profile, user, "Düzenlenecek");
        _factory.Agent.Stage = (conn, cmd, token) => Task.FromResult(new AgentStageReply(false, AgentStageReply.Failed));
        await EnqueueAndStartAsync(client, profile, [newProfileConcept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(newProfileConcept).Status == ProductionJobStatus.Failed), Limit, ct);
        (await client.PostAsJsonAsync($"/api/production/jobs/{Job(newProfileConcept).Id}/remove", new { }, ct)).EnsureSuccessStatusCode();
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(3);
        (await client.PostAsJsonAsync($"/api/concepts/{newProfileConcept}/scenes", new { scenes = Edited("Yeni anlatım") }, ct)).EnsureSuccessStatusCode();
        _factory.Agent.Stage = FakeAgentChannel.DefaultStage;
        await EnqueueAndStartAsync(client, profile, [newProfileConcept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Concept(newProfileConcept).Status == ConceptStatus.VideoReady), Limit, ct);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(4); // Metin değişti → baştan.
    }

    /// <summary>
    /// AC-14: ajan bağlanınca Ajan bekleniyor işi kendiliğinden (zamanlayıcıyla) sürer, biter ve sıradaki iş başlayıp biter.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_AgentOffline_WaitsAfterOnline_ResumesOnConnect()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, agent: false);
        var first = Seed(profile, user, "Bekleyen");
        var second = Seed(profile, user, "Sıradaki");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(first).Status == ProductionJobStatus.WaitingForAgent), Limit, ct);
        await Task.Delay(500, ct);
        Job(second).Status.ShouldBe(ProductionJobStatus.Queued);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(1);

        await RegisterAgentAsync(user, ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(second).Status == ProductionJobStatus.Completed), Limit, ct);
        Job(first).Status.ShouldBe(ProductionJobStatus.Completed);
        (await StateAsync(client, profile, ct)).GetProperty("agentConnected").GetBoolean().ShouldBeTrue();
    }

    /// <summary>
    /// AC-15, AC-30: ajan render ortasında koparsa iş Ajan bekleniyor olur; kullanıcı yeni ajan eşleştirince (yeni cihaz) araçlar yeni ajana
    /// sorulur, iş render'dan devam eder (analiz/kod yeniden çalışmaz) ve tamamlanır; eski ajana bu andan sonra hiçbir komut gitmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_AgentDropsMidStage_StageRerunsOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, oldConn) = await SetupAsync(ct);
        var dropped = 0;
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Render && conn == oldConn && Interlocked.Exchange(ref dropped, 1) == 0)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>().DisconnectAsync(conn, CancellationToken.None);
                throw new IOException("bağlantı koptu");
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        var concept = Seed(profile, user, "Kopan");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.WaitingForAgent), Limit, ct);
        Stage(concept, PipelineStage.Render).ShouldBe(StageState.Pending);
        var sentToOld = _factory.Agent.Sent.Count(s => s.ConnectionId == oldConn);

        var newConn = await RegisterAgentAsync(user, ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Completed), Limit, ct);

        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(1);
        _factory.Provider.CallsOf(CallRole.Code).Count.ShouldBe(2);
        _factory.Agent.Sent.Count(s => s.ConnectionId == oldConn).ShouldBe(sentToOld);
        _factory.Agent.Sent.Where(s => s.ConnectionId == newConn).Select(s => s.Method).ShouldBe(["CheckTools", "RunStage", "RunStage"]);
    }

    /// <summary>AC-16: ajan bağlı ve kurulum eksikse iş, hiçbir online çağrı ve harcama olmadan "Ajan kurulumu eksik: HyperFrames" ile Hata olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_ToolsMissing_FailsBeforeAnyCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        _factory.Agent.Missing = ["hyperframes"];
        var concept = Seed(profile, user, "Eksik Araç");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);

        var row = (await StateAsync(client, profile, ct)).GetProperty("jobs").EnumerateArray().Single();
        row.GetProperty("error").GetString().ShouldBe("Ajan kurulumu eksik: HyperFrames");
        _factory.Provider.Calls.ShouldBeEmpty();
        _factory.Spend.Records.Where(r => r.UserId == user).ShouldBeEmpty();
    }

    /// <summary>
    /// AC-12, AC-31 (NVENC kısmı): seslendirme aşaması GPU'yu kullanamazsa iş "Ekran kartı kullanılamadı: Supertonic 3" ile Hata olur (sade,
    /// stack trace/SQL/key yok), tamamlanmış aşamalar korunur ve sıra sonraki işe geçer; Devam et yalnızca hatalı aşamadan sürer.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_Failure_PlainError_ResumeFromFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: "local/supertonic-3");
        var failOnce = 0;
        _factory.Agent.Stage = (conn, cmd, token) =>
            cmd.Stage == AgentStageCommand.Voice && Interlocked.Exchange(ref failOnce, 1) == 0
                ? Task.FromResult(new AgentStageReply(false, AgentStageReply.GpuUnavailable, "supertonic-3"))
                : FakeAgentChannel.DefaultStage(conn, cmd, token);
        var first = Seed(profile, user, "Hatalı");
        var second = Seed(profile, user, "Sağlam");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(second).Status == ProductionJobStatus.Completed), Limit, ct);

        Job(first).Status.ShouldBe(ProductionJobStatus.Failed);
        var row = (await StateAsync(client, profile, ct)).GetProperty("jobs").EnumerateArray().Single();
        var error = row.GetProperty("error").GetString()!;
        error.ShouldBe("Ekran kartı kullanılamadı: Supertonic 3");
        row.GetProperty("stages").EnumerateArray().Select(s => s.GetProperty("state").GetString()).ShouldBe(["done", "done", "skipped", "failed", "pending", "pending"]);

        (await client.PostAsJsonAsync($"/api/production/jobs/{Job(first).Id}/resume", new { }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Job(first).Status == ProductionJobStatus.Completed), Limit, ct);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(2);
        _factory.Agent.Count("RunStage", AgentStageCommand.Voice).ShouldBe(3);
    }

    /// <summary>
    /// AC-12: art arda 3 iş Hata olunca sıra durur, sonraki iş başlamaz ve uyarı görünür; aradaki başarılı iş sayacı sıfırlar.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_ThreeFailures_QueueHalts()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        _factory.Provider.Handler = (call, role, index) =>
            role == CallRole.Analysis && index != 2
                ? Task.FromResult(ProviderAttempt.Completed("biçim dışı", 10, 10))
                : Reply(call, role, index);
        var ids = Enumerable.Range(1, 6).Select(i => Seed(profile, user, "Konu " + i)).ToList();
        await EnqueueAndStartAsync(client, profile, ids, ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(ids[4]).Status == ProductionJobStatus.Failed), Limit, ct);
        await Task.Delay(500, ct);

        Job(ids[1]).Status.ShouldBe(ProductionJobStatus.Completed);
        Job(ids[5]).Status.ShouldBe(ProductionJobStatus.Queued);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(5);
        var state = await StateAsync(client, profile, ct);
        state.GetProperty("queueHalted").GetBoolean().ShouldBeTrue();
        state.GetProperty("jobs").EnumerateArray().First().GetProperty("error").GetString()
            .ShouldBe("Konsept incelenemedi: model beklenen biçimde yanıt vermedi.");
    }

    /// <summary>
    /// AC-13: render hata verince video kodu modeline hata özetiyle tam 1 düzeltme çağrısı yapılır ve harcaması kaydedilir; düzeltilen kod da
    /// başarısız olursa iş Hata olur ve ikinci düzeltme yapılmaz (kasıtlı bozma: sınır kaldırılınca kırmızı).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_RenderFails_OneFix_ThenFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        _factory.Agent.Stage = (conn, cmd, token) =>
            cmd.Stage == AgentStageCommand.Render
                ? Task.FromResult(new AgentStageReply(false, AgentStageReply.RenderFailed, "ReferenceError: x is not defined", FailedScene: 1))
                : FakeAgentChannel.DefaultStage(conn, cmd, token);
        var concept = Seed(profile, user, "Bozuk Kod");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);

        var fixes = _factory.Provider.CallsOf(CallRole.Fix);
        fixes.Count.ShouldBe(1);
        fixes[0].Prompt.ShouldContain("ReferenceError: x is not defined");
        _factory.Agent.Count("RunStage", AgentStageCommand.Render).ShouldBe(2);
        _factory.Spend.Records.Count(r => r.UserId == user && r.Purpose == CallPurpose.VideoCode).ShouldBe(4); // Analiz + 2 kod + düzeltme.
        Job(concept).Progress!.Error.ShouldBe("Video kodu videoya dönüştürülemedi (düzeltme denemesinden sonra da).");
    }

    /// <summary>
    /// AC-27: sağlayıcının key'i silinmişse iş o sağlayıcıya hiç çağrı yapmadan ve harcama olmadan "API key yok — Ayarlar" ile Hata olur; key
    /// yeniden girilip Devam et denince hatalı aşamadan sürer ve tamamlanır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_NoKey_FailsWithoutCall_ResumeAfterKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        var gate = new TaskCompletionSource();
        _factory.Agent.BeforeCheck = () => gate.Task;
        var concept = Seed(profile, user, "Keysiz");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        (await client.DeleteAsync(new Uri("/api/ai/keys/anthropic", UriKind.Relative), ct)).EnsureSuccessStatusCode();
        gate.SetResult();
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);

        Job(concept).Progress!.Error.ShouldBe("API key yok — Ayarlar");
        _factory.Provider.Calls.ShouldBeEmpty();
        _factory.Spend.Records.Where(r => r.UserId == user).ShouldBeEmpty();

        _factory.Agent.BeforeCheck = null;
        await KeyAsync(client, "anthropic", ct);
        (await client.PostAsJsonAsync($"/api/production/jobs/{Job(concept).Id}/resume", new { }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Completed), Limit, ct);
    }

    /// <summary>
    /// AC-17, AC-19: online görselli iş — görseller Pi'de kullanıcının key'iyle üretilir ve sahne sahne yalnızca görsel ajana gider; ajana giden
    /// hiçbir komutta key (ya da parçası) yok; komutlar yalnızca sahibinin ajanına gider, başka kullanıcının ajanına hiçbir şey gitmez. Video
    /// kodu çağrısının çıktı sınırı modelin katalog değeridir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_AgentCommands_NoKey_OnlyOwnerAgent()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, conn) = await SetupAsync(ct, image: "openai/gpt-image-1");
        var (_, _, otherUser, otherConn) = await SetupAsync(ct, reset: false);
        var concept = Seed(profile, user, "Online Görsel");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Completed), Limit, ct);

        _factory.Provider.CallsOf(CallRole.Image).Count.ShouldBe(2);
        _factory.Agent.Count("RunStage", AgentStageCommand.Images).ShouldBe(2);
        _factory.Agent.Sent.Where(s => s.Method == "RunStage" && s.Payload.Contains("\"stage\":\"images\"", StringComparison.Ordinal))
            .ShouldAllBe(s => s.Payload.Contains("\"imageBase64\":\"aGVsbG8=\"", StringComparison.Ordinal));
        var keys = _factory.Provider.Calls.Select(c => c.ApiKey).Distinct().ToList();
        keys.Count.ShouldBe(2);
        foreach (var key in keys)
        {
            var tail = key.Substring(key.Length - 12);
            _factory.Agent.Sent.ShouldAllBe(s => !s.Payload.Contains(key, StringComparison.Ordinal) && !s.Payload.Contains(tail, StringComparison.Ordinal));
        }

        _factory.Agent.Sent.ShouldAllBe(s => s.ConnectionId == conn);
        _factory.Agent.Sent.ShouldNotContain(s => s.ConnectionId == otherConn);
        otherUser.ShouldNotBe(user);
        var limit = new Konsept.Modules.AiGateway.Infrastructure.Catalog.EmbeddedCatalogSource().Catalog.Models.Single(m => m.Id == Sonnet).OutputTokenLimit;
        _factory.Provider.CallsOf(CallRole.Code).ShouldAllBe(c => c.MaxOutputTokens == limit);
    }

    /// <summary>
    /// AC-24, AC-25 (Pi tarafı): biten video için akış adresi yalnızca sahibine ve sahibinin ajanından açılır; başka kullanıcı 404; ajan
    /// çevrimdışıyken "Videoyu izlemek için ajan bağlı olmalı"; dosya ajanda yoksa "Video dosyası bulunamadı".
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_Stream_OwnerOnly_OfflineAndMissing()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, conn) = await SetupAsync(ct);
        var concept = Seed(profile, user, "İzlenecek");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Videos.Rows.Any(v => v.ConceptId == concept)), Limit, ct);
        var video = _factory.Videos.Rows.Single(v => v.ConceptId == concept).Id;

        var opened = await client.PostAsJsonAsync($"/api/production/videos/{video}/stream", new { }, ct);
        opened.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await opened.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("url").GetString().ShouldStartWith("http://127.0.0.1:");
        _factory.Agent.Sent.Last().ConnectionId.ShouldBe(conn);

        var (other, _, _, _) = await SetupAsync(ct, reset: false);
        (await other.PostAsJsonAsync($"/api/production/videos/{video}/stream", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        _factory.Agent.Stream = r => new AgentStreamReply(null, AgentStreamReply.NotFound);
        var missing = await client.PostAsJsonAsync($"/api/production/videos/{video}/stream", new { }, ct);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await missing.Content.ReadAsStringAsync(ct)).ShouldContain("Video dosyası bulunamadı");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>().DisconnectAsync(conn!, CancellationToken.None);
        }

        var offline = await client.PostAsJsonAsync($"/api/production/videos/{video}/stream", new { }, ct);
        offline.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await offline.Content.ReadAsStringAsync(ct)).ShouldContain("Videoyu izlemek için ajan bağlı olmalı");
    }

    /// <summary>
    /// Review r1 M-1: ajan birleştirmeyi tam Durdur anında bitirse de iş Durduruldu kalır — video kaydı, Video oldu ve sıradaki iş yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_StopDuringMerge_NoRecord_NextNotStarted()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Merge)
            {
                (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, CancellationToken.None)).EnsureSuccessStatusCode();
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, CancellationToken.None);
        };
        var first = Seed(profile, user, "Son Anda");
        var second = Seed(profile, user, "Bekleyecek");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Agent.Count("RunStage", AgentStageCommand.Merge) == 1), Limit, ct);
        await Task.Delay(500, ct);

        Job(first).Status.ShouldBe(ProductionJobStatus.Stopped);
        Job(second).Status.ShouldBe(ProductionJobStatus.Queued);
        Concept(first).Status.ShouldNotBe(ConceptStatus.VideoReady);
        _factory.Videos.Rows.ShouldNotContain(v => v.UserId == user);
    }

    /// <summary>
    /// Review r1 M-3 (H-3): ajan işleyicisi hata verirse (bağlantı kopması değil) iş "Ajan bekleniyor" döngüsüne girmez, Hata olur; ücretli
    /// online görsel yalnızca bir kez üretilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_AgentHandlerError_FailsOnce_NoPaidRetry()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, image: "openai/gpt-image-1");
        _factory.Agent.Stage = (conn, cmd, token) => throw new Microsoft.AspNetCore.SignalR.HubException("işleyici yok");
        var concept = Seed(profile, user, "Uyumsuz Ajan");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);
        await Task.Delay(TimeSpan.FromSeconds(4), ct); // Bir tarama süresinden uzun.

        _factory.Provider.CallsOf(CallRole.Image).Count.ShouldBe(1);
        Job(concept).Status.ShouldBe(ProductionJobStatus.Failed);
        Job(concept).Progress!.Error.ShouldBe("Görseller üretiliyor aşaması tamamlanamadı.");
    }

    /// <summary>
    /// Review r1 M-4 (H-3): hatta beklenmeyen bir altyapı hatası iş Üretiliyor'da takılıp ücretli aşamayı her taramada yeniden çalıştırmaz;
    /// iş sade bir açıklamayla Hata olur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pipeline_UnexpectedError_FailsJob_NoRerun()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct);
        _factory.Outputs.FailWrites = true;
        try
        {
            var concept = Seed(profile, user, "Kırık Depo");
            await EnqueueAndStartAsync(client, profile, [concept], ct);
            await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);
            await Task.Delay(TimeSpan.FromSeconds(4), ct);

            _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(1);
            Job(concept).Progress!.Error.ShouldBe("Beklenmeyen bir hata oluştu; Kaldığı yerden devam et ile yeniden deneyin.");
        }
        finally
        {
            _factory.Outputs.FailWrites = false;
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    private static Task<ProviderAttempt> Reply(ProviderCall call, CallRole role, int index) => Task.FromResult(role switch
    {
        CallRole.Analysis => ProviderAttempt.Completed(Plan(call.Prompt), 100, 50),
        CallRole.Code or CallRole.Fix => ProviderAttempt.Completed(Html, 100, 200),
        CallRole.Image => ProviderAttempt.Completed(null, null, null) with { Images = ["aGVsbG8="] },
        _ => ProviderAttempt.Completed("{}", 10, 10),
    });

    private static string Plan(string prompt)
    {
        var count = Regex.Matches(prompt, "<scene index=").Count;
        return JsonSerializer.Serialize(new
        {
            scenes = Enumerable.Range(0, count).Select(i => new { durationSeconds = 30, narration = "Anlatım " + i, imagePrompt = "Resim " + i }),
        });
    }

    private static object SettingsBody(string? image = null, string? audio = null, bool progressBar = true, bool subtitles = true) =>
        new { subtitles, progressBar, codeModel = Sonnet, codeEffort = (string?)null, engine = "local/hyperframes", imageModel = image, audioModel = audio };

    private static object[] Edited(string narration) =>
    [
        new { title = "Açılış", visual = "Surlar görünür.", narration, durationSeconds = 30 },
        new { title = "Son", visual = "Şehir sessizleşir.", narration = "Bir çağ kapanıyor.", durationSeconds = 30 },
    ];

    private static async Task KeyAsync(HttpClient client, string provider, CancellationToken ct) =>
        (await client.PutAsJsonAsync($"/api/ai/keys/{provider}", new { apiKey = (provider == "anthropic" ? "sk-ant-" : "sk-proj-") + Guid.NewGuid().ToString("N") }, ct))
            .EnsureSuccessStatusCode();

    private static async Task<JsonElement> StateAsync(HttpClient client, Guid profile, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/production/state?profileId={profile}", ct);

    private static List<string?> Statuses(JsonElement state) => [.. state.GetProperty("jobs").EnumerateArray().Select(j => j.GetProperty("status").GetString())];

    private static async Task EnqueueAndStartAsync(HttpClient client, Guid profile, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        (await client.PostAsJsonAsync("/api/production/queue", new { profileId = profile, conceptIds = ids }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
    }

    [GeneratedRegex(@"\(\d{2}:\d{2}–\d{2}:\d{2}\)")]
    private static partial Regex TimeRange();

    private async Task<(HttpClient Client, Guid Profile, Guid User, string? Conn)> SetupAsync(
        CancellationToken ct, string? image = null, string? audio = null, bool progressBar = true, bool agent = true, bool reset = true)
    {
        if (reset)
        {
            _factory.Provider.Reset();
            _factory.Provider.Handler = Reply;
            _factory.Agent.Reset();
        }

        var client = await _factory.CreateUserClientAsync(ct);
        using var created = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body("Hat Kanalı " + Guid.NewGuid().ToString("N")[..6]), ct);
        created.EnsureSuccessStatusCode();
        var profile = (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        await KeyAsync(client, "anthropic", ct);
        if (image == "openai/gpt-image-1")
        {
            await KeyAsync(client, "openai", ct);
        }

        (await client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", SettingsBody(image, audio, progressBar), ct)).EnsureSuccessStatusCode();
        var user = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        var conn = agent ? await RegisterAgentAsync(user, ct) : null;
        return (client, profile, user, conn);
    }

    private async Task<string> RegisterAgentAsync(Guid userId, CancellationToken ct)
    {
        var conn = "pipe-" + Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>()
            .RegisterAsync(conn, new AgentOwner(userId, Guid.NewGuid()), new AgentRegistration("PC", "1.0.0"), ct);
        return conn;
    }

    private Guid Seed(Guid profile, Guid user, string title)
    {
        var id = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow.AddHours(-1);
        _factory.Concepts.AddAsync(
            new Concept
            {
                Id = id,
                UserId = user,
                ProfileId = profile,
                Title = title,
                TitleKey = TitleKey.Of(title, "tr"),
                Source = ConceptSource.Idea,
                Status = ConceptStatus.Approved,
                Language = "tr",
                Format = "landscape",
                TargetDurationSeconds = 60,
                Scenes = Scenes,
                CreatedAt = now,
                UpdatedAt = now,
            },
            CancellationToken.None).GetAwaiter().GetResult();
        return id;
    }

    private Concept Concept(Guid id) => _factory.Concepts.Rows.Single(c => c.Id == id);

    private Konsept.Modules.Production.Domain.Entities.ProductionJob Job(Guid concept) =>
        _factory.Jobs.Rows.Where(j => j.ConceptId == concept).OrderByDescending(j => j.CreatedAt).First();

    private StageState Stage(Guid concept, PipelineStage stage) => Job(concept).Progress?.StateOf(stage) ?? StageState.Pending;
}
