using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Yeni sürüm ve yeniden üret uçtan uca (spec 0020): gerçek Production (hat dahil), Concepts (düzenleme döngüsü, bağlam), AiGateway (kapı,
/// harcama), AgentGateway ve Identity; bellek depoları, sahte sağlayıcı (gerçek LLM yok) ve sahte ajan kanalı (gerçek ajan/render yok).
/// Testler sırayla koşar; her test kendi kullanıcısıyla çalışır ve sahteleri sıfırlar.
/// </summary>
public sealed partial class VideoVersionTests : IClassFixture<VersionApiFactory>
{
    private const string Sonnet = "anthropic/claude-sonnet-5-5";
    private const string Html = "<!doctype html><html><body><div>sahne</div></body></html>";
    private const string Supertonic = "local/supertonic-3";
    private const string Chatterbox = "local/chatterbox-multilingual-v3";
    private const string ZImage = "local/z-image-turbo";
    private const string Flux = "local/flux2-klein-4b";
    private const string GptImage = "openai/gpt-image-2.6-flare";
    private const string Busy = "Bu video için zaten üretimde bir sürüm var.";

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);

    private static readonly ConceptScene[] Scenes =
    [
        new("Açılış", "Surlar görünür.", "Bizans'ın son sabahı başlıyor.", 30),
        new("Son", "Şehir sessizleşir.", "Bir çağ kapanıyor.", 30),
    ];

    private readonly VersionApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="VideoVersionTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public VideoVersionTests(VersionApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-2: seçenekler katalogdan — motor yalnızca HyperFrames (Remotion ajan kurulu bildirince eklenir; Revideo/Motion Canvas yok), görsel
    /// gpt-image (online) + Z-Image-Turbo + FLUX.2 klein (yerel), ses Supertonic + Chatterbox (yerel) + ElevenLabs (online); hiçbir metin/Ollama
    /// modeli yok; listede olmayan istekle gönderilse de reddedilir. Yetkisi olmayan model listelenmez ve reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_Options_ListedAndRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, image: ZImage, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Seçenekli", ct);
        var options = await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}/version-options", ct);

        Ids(options, "engines").ShouldBe(["local/hyperframes"]);
        Ids(options, "images").ShouldBe([GptImage, ZImage, Flux]);
        Ids(options, "audios").ShouldBe([Supertonic, Chatterbox, VersionApiFactory.ElevenLabs]);
        options.GetProperty("images").EnumerateArray().Single(m => m.GetProperty("id").GetString() == GptImage).GetProperty("local").GetBoolean().ShouldBeFalse();
        options.GetProperty("audios").EnumerateArray().Single(m => m.GetProperty("id").GetString() == VersionApiFactory.ElevenLabs).GetProperty("local").GetBoolean().ShouldBeFalse();
        options.GetProperty("audioModel").GetString().ShouldBe(Supertonic);
        var v1 = Versions(video)[0];
        foreach (var bad in new[] { "local/revideo", "local/motion-canvas", "ollama/llama3.1", Sonnet })
        {
            using var rejected = await client.PostAsJsonAsync($"/api/produced/{video}/version-preview", Body(v1.Id, engine: bad, image: ZImage, audio: Supertonic), ct);
            rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using (var ollamaVoice = await client.PostAsJsonAsync($"/api/produced/{video}/version-preview", Body(v1.Id, image: ZImage, audio: "ollama/llama3.1"), ct))
        {
            ollamaVoice.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using (var remotionMissing = await client.PostAsJsonAsync($"/api/produced/{video}/version-preview", Body(v1.Id, engine: "local/remotion", image: ZImage, audio: Supertonic), ct))
        {
            remotionMissing.StatusCode.ShouldBe(HttpStatusCode.BadRequest); // Kurulu değilken seçilemez.
        }

        await RegisterAgentAsync(user, Guid.NewGuid(), ct, "remotion");
        Ids(await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}/version-options", ct), "engines").ShouldBe(["local/hyperframes", "local/remotion"]);

        await using var restricted = new VersionApiFactory
        {
            UserModelPermissions = $"{Sonnet},{GptImage},local/hyperframes,{Supertonic},{Chatterbox},{ZImage}",
        };
        using var limited = await restricted.CreateUserClientAsync(ct);
        using var created = await limited.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body("Yetki Kanalı"), ct);
        var limitedProfile = (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        var limitedUser = restricted.Profiles.Rows.Single(p => p.Id == limitedProfile).UserId;
        var (limitedVideo, limitedVersion) = SeedVideo(restricted, limitedUser, limitedProfile, ZImage, Supertonic);
        var shown = await limited.GetFromJsonAsync<JsonElement>($"/api/produced/{limitedVideo}/version-options", ct);
        Ids(shown, "images").ShouldBe([GptImage, ZImage]);
        Ids(shown, "audios").ShouldBe([Supertonic, Chatterbox]);
        using var denied = await limited.PostAsJsonAsync($"/api/produced/{limitedVideo}/versions", Body(limitedVersion, image: Flux, audio: Supertonic, confirm: true), ct);
        denied.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await denied.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Bu modeli kullanma yetkiniz yok");
        restricted.Jobs.Rows.ShouldBeEmpty();
    }

    /// <summary>
    /// AC-1, AC-3, AC-4, B-122: yerel → yerel ses değişikliği — önizleme "seslendirme + birleştirme", online yok; onaysız başlar; yalnızca
    /// seslendirme ve birleştirme çalışır (analiz/kod/render yeniden çalışmaz, online çağrı 0, harcama yok), yeni sürümün ücreti 0 ("Yerel —
    /// ücretsiz"), önceki sürümün kaydı profil ayarı değişse de aynı kalır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_VoiceChange_RunsOnlyVoiceAndMerge_PreviousUntouched()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Sesli", ct);
        var v1 = Versions(video)[0];
        var calls = _factory.Provider.Calls.Count;
        var spend = SpendOf(user);
        var (voice, render, merge) = (Count(AgentStageCommand.Voice), Count(AgentStageCommand.Render), Count(AgentStageCommand.Merge));

        var preview = await PreviewAsync(client, video, Body(v1.Id, audio: Chatterbox), ct);
        Stages(preview).ShouldBe(["voice", "merge"]);
        preview.GetProperty("online").GetBoolean().ShouldBeFalse();
        (await PreviewAsync(client, video, Body(v1.Id, audio: Supertonic), ct)).GetProperty("unchanged").GetBoolean().ShouldBeTrue();
        using (var nothing = await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, audio: Supertonic, confirm: true), ct))
        {
            nothing.StatusCode.ShouldBe(HttpStatusCode.Conflict); // Hiçbir ayar değişmedi: başlatma pasif.
        }

        (await client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", SettingsBody(audio: null, progressBar: false), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);

        _factory.Provider.Calls.Count.ShouldBe(calls);
        SpendOf(user).ShouldBe(spend);
        Count(AgentStageCommand.Voice).ShouldBe(voice + 1);
        Count(AgentStageCommand.Merge).ShouldBe(merge + 1);
        Count(AgentStageCommand.Render).ShouldBe(render);
        var job = LatestJob(video);
        job.Version!.Stages.ShouldBe([PipelineStage.Voice, PipelineStage.Merge]);
        job.Progress!.Stages.Select(s => s.State).ShouldBe(
            [StageState.Done, StageState.Done, StageState.Skipped, StageState.Done, StageState.Done, StageState.Done]);
        var versions = Versions(video);
        versions[0].ShouldBe(v1); // Önceki sürümün kaydı aynen (B-122).
        versions[1].AudioModel.ShouldBe(Chatterbox);
        versions[1].CostUsd.ShouldBe(0m);
        versions[1].ConceptText.ShouldBe(v1.ConceptText);
        versions[1].FileName.ShouldNotBe(v1.FileName);
        versions[1].Subtitles.ShouldBeTrue();
        versions[1].ProgressBar.ShouldBeTrue(); // Sürüm işi profil ayarını değil taban sürümü kullanır.
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct);
        detail.GetProperty("versions")[1].GetProperty("costUsd").GetDecimal().ShouldBe(0m);
        detail.GetProperty("versions")[1].GetProperty("changes").GetString().ShouldBe("Değişti: ses motoru");
        detail.GetProperty("active").ValueKind.ShouldBe(JsonValueKind.Null);
        _factory.Agent.Sent.Last(s => s.Method == "RunStage").Payload.ShouldContain(Chatterbox);
    }

    /// <summary>
    /// AC-3, AC-5, AC-6: video motoru değişimi — önizleme "video kodu + render + birleştirme", online; onaysız 409 (iş ve çağrı yok); onayla
    /// yalnızca kod (sahne başına 1) + render + birleştirme çalışır, analiz ve ses yeniden çalışmaz; sürümün ücreti yeni harcama kayıtlarının
    /// toplamı ve aylık harcama o kadar artar.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_EngineChange_CodeRenderMerge()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic, components: ["remotion"]);
        var video = await ProduceAsync(client, profile, user, "Motorlu", ct);
        var v1 = Versions(video)[0];
        var (analysis, code) = (_factory.Provider.CallsOf(CallRole.Analysis).Count, _factory.Provider.CallsOf(CallRole.Code).Count);
        var (voice, render) = (Count(AgentStageCommand.Voice), Count(AgentStageCommand.Render));
        var month = await MonthAsync(client, ct);
        var spend = SpendOf(user);
        var body = Body(v1.Id, engine: "local/remotion", audio: Supertonic);

        var preview = await PreviewAsync(client, video, body, ct);
        Stages(preview).ShouldBe(["code", "render", "merge"]);
        preview.GetProperty("online").GetBoolean().ShouldBeTrue();
        using (var unconfirmed = await client.PostAsJsonAsync($"/api/produced/{video}/versions", body, ct))
        {
            unconfirmed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await unconfirmed.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Online model çağrısı yapılacak; başlatmak için onay gerekli.");
        }

        _factory.Jobs.Rows.Count(j => j.ConceptId == video && j.Status != ProductionJobStatus.Completed).ShouldBe(0);
        _factory.Provider.CallsOf(CallRole.Code).Count.ShouldBe(code);

        foreach (var seen in new string[]?[] { null, ["merge"] })
        {
            // Review r1 M-1: onay gördüğü aşamalara bağlı — aşamasız ya da farklı aşamalı onay iş açmaz.
            using var mismatch = await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, engine: "local/remotion", audio: Supertonic, confirm: true, stages: seen), ct);
            mismatch.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await mismatch.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Önizleme değişti; çalışacak aşamalara yeniden bakıp tekrar başlatın.");
        }

        _factory.Jobs.Rows.Count(j => j.ConceptId == video && j.Status != ProductionJobStatus.Completed).ShouldBe(0);
        (await client.PostAsJsonAsync(
            $"/api/produced/{video}/versions", Body(v1.Id, engine: "local/remotion", audio: Supertonic, confirm: true, stages: ["code", "render", "merge"]), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);

        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(analysis);
        _factory.Provider.CallsOf(CallRole.Code).Count.ShouldBe(code + 2);
        Count(AgentStageCommand.Voice).ShouldBe(voice);
        Count(AgentStageCommand.Render).ShouldBe(render + 1);
        var v2 = Versions(video)[1];
        v2.Engine.ShouldBe("local/remotion");
        var added = SpendOf(user) - spend;
        added.ShouldBeGreaterThan(0m);
        v2.CostUsd.ShouldBe(added);
        (await MonthAsync(client, ct)).ShouldBe(month + added);
        Versions(video)[0].ShouldBe(v1);
    }

    /// <summary>
    /// AC-4 (kasıtlı bozma hedefi): yerel → yerel görsel değişimi online onay istemez; render hata verirse online düzeltme çağrısı da yapılmaz —
    /// iş sade hatayla durur, sağlayıcıya hiç gidilmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_LocalOnly_RenderFails_NoFixCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, image: ZImage);
        var video = await ProduceAsync(client, profile, user, "Görselli", ct);
        var v1 = Versions(video)[0];
        var calls = _factory.Provider.Calls.Count;
        _factory.Agent.Stage = (conn, cmd, token) => cmd.Stage == AgentStageCommand.Render
            ? Task.FromResult(new AgentStageReply(false, AgentStageReply.RenderFailed, "TypeError", 0))
            : FakeAgentChannel.DefaultStage(conn, cmd, token);

        var preview = await PreviewAsync(client, video, Body(v1.Id, image: Flux), ct);
        Stages(preview).ShouldBe(["images", "render", "merge"]);
        preview.GetProperty("online").GetBoolean().ShouldBeFalse();
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, image: Flux), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Status == ProductionJobStatus.Failed), Limit, ct);

        _factory.Provider.Calls.Count.ShouldBe(calls);
        _factory.Provider.CallsOf(CallRole.Fix).Count.ShouldBe(0);
        LatestJob(video).Progress!.FixUsed.ShouldBeFalse();
        Versions(video).Count.ShouldBe(1);
    }

    /// <summary>
    /// AC-5: online görsel modeline geçiş — önizleme "online model çağrısı yapılacak"; onay olmadan iş oluşmaz, görsel çağrısı ve ajan komutu yok;
    /// onayla sahne başına bir görsel çağrısı, ücret = harcama kayıtları.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_Online_RequiresConfirmation_NoJobNoCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, image: ZImage, openAiKey: true);
        var video = await ProduceAsync(client, profile, user, "Online görsel", ct);
        var v1 = Versions(video)[0];
        var sent = _factory.Agent.Sent.Count;
        var spend = SpendOf(user);

        var preview = await PreviewAsync(client, video, Body(v1.Id, image: GptImage), ct);
        Stages(preview).ShouldBe(["images", "render", "merge"]);
        preview.GetProperty("online").GetBoolean().ShouldBeTrue();
        for (var i = 0; i < 2; i++)
        {
            using var refused = await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, image: GptImage), ct);
            refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        await Task.Delay(300, ct);
        _factory.Jobs.Rows.Count(j => j.ConceptId == video && j.Status != ProductionJobStatus.Completed).ShouldBe(0);
        _factory.Provider.CallsOf(CallRole.Image).Count.ShouldBe(0);
        _factory.Agent.Sent.Count.ShouldBe(sent);

        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, image: GptImage, confirm: true, stages: ["images", "render", "merge"]), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);
        _factory.Provider.CallsOf(CallRole.Image).Count.ShouldBe(2);
        Versions(video)[1].CostUsd.ShouldBe(SpendOf(user) - spend);
        Versions(video)[1].CostUsd.ShouldBeGreaterThan(0m);
    }

    /// <summary>
    /// AC-7 (kasıtlı bozma hedefi): aynı isteğin eşzamanlı iki gönderimi tek iş açar; Üretiliyor, Sırada ve Durduruldu sürüm varken gelen yeni
    /// istek iş oluşturmaz ve "Bu video için zaten üretimde bir sürüm var" der.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_DoubleSubmit_OneJob_ActiveBlocks()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var first = await ProduceAsync(client, profile, user, "Birinci video", ct);
        var second = await ProduceAsync(client, profile, user, "İkinci video", ct);
        var gate = new TaskCompletionSource();
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Voice)
            {
                await gate.Task.WaitAsync(token);
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        var body = Body(Versions(first)[0].Id, audio: Chatterbox);

        var both = await Task.WhenAll(
            client.PostAsJsonAsync($"/api/produced/{first}/versions", body, ct),
            client.PostAsJsonAsync($"/api/produced/{first}/versions", body, ct));
        both.Select(r => r.StatusCode).Order().ShouldBe([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        (await both.Single(r => r.StatusCode == HttpStatusCode.Conflict).Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe(Busy);
        ActiveJobs(first).Count.ShouldBe(1);
        await Eventually.WithinAsync(() => Task.FromResult(Count(AgentStageCommand.Voice) >= 3), Limit, ct);
        await ExpectBusyAsync(client, first, body, ct); // Üretiliyor.

        (await client.PostAsJsonAsync($"/api/produced/{second}/versions", Body(Versions(second)[0].Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        ActiveJobs(second).Single().Status.ShouldBe(ProductionJobStatus.Queued);
        await ExpectBusyAsync(client, second, Body(Versions(second)[0].Id, audio: Chatterbox), ct); // Sırada.

        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        ActiveJobs(first).Single().Status.ShouldBe(ProductionJobStatus.Stopped);
        await ExpectBusyAsync(client, first, body, ct); // Durduruldu.
        ActiveJobs(first).Count.ShouldBe(1);
        ActiveJobs(second).Count.ShouldBe(1);
        gate.SetResult();
        foreach (var r in both)
        {
            r.Dispose();
        }
    }

    /// <summary>
    /// AC-8: süren sürümün durumu liste öğesinde ve ayrıntıda 0006 adlarıyla (Üretiliyor + aşama); başarısız sürüm Hata ve sade neden gösterir,
    /// önceki sürüm listede kalır ve oynatılabilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_StatusBadge_ListAndDetail_FailedKeepsPrevious()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Rozetli", ct);
        var v1 = Versions(video)[0];
        var gate = new TaskCompletionSource();
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Voice && cmd.AudioModel == Chatterbox)
            {
                await gate.Task.WaitAsync(token);
                return new AgentStageReply(false, AgentStageReply.Failed);
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Progress?.StateOf(PipelineStage.Voice) == StageState.Running), Limit, ct);

        var item = (await client.GetFromJsonAsync<JsonElement>($"/api/produced?profileId={profile}", ct)).GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == video);
        item.GetProperty("status").GetString().ShouldBe("producing");
        item.GetProperty("stage").GetString().ShouldBe("voice");
        var running = await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct);
        running.GetProperty("active").GetProperty("status").GetString().ShouldBe("producing");
        running.GetProperty("active").GetProperty("stage").GetString().ShouldBe("voice");

        gate.SetResult();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Status == ProductionJobStatus.Failed), Limit, ct);
        var failed = await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct);
        failed.GetProperty("active").GetProperty("status").GetString().ShouldBe("failed");
        var error = failed.GetProperty("active").GetProperty("error").GetString()!;
        error.ShouldBe("Seslendirme yapılıyor aşaması tamamlanamadı.");
        error.ShouldNotContain("Exception");
        failed.GetProperty("versions").GetArrayLength().ShouldBe(1);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced?profileId={profile}", ct)).GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetGuid() == video).GetProperty("status").GetString().ShouldBe("failed");
        (await client.PostAsJsonAsync($"/api/production/videos/{v1.Id}/stream", new { }, ct)).EnsureSuccessStatusCode();
    }

    /// <summary>
    /// AC-9: sürüm işi sıranın sonuna eklenir ve Konseptten Video sayfasında görünür; sıra onu kendiliğinden başlatır; Durdur'da tamamlanan
    /// aşamalar korunur, Devam et yarım aşamadan sürer (analiz/kod/render yeniden çalışmaz).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_QueueEnd_VisibleOnProductionPage_StopResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Sıralı video", ct);
        var waiting = Seed(profile, user, "Önceki konsept");
        (await client.PostAsJsonAsync("/api/production/queue", new { profileId = profile, conceptIds = new[] { waiting } }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(Versions(video)[0].Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();

        var state = await client.GetFromJsonAsync<JsonElement>($"/api/production/state?profileId={profile}", ct);
        state.GetProperty("jobs").EnumerateArray().Select(j => (j.GetProperty("title").GetString(), j.GetProperty("status").GetString()))
            .ShouldBe([("Önceki konsept", "queued"), ("Sıralı video", "queued")]);
        var gate = new TaskCompletionSource();
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Voice && cmd.WorkKey == video)
            {
                await gate.Task.WaitAsync(token);
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        var analyses = _factory.Provider.CallsOf(CallRole.Analysis).Count;
        (await client.PostAsJsonAsync("/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Progress?.StateOf(PipelineStage.Voice) == StageState.Running), Limit, ct);
        LatestJob(waiting).Status.ShouldBe(ProductionJobStatus.Completed);
        var renders = Count(AgentStageCommand.Render);

        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Progress?.StateOf(PipelineStage.Voice) == StageState.Pending), Limit, ct);
        LatestJob(video).Status.ShouldBe(ProductionJobStatus.Stopped);
        _factory.Agent.Stage = FakeAgentChannel.DefaultStage;
        (await client.PostAsJsonAsync($"/api/production/jobs/{LatestJob(video).Id}/resume", new { }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);

        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(analyses + 1); // Yalnızca önceki konseptin analizi.
        Count(AgentStageCommand.Render).ShouldBe(renders);
        Concept(video).Status.ShouldBe(ConceptStatus.VideoReady);
        gate.TrySetResult();
    }

    /// <summary>AC-9 (Ç-4): çıkışta çalışan sürüm işi Durduruldu olur, Sırada sürüm işi Sırada kalır; sıradan çıkarılan sürüm işinin konsepti Video oldu kalır (AC-18).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_Logout_StopsRunning_QueuedStays_RemoveKeepsVideoReady()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic, email: email);
        var first = await ProduceAsync(client, profile, user, "Çalışan sürüm", ct);
        var second = await ProduceAsync(client, profile, user, "Bekleyen sürüm", ct);
        var gate = new TaskCompletionSource();
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Voice)
            {
                await gate.Task.WaitAsync(token);
            }

            return await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        (await client.PostAsJsonAsync($"/api/produced/{first}/versions", Body(Versions(first)[0].Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/produced/{second}/versions", Body(Versions(second)[0].Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(first).Progress?.StateOf(PipelineStage.Voice) == StageState.Running), Limit, ct);

        (await client.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct)).EnsureSuccessStatusCode();

        LatestJob(first).Status.ShouldBe(ProductionJobStatus.Stopped);
        LatestJob(second).Status.ShouldBe(ProductionJobStatus.Queued);
        gate.SetResult();
        await Task.Delay(300, ct);
        LatestJob(second).Status.ShouldBe(ProductionJobStatus.Queued); // Sıra durur.

        using var again = _factory.CreateClient();
        var session = await AuthenticatedClient.LoginAsync(again, _factory.Services, email, AuthenticatedClient.Password, ct);
        again.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        (await again.PostAsJsonAsync($"/api/production/jobs/{LatestJob(second).Id}/remove", new { }, ct)).EnsureSuccessStatusCode();
        Concept(second).Status.ShouldBe(ConceptStatus.VideoReady);
        ActiveJobs(second).ShouldBeEmpty();
        Versions(second).Count.ShouldBe(1);
    }

    /// <summary>AC-10: ajan çevrimdışıyken başlatılan yeni sürüm Ajan bekleniyor kalır, hiçbir yerel aşama başlamaz; ajan bağlanınca kendiliğinden biter.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_AgentOffline_Waits_ResumesOnConnect()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, conn, device) = await SetupAsync(ct, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Ajansız", ct);
        var v1 = Versions(video)[0];
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>().DisconnectAsync(conn!, CancellationToken.None);
        }

        var sent = _factory.Agent.Sent.Count;
        var renders = Count(AgentStageCommand.Render);
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Status == ProductionJobStatus.WaitingForAgent), Limit, ct);
        await Task.Delay(500, ct);
        _factory.Agent.Sent.Count.ShouldBe(sent);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct)).GetProperty("active").GetProperty("status").GetString().ShouldBe("waiting_for_agent");
        await ExpectBusyAsync(client, video, Body(v1.Id, audio: Chatterbox, progressBar: false), ct); // Ajan bekleniyor.

        await RegisterAgentAsync(user, device, ct);
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);

        // Review r1 M-2: açık Yeniden üret bağlamı, yeni sürüm başlayınca kapanır — sıradaki konsept düzenlenemez.
        var token = (await (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate/open", new { }, ct)).Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("token").GetString();
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(Versions(video)[1].Id, audio: Supertonic), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/concepts/{video}/scenes?regen={token}", new { scenes = Edited("Sırada düzenleme") }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        Concept(video).Scenes[0].Narration.ShouldBe("Bizans'ın son sabahı başlıyor.");
        Count(AgentStageCommand.Render).ShouldBe(renders); // Aynı cihaz: render yeniden çalışmaz.
    }

    /// <summary>
    /// AC-11, AC-18: Video oldu konsept bağlam dışında düzenlenemez/yeniden düzenletilemez (çağrı yok) ve sıraya alınamaz; Yeniden üret bağlamında
    /// 0015 döngüsüyle (online düzenleme → revizyon → onay) düzenlenir, durumu Video oldu kalır; önizleme tüm aşamalar + online; onaysız iş yok;
    /// onayla tüm aşamalar baştan çalışır, yeni sürüm eklenir, eski sürüm ve metni korunur; konsept onaylananlarda görünmez; bağlam kapanır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Regenerate_Context_EditApprove_NewVersionAllStages()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct);
        await ConceptTestKit.SelectModelsAsync(client, profile, ct, ConceptTestKit.ConceptModel, null, null);
        var video = await ProduceAsync(client, profile, user, "Yeniden", ct);
        var v1 = Versions(video)[0];
        _factory.Provider.Handler = (call, role, index) => role == CallRole.Concept
            ? ConceptTestKit.Ok(ConceptTestKit.ConceptJson("Yeniden", [30, 30], "Revize anlatım."))
            : Reply(call, role, index);
        var calls = _factory.Provider.Calls.Count;

        (await client.PostAsJsonAsync($"/api/concepts/{video}/scenes", new { scenes = Edited("Bağlamsız") }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync($"/api/concepts/{video}/regenerate", new { instruction = "Kısalt" }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync("/api/production/queue", new { profileId = profile, conceptIds = new[] { video } }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _factory.Provider.Calls.Count.ShouldBe(calls);

        var opened = await (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate/open?baseVersionId={v1.Id}", new { }, ct)).Content.ReadFromJsonAsync<JsonElement>(ct);
        var token = opened.GetProperty("token").GetString();
        opened.GetProperty("profileId").GetGuid().ShouldBe(profile);
        (await client.PostAsJsonAsync($"/api/concepts/{video}/scenes?regen=yanlis", new { scenes = Edited("Yanlış") }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ActiveJobs(video).ShouldBeEmpty(); // Bağlam açıldı, düzenleme iptal: iş yok.

        (await client.PostAsJsonAsync($"/api/concepts/{video}/regenerate?regen={token}", new { instruction = "Kısalt" }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(
            async () => (await client.GetFromJsonAsync<JsonElement>($"/api/concepts/{video}/revision", ct)).GetProperty("status").GetString() == "ready", Limit, ct);
        (await client.PostAsJsonAsync($"/api/concepts/{video}/revision/approve", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync($"/api/concepts/{video}/revision/approve?regen={token}", new { }, ct)).EnsureSuccessStatusCode();
        Concept(video).Status.ShouldBe(ConceptStatus.VideoReady);
        Concept(video).Scenes[0].Narration.ShouldBe("Revize anlatım.");

        var preview = await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}/regenerate?token={token}&baseVersionId={v1.Id}", ct);
        Stages(preview).ShouldBe(["analysis", "code", "render", "merge"]);
        preview.GetProperty("online").GetBoolean().ShouldBeTrue();
        preview.GetProperty("unchanged").GetBoolean().ShouldBeFalse();
        (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate", new { token, baseVersionId = v1.Id }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ActiveJobs(video).ShouldBeEmpty();
        var (analysis, code) = (_factory.Provider.CallsOf(CallRole.Analysis).Count, _factory.Provider.CallsOf(CallRole.Code).Count);

        (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate", new { token, baseVersionId = v1.Id, confirmOnline = true }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);

        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(analysis + 1);
        _factory.Provider.CallsOf(CallRole.Code).Count.ShouldBe(code + 2);
        LatestJob(video).Version!.Kind.ShouldBe(VersionKind.Regenerate);
        var versions = Versions(video);
        versions[0].ShouldBe(v1);
        versions[0].ConceptText.ShouldContain("Bizans'ın son sabahı başlıyor.");
        versions[1].ConceptText.ShouldContain("Revize anlatım.");
        Concept(video).Status.ShouldBe(ConceptStatus.VideoReady);
        var approved = await client.GetFromJsonAsync<JsonElement>($"/api/concepts?profileId={profile}&statuses=approved", ct);
        approved.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldNotContain(video);
        (await client.PostAsJsonAsync($"/api/concepts/{video}/scenes?regen={token}", new { scenes = Edited("Sonradan") }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>AC-12: metin değişmediyse "Konsept değişmedi"; "Yine de baştan üret" de online onay ister; onayla aynı metinle tüm aşamalar baştan.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Regenerate_Unchanged_Blocked_ForceNeedsConfirm()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct);
        var video = await ProduceAsync(client, profile, user, "Aynı metin", ct);
        var v1 = Versions(video)[0];
        var token = (await (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate/open", new { }, ct)).Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("token").GetString();

        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}/regenerate?token={token}", ct)).GetProperty("unchanged").GetBoolean().ShouldBeTrue();
        using (var unchanged = await client.PostAsJsonAsync($"/api/produced/{video}/regenerate", new { token, baseVersionId = v1.Id, confirmOnline = true }, ct))
        {
            unchanged.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await unchanged.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Konsept değişmedi.");
        }

        (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate", new { token, baseVersionId = v1.Id, force = true }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ActiveJobs(video).ShouldBeEmpty();
        var analysis = _factory.Provider.CallsOf(CallRole.Analysis).Count;

        (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate", new { token, baseVersionId = v1.Id, force = true, confirmOnline = true }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);
        _factory.Provider.CallsOf(CallRole.Analysis).Count.ShouldBe(analysis + 1);
        Versions(video)[1].ConceptText.ShouldBe(v1.ConceptText);
    }

    /// <summary>
    /// AC-13: hatalı sürümde sade neden; Tekrar dene yarım aşamadan sürer (seslendirme yeniden çalışmaz); Sürümü kaldır yarım dosyayı ajana
    /// sildirir (silinemezse kayıt kalır), sürüm işini kaldırır; önceki sürümler ve harcama kayıtları aynı kalır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_Failed_Retry_ResumesFromStage_Remove_KeepsHistory()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Hatalı sürüm", ct);
        var v1 = Versions(video)[0];
        var failMerge = 1;
        _factory.Agent.Stage = (conn, cmd, token) => cmd.Stage == AgentStageCommand.Merge && Volatile.Read(ref failMerge) == 1
            ? Task.FromResult(new AgentStageReply(false, AgentStageReply.Failed))
            : FakeAgentChannel.DefaultStage(conn, cmd, token);
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Status == ProductionJobStatus.Failed), Limit, ct);
        var error = (await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct)).GetProperty("active").GetProperty("error").GetString()!;
        error.ShouldBe("Birleştiriliyor aşaması tamamlanamadı.");
        LatestJob(video).Progress!.PendingFile.ShouldNotBeNull();
        var voices = Count(AgentStageCommand.Voice);

        Volatile.Write(ref failMerge, 0);
        (await client.PostAsJsonAsync($"/api/produced/{video}/retry", new { }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(video).Count == 2), Limit, ct);
        Count(AgentStageCommand.Voice).ShouldBe(voices);
        (await client.PostAsJsonAsync($"/api/produced/{video}/retry", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        Volatile.Write(ref failMerge, 1);
        var versions = Versions(video);
        (await client.PostAsJsonAsync($"/api/produced/{video}/versions", Body(versions[1].Id, audio: Chatterbox, progressBar: false), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(video).Status == ProductionJobStatus.Failed), Limit, ct);
        var pending = LatestJob(video).Progress!.PendingFile!;
        var spend = _factory.Spend.Records.Where(r => r.UserId == user).ToList();
        _factory.Agent.DeleteFiles = (request, _) => Task.FromResult(new AgentDeleteFilesReply(request.FileNames));
        using (var locked = await client.PostAsJsonAsync($"/api/produced/{video}/remove-failed", new { }, ct))
        {
            locked.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        ActiveJobs(video).Single().Status.ShouldBe(ProductionJobStatus.Failed);
        _factory.Agent.DeleteFiles = FakeAgentChannel.DefaultDeleteFiles;
        (await client.PostAsJsonAsync($"/api/produced/{video}/remove-failed", new { }, ct)).EnsureSuccessStatusCode();

        using (var payload = JsonDocument.Parse(_factory.Agent.Sent.Last(s => s.Method == "DeleteFiles").Payload))
        {
            payload.RootElement.GetProperty("fileNames").EnumerateArray().Select(f => f.GetString()).ShouldBe([pending]);
        }

        ActiveJobs(video).ShouldBeEmpty();
        Versions(video).ShouldBe(versions);
        _factory.Spend.Records.Where(r => r.UserId == user).ToList().ShouldBe(spend);
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/{video}", ct)).GetProperty("active").ValueKind.ShouldBe(JsonValueKind.Null);
        (await client.PostAsJsonAsync($"/api/produced/{video}/remove-failed", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// AC-14: Üretiliyor sürüm varken Sil pasif ("Önce üretimi durdurun"); Durduruldu ya da Hata sürümü olan video silinir, iş sıradan kalkar ve
    /// sonrasında hiçbir aşama ya da online çağrı çalışmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_Delete_BlockedWhileActive_AllowedWhenStoppedOrFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var stopped = await ProduceAsync(client, profile, user, "Durdurulan", ct);
        var failing = await ProduceAsync(client, profile, user, "Hata veren", ct);
        var gate = new TaskCompletionSource();
        _factory.Agent.Stage = async (conn, cmd, token) =>
        {
            if (cmd.Stage == AgentStageCommand.Voice && cmd.WorkKey == stopped)
            {
                await gate.Task.WaitAsync(token);
            }

            return cmd.Stage == AgentStageCommand.Voice && cmd.WorkKey == failing
                ? new AgentStageReply(false, AgentStageReply.Failed)
                : await FakeAgentChannel.DefaultStage(conn, cmd, token);
        };
        (await client.PostAsJsonAsync($"/api/produced/{stopped}/versions", Body(Versions(stopped)[0].Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(stopped).Progress?.StateOf(PipelineStage.Voice) == StageState.Running), Limit, ct);

        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/{stopped}", ct)).GetProperty("deleteBlocked").GetBoolean().ShouldBeTrue();
        using (var blocked = await client.PostAsJsonAsync($"/api/produced/{stopped}/delete", new { }, ct))
        {
            blocked.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await blocked.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Önce üretimi durdurun.");
        }

        Versions(stopped).ShouldAllBe(v => v.DeletedAt == null);
        (await client.PostAsJsonAsync("/api/production/stop", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        (await client.GetFromJsonAsync<JsonElement>($"/api/produced/{stopped}", ct)).GetProperty("deleteBlocked").GetBoolean().ShouldBeFalse();
        (await client.PostAsJsonAsync($"/api/produced/{stopped}/delete", new { }, ct)).EnsureSuccessStatusCode();
        ActiveJobs(stopped).ShouldBeEmpty();

        (await client.PostAsJsonAsync($"/api/produced/{failing}/versions", Body(Versions(failing)[0].Id, audio: Chatterbox), ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(LatestJob(failing).Status == ProductionJobStatus.Failed), Limit, ct);
        (await client.PostAsJsonAsync($"/api/produced/{failing}/delete", new { }, ct)).EnsureSuccessStatusCode();
        ActiveJobs(failing).ShouldBeEmpty();

        var sent = _factory.Agent.Sent.Count;
        var calls = _factory.Provider.Calls.Count;
        gate.SetResult();
        await Task.Delay(800, ct);
        _factory.Agent.Sent.Count(s => s.Method == "RunStage").ShouldBe(_factory.Agent.Sent.Take(sent).Count(s => s.Method == "RunStage"));
        _factory.Provider.Calls.Count.ShouldBe(calls);
        (await client.GetAsync(new Uri($"/api/produced/{stopped}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// AC-15 (kasıtlı bozma hedefi): başka kullanıcının videosu için seçenek, önizleme, yeni sürüm, yeniden üret (aç/önizle/başlat), tekrar dene ve
    /// sürümü kaldır "bulunamadı" döner; iş oluşmaz, ajana komut ve sağlayıcıya çağrı gitmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_OtherUser_NotFound_NoJobNoCommand()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _, _) = await SetupAsync(ct, audio: Supertonic);
        var video = await ProduceAsync(client, profile, user, "Sahipli", ct);
        var v1 = Versions(video)[0];
        var token = (await (await client.PostAsJsonAsync($"/api/produced/{video}/regenerate/open", new { }, ct)).Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("token").GetString();
        var (other, _, _, _, _) = await SetupAsync(ct, reset: false);
        var sent = _factory.Agent.Sent.Count;
        var calls = _factory.Provider.Calls.Count;

        var responses = new List<HttpResponseMessage>
        {
            await other.GetAsync(new Uri($"/api/produced/{video}/version-options", UriKind.Relative), ct),
            await other.PostAsJsonAsync($"/api/produced/{video}/version-preview", Body(v1.Id, audio: Chatterbox), ct),
            await other.PostAsJsonAsync($"/api/produced/{video}/versions", Body(v1.Id, audio: Chatterbox, confirm: true), ct),
            await other.PostAsJsonAsync($"/api/produced/{video}/regenerate/open", new { }, ct),
            await other.GetAsync(new Uri($"/api/produced/{video}/regenerate?token={token}", UriKind.Relative), ct),
            await other.PostAsJsonAsync($"/api/produced/{video}/regenerate", new { token, baseVersionId = v1.Id, force = true, confirmOnline = true }, ct),
            await other.PostAsJsonAsync($"/api/produced/{video}/retry", new { }, ct),
            await other.PostAsJsonAsync($"/api/produced/{video}/remove-failed", new { }, ct),
            await other.PostAsJsonAsync($"/api/concepts/{video}/scenes?regen={token}", new { scenes = Edited("Başkası") }, ct),
        };

        responses.Select(r => r.StatusCode).ShouldAllBe(s => s == HttpStatusCode.NotFound);
        ActiveJobs(video).ShouldBeEmpty();
        _factory.Agent.Sent.Count.ShouldBe(sent);
        _factory.Provider.Calls.Count.ShouldBe(calls);
        Concept(video).Scenes[0].Narration.ShouldBe("Bizans'ın son sabahı başlıyor.");
        foreach (var r in responses)
        {
            r.Dispose();
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

    private static object SettingsBody(string? image = null, string? audio = null, bool progressBar = true) =>
        new { subtitles = true, progressBar, codeModel = Sonnet, codeEffort = (string?)null, engine = "local/hyperframes", imageModel = image, audioModel = audio };

    private static object Body(
        Guid baseVersion,
        string engine = "local/hyperframes",
        string? image = null,
        string? audio = null,
        bool subtitles = true,
        bool progressBar = true,
        bool confirm = false,
        string[]? stages = null) =>
        new { baseVersionId = baseVersion, engine, imageModel = image, audioModel = audio, subtitles, progressBar, confirmOnline = confirm, previewStages = stages };

    private static object[] Edited(string narration) =>
    [
        new { title = "Açılış", visual = "Surlar görünür.", narration, durationSeconds = 30 },
        new { title = "Son", visual = "Şehir sessizleşir.", narration = "Bir çağ kapanıyor.", durationSeconds = 30 },
    ];

    private static List<string?> Ids(JsonElement options, string list) =>
        [.. options.GetProperty(list).EnumerateArray().Select(m => m.GetProperty("id").GetString())];

    private static List<string?> Stages(JsonElement preview) => [.. preview.GetProperty("stages").EnumerateArray().Select(s => s.GetString())];

    private static async Task<JsonElement> PreviewAsync(HttpClient client, Guid video, object body, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"/api/produced/{video}/version-preview", body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    private static async Task<decimal> MonthAsync(HttpClient client, CancellationToken ct) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/ai/spend/month", ct)).GetProperty("totalUsd").GetDecimal();

    private static (Guid Video, Guid Version) SeedVideo(ProductionApiFactory factory, Guid user, Guid profile, string? image, string? audio)
    {
        var id = Guid.CreateVersion7();
        var version = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow.AddHours(-1);
        factory.Concepts.AddAsync(NewConcept(id, user, profile, "Hazır video", ConceptStatus.VideoReady, now), CancellationToken.None).GetAwaiter().GetResult();
        factory.Videos.AddAsync(
            new ProducedVideo
            {
                Id = version,
                UserId = user,
                ProfileId = profile,
                ConceptId = id,
                JobId = Guid.CreateVersion7(),
                Title = "Hazır video",
                ConceptText = "{}",
                CodeModel = Sonnet,
                Engine = "local/hyperframes",
                ImageModel = image,
                AudioModel = audio,
                Subtitles = audio is not null,
                FileName = "hazir-" + version.ToString("N")[..8] + ".mp4",
                StartedAt = now,
                EndedAt = now.AddMinutes(5),
            },
            CancellationToken.None).GetAwaiter().GetResult();
        return (id, version);
    }

    private static Concept NewConcept(Guid id, Guid user, Guid profile, string title, ConceptStatus status, DateTimeOffset now) => new()
    {
        Id = id,
        UserId = user,
        ProfileId = profile,
        Title = title,
        TitleKey = TitleKey.Of(title, "tr"),
        Source = ConceptSource.Idea,
        Status = status,
        Language = "tr",
        Format = "landscape",
        TargetDurationSeconds = 60,
        Scenes = Scenes,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static async Task ExpectBusyAsync(HttpClient client, Guid video, object body, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"/api/produced/{video}/versions", body, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe(Busy);
    }

    private async Task<(HttpClient Client, Guid Profile, Guid User, string? Conn, Guid Device)> SetupAsync(
        CancellationToken ct,
        string? image = null,
        string? audio = null,
        bool openAiKey = false,
        string[]? components = null,
        bool reset = true,
        string? email = null)
    {
        if (reset)
        {
            _factory.Provider.Reset();
            _factory.Provider.Handler = Reply;
            _factory.Agent.Reset();
        }

        var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        using var created = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body("Sürüm Kanalı " + Guid.NewGuid().ToString("N")[..6]), ct);
        created.EnsureSuccessStatusCode();
        var profile = (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = "sk-ant-" + Guid.NewGuid().ToString("N") }, ct)).EnsureSuccessStatusCode();
        if (openAiKey)
        {
            (await client.PutAsJsonAsync("/api/ai/keys/openai", new { apiKey = "sk-proj-" + Guid.NewGuid().ToString("N") }, ct)).EnsureSuccessStatusCode();
        }

        (await client.PutAsJsonAsync($"/api/production/settings?profileId={profile}", SettingsBody(image, audio), ct)).EnsureSuccessStatusCode();
        var user = _factory.Profiles.Rows.Single(p => p.Id == profile).UserId;
        var device = Guid.NewGuid();
        var conn = await RegisterAgentAsync(user, device, ct, components ?? []);
        return (client, profile, user, conn, device);
    }

    private async Task<string> RegisterAgentAsync(Guid userId, Guid device, CancellationToken ct, params string[] components)
    {
        var conn = "ver-" + Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>()
            .RegisterAsync(conn, new AgentOwner(userId, device), new AgentRegistration("PC", "1.0.0", components), ct);
        return conn;
    }

    private async Task<Guid> ProduceAsync(HttpClient client, Guid profile, Guid user, string title, CancellationToken ct)
    {
        var id = Seed(profile, user, title);
        (await client.PostAsJsonAsync("/api/production/queue", new { profileId = profile, conceptIds = new[] { id } }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/production/start", new { profileId = profile }, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(Versions(id).Count == 1 && LatestJob(id).Status == ProductionJobStatus.Completed), Limit, ct);
        return id;
    }

    private Guid Seed(Guid profile, Guid user, string title)
    {
        var id = Guid.CreateVersion7();
        _factory.Concepts.AddAsync(NewConcept(id, user, profile, title, ConceptStatus.Approved, DateTimeOffset.UtcNow.AddHours(-1)), CancellationToken.None)
            .GetAwaiter().GetResult();
        return id;
    }

    private int Count(string stage) => _factory.Agent.Count("RunStage", stage);

    private decimal SpendOf(Guid user) => _factory.Spend.Records.Where(r => r.UserId == user).Sum(r => r.CostUsd);

    private List<ProducedVideo> Versions(Guid concept) => [.. _factory.Videos.Rows.Where(v => v.ConceptId == concept).OrderBy(v => v.EndedAt)];

    private List<ProductionJob> ActiveJobs(Guid concept) => [.. _factory.Jobs.Rows.Where(j => j.ConceptId == concept && j.Status != ProductionJobStatus.Completed)];

    private ProductionJob LatestJob(Guid concept) => _factory.Jobs.Rows.Where(j => j.ConceptId == concept).OrderByDescending(j => j.CreatedAt).First();

    private Concept Concept(Guid id) => _factory.Concepts.Rows.Single(c => c.Id == id);
}
