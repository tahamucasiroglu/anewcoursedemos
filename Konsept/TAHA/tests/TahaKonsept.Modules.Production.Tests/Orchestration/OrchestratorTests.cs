using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Diagnostics;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Orchestration;

/// <summary>
/// Orkestratör uçtan uca (sahte LLM/medya/ajan): tam hat, ajan çevrimdışı bekleme, online ses/görsel artefaktları, sahne lint hatasında
/// kodu yeniden yazdırma, işlem günlüğü ve istatistik.
/// </summary>
public sealed class OrchestratorTests
{
    /// <summary>Yerel ses + görsel kapalı: tüm aşamalar çalışır, video kaydedilir, günlük ve istatistik yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Full_pipeline_produces_video_with_logs_and_stats()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await StartAsync(h);

        await h.RunUntilIdleAsync();

        var job = (await h.JobsAsync()).Single();
        job.Status.ShouldBe(JobStatus.Completed);
        job.Runs.ShouldAllBe(r => r.IsDone);
        job.Runs.Count.ShouldBe(9 + 2 * 3);
        h.Provider.Requests.Count.ShouldBe(3);
        h.Provider.Requests.ShouldAllBe(r => r.MaxOutputTokens >= 32000 && r.Purpose == "scene-code" && r.SystemPrompt.Contains("1080"));
        h.Agent.Sent.Select(c => c.Stage).Distinct().ShouldBe([PipelineStage.Speech, PipelineStage.Align, PipelineStage.SceneRender, PipelineStage.Compose, PipelineStage.Mux]);
        h.Agent.Sent.Where(c => c.Stage == PipelineStage.SceneRender).Select(c => c.SceneIndex).ShouldBe([0, 1, 2]);
        h.Agent.Sent.ShouldAllBe(c => c.JobId == job.WorkspaceId && c.VideoId == job.VideoId && c.InputHash.Length == 64);

        var compose = ProductionJson.Read<ComposeStagePayload>(h.Agent.Sent.Single(c => c.Stage == PipelineStage.Compose).PayloadJson)!;
        compose.SceneDurationsSec.ShouldBe([3.0, 3.0, 3.0]);
        compose.IncludeAudio.ShouldBeTrue();
        compose.BurnSubtitles.ShouldBeTrue();

        var video = (await h.VideosAsync()).Single();
        video.Id.ShouldBe(job.VideoId);
        video.DurationSec.ShouldBe(9.0);
        video.TotalCostUsd.ShouldBe(0.15m);
        video.Version.ShouldBe(1);
        var state = await h.StateAsync();
        state.Items.Single().Status.ShouldBe(QueueItemStatus.Done);
        state.Status.ShouldBe(QueueRunState.Idle);
        h.Artifacts.DeletedJobs.ShouldContain(job.Id);

        h.Activity.Entries.ShouldContain(e => e.Message.StartsWith("Video hazırlandı — süre 9 sn", StringComparison.Ordinal) && e.Message.Contains("$0.15"));
        h.Activity.Entries.ShouldContain(e => e.Message.StartsWith("Sahne kodu bitti", StringComparison.Ordinal));

        var detail = await h.WithAsync<IVideoService, VideoDetailDto>(async s => (await s.GetAsync(video.Id, CancellationToken.None)).Value);
        detail.Scenes.Count.ShouldBe(3);
        detail.Scenes[1].SpokenText.ShouldContain("Gugıl");
        detail.Scenes[1].DisplayText.ShouldContain("Google");
        detail.PlaybackUrl.ShouldNotBeNull().ShouldContain(video.Id.ToString());
        detail.CostBreakdown.Single(c => c.Stage == PipelineStage.SceneCode).CostUsd.ShouldBe(0.15m);

        var stats = await h.InScopeAsync(async sp =>
        {
            var merged = new Dictionary<string, object>();
            foreach (var contributor in sp.GetServices<IStatsContributor>())
            {
                foreach (var pair in await contributor.GetStatsAsync(CancellationToken.None))
                {
                    merged[pair.Key] = pair.Value;
                }
            }

            return merged;
        });
        stats[StatsKeys.VideosTotal].ShouldBe(1);
        ((Dictionary<DateOnly, int>)stats[StatsKeys.VideosPerDay]).Values.Sum().ShouldBe(1);

        var dictionary = await h.WithAsync<IPronunciationService, IReadOnlyList<PronunciationDto>>(async s => (await s.ListAsync(CancellationToken.None)).Value);
        dictionary.ShouldContain(d => d.Written == "Google" && d.Status == PronunciationStatus.Suggested);
    }

    /// <summary>Ajan çevrimdışıyken iş "Ajan bekleniyor" olur; ajan gelince kaldığı yerden sürer.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Offline_agent_makes_job_wait_then_resumes()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        h.Agent.Online = false;
        await StartAsync(h);

        await h.TickAsync();

        var state = await h.StateAsync();
        state.AgentOnline.ShouldBeFalse();
        state.Items.Single().WaitingForAgent.ShouldBeTrue();
        state.Message.ShouldNotBeNull().ShouldContain("Ajan bekleniyor");
        h.Agent.Sent.ShouldBeEmpty();
        h.Provider.Requests.Count.ShouldBe(3);

        h.Agent.Online = true;
        await h.RunUntilIdleAsync();

        (await h.VideosAsync()).Count.ShouldBe(1);
        h.Provider.Requests.Count.ShouldBe(3);
    }

    /// <summary>Online ses ve görsel: sunucuda üretilir, artefakt olarak ajana gider (birleşik speech.wav + sahne görselleri).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Online_media_is_generated_on_server_and_sent_as_artifacts()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.OnlineMedia);
        await h.EnqueueSampleAsync();
        await StartAsync(h);

        await h.RunUntilIdleAsync();

        h.Media.SpeechRequests.Count.ShouldBe(3);
        h.Media.SpeechRequests.ShouldAllBe(r => r.Format == "wav" && r.Language == "tr-TR" && r.Voice == "Kore");
        h.Media.SpeechRequests.ShouldContain(r => r.Text.Contains("Gugıl"));
        h.Media.ImageRequests.Count.ShouldBe(3);
        h.Media.ImageRequests.ShouldAllBe(r => r.Prompt.Contains("no text") && r.Width == 1080 && r.Height == 1920);
        h.Agent.Sent.ShouldNotContain(c => c.Stage == PipelineStage.Speech || c.Stage == PipelineStage.Images);

        var align = h.Agent.Sent.Single(c => c.Stage == PipelineStage.Align);
        ProductionJson.Read<AlignStagePayload>(align.PayloadJson)!.AudioArtifactName.ShouldBe(AgentArtifactNames.SpeechAudio);
        align.Artifacts.Select(a => a.Name).ShouldBe([AgentArtifactNames.SpeechAudio]);
        var render0 = h.Agent.Sent.First(c => c.Stage == PipelineStage.SceneRender && c.SceneIndex == 0);
        render0.Artifacts.Select(a => a.Name).ShouldBe(["scene-0-1.png"]);
        ProductionJson.Read<SceneRenderStagePayload>(render0.PayloadJson)!.AssetNames.ShouldBe(["scene-0-1.png"]);
        h.Provider.Requests.First().UserPrompt.ShouldContain("assets/scene-0-1.png");

        var compose = ProductionJson.Read<ComposeStagePayload>(h.Agent.Sent.Single(c => c.Stage == PipelineStage.Compose).PayloadJson)!;
        compose.SceneDurationsSec.Sum().ShouldBeGreaterThan(0);
        (await h.VideosAsync()).Single().TotalCostUsd.ShouldBe(0.15m + 0.03m + 0.12m);
    }

    /// <summary>Ajan sahne kodunu lint'te reddedince kod hata ile bir kez yeniden yazdırılır ve hat tamamlanır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Scene_lint_failure_rewrites_scene_code_with_error()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await StartAsync(h);

        for (var i = 0; i < 20 && !h.Agent.Pending.Any(c => c.Stage == PipelineStage.SceneRender); i++)
        {
            await h.TickAsync();
            if (!h.Agent.Pending.Any(c => c.Stage == PipelineStage.SceneRender))
            {
                await h.CompletePendingAsync();
            }
        }

        await h.FailNextAsync("agent.scene_invalid", details: "lint: #root data-duration eksik");
        await h.RunUntilIdleAsync();

        h.Provider.Requests.Count.ShouldBe(4);
        h.Provider.Requests.Last().UserPrompt.ShouldContain("lint: #root data-duration eksik");
        h.Agent.Sent.Count(c => c.Stage == PipelineStage.SceneRender && c.SceneIndex == 0).ShouldBe(2);
        (await h.JobsAsync()).Single().Status.ShouldBe(JobStatus.Completed);
    }

    /// <summary>LLM biçimsiz kod döndürürse aynı aşamada hatayla bir kez yeniden istenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Invalid_llm_output_is_retried_once_with_validation_error()
    {
        await using var h = new ProductionTestHarness();
        h.Provider.InvalidResponsesLeft = 1;
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await StartAsync(h);

        await h.RunUntilIdleAsync();

        h.Provider.Requests.Count.ShouldBe(4);
        h.Provider.Requests.ElementAt(1).UserPrompt.ShouldContain("doğrulama hatası");
        (await h.VideosAsync()).Single().TotalCostUsd.ShouldBe(0.20m);
    }

    private static Task StartAsync(ProductionTestHarness h) =>
        h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
}
