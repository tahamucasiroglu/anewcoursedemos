using Shouldly;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Orchestration;

/// <summary>
/// Yeni sürüm / yeniden üret: değişmeyen aşamaların yeniden kullanımı (LLM tekrar çağrılmaz), ajan çalışma alanının paylaşımı,
/// online ses değişince yeni alan, yeniden ürette sıfırdan çalışma; video silme ve oynatma işlemleri.
/// </summary>
public sealed class NewVersionTests
{
    /// <summary>Yalnız ses değişti: sahne kodu yeniden kullanılır, sahne render'ları atlanır (aynı alan), ses yeniden üretilir; sürüm 2.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task New_version_with_voice_change_reuses_code_and_renders()
    {
        await using var h = await CompletedAsync(TestSettings.LocalSpeech);
        var first = (await h.VideosAsync()).Single();

        var item = await h.WithAsync<IVideoService, QueueItemDto>(async s =>
            (await s.NewVersionAsync(first.Id, new NewVersionRequest { Settings = TestSettings.LocalSpeech with { VoiceId = "F2" }, Changed = ["speech"] }, CancellationToken.None)).Value);
        item.Version.ShouldBe(2);
        item.ParentVideoId.ShouldBe(first.Id);
        await h.RunUntilIdleAsync();

        h.Provider.Requests.Count.ShouldBe(3);
        var jobs = await h.JobsAsync();
        var second = jobs[1];
        second.WorkspaceId.ShouldBe(jobs[0].WorkspaceId);
        second.Runs.Where(r => r.Stage == PipelineStage.SceneCode).ShouldAllBe(r => r.Reused && r.Status == StageStatus.Skipped && r.CostUsd == 0);
        second.Runs.Where(r => r.Stage == PipelineStage.SceneRender).ShouldAllBe(r => r.Reused && r.Status == StageStatus.Skipped);
        second.FindRun(PipelineStage.Speech)!.Reused.ShouldBeFalse();
        second.FindRun(PipelineStage.Normalize)!.Reused.ShouldBeTrue();
        h.Agent.Sent.Where(c => c.JobId == second.WorkspaceId && c.Stage == PipelineStage.SceneRender).Count().ShouldBe(6);

        var videos = await h.VideosAsync();
        videos.Count.ShouldBe(2);
        videos[1].Version.ShouldBe(2);
        videos[1].ParentVideoId.ShouldBe(first.Id);
        videos[1].TotalCostUsd.ShouldBe(0m);
    }

    /// <summary>Online ses değişince ajan çalışma alanı paylaşılmaz (eski aynı adlı dosyalar karışmasın).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task New_version_with_online_speech_change_uses_fresh_workspace()
    {
        await using var h = await CompletedAsync(TestSettings.OnlineMedia);
        var first = (await h.VideosAsync()).Single();

        await h.WithAsync<IVideoService, QueueItemDto>(async s =>
            (await s.NewVersionAsync(first.Id, new NewVersionRequest { Settings = TestSettings.OnlineMedia with { VoiceId = "Puck" } }, CancellationToken.None)).Value);
        await h.RunUntilIdleAsync();

        var second = (await h.JobsAsync())[1];
        second.WorkspaceId.ShouldBe(second.Id);
        h.Provider.Requests.Count.ShouldBe(3);
        h.Media.ImageRequests.Count.ShouldBe(3);
        h.Media.SpeechRequests.Count.ShouldBe(6);
        second.Runs.Where(r => r.Stage == PipelineStage.SceneRender).ShouldAllBe(r => !r.Reused);
    }

    /// <summary>Yeniden üret: düzenlenmiş metinle sıfırdan (sahne kodları yeniden yazılır).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Regenerate_runs_from_scratch_with_edited_document()
    {
        await using var h = await CompletedAsync(TestSettings.LocalSpeech);
        var first = (await h.VideosAsync()).Single();
        var document = FakeConceptSource.Sample() with { Title = "Fetih — düzenlenmiş" };

        await h.WithAsync<IVideoService, QueueItemDto>(async s => (await s.RegenerateAsync(first.Id, new RegenerateRequest { Document = document }, CancellationToken.None)).Value);
        await h.RunUntilIdleAsync();

        h.Provider.Requests.Count.ShouldBe(6);
        var videos = await h.VideosAsync();
        videos[1].Title.ShouldBe("Fetih — düzenlenmiş");
        (await h.JobsAsync())[1].BaseJobId.ShouldBeNull();
    }

    /// <summary>Silme ajandaki dosyayı da sildirir; klasörde aç / oynatıcıda aç ajana gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Delete_and_open_actions_reach_agent()
    {
        await using var h = await CompletedAsync(TestSettings.LocalSpeech);
        var video = (await h.VideosAsync()).Single();

        (await h.WithAsync<IVideoService, bool>(async s => (await s.OpenFolderAsync(video.Id, CancellationToken.None)).IsSuccess)).ShouldBeTrue();
        (await h.WithAsync<IVideoService, bool>(async s => (await s.OpenInPlayerAsync(video.Id, CancellationToken.None)).IsSuccess)).ShouldBeTrue();
        (await h.WithAsync<IVideoService, bool>(async s => (await s.DeleteAsync(video.Id, CancellationToken.None)).IsSuccess)).ShouldBeTrue();

        h.Agent.DeletedVideos.ShouldContain(video.AgentVideoId);
        (await h.VideosAsync()).ShouldBeEmpty();
        (await h.WithAsync<IVideoService, bool>(async s => (await s.GetAsync(video.Id, CancellationToken.None)).IsFailure)).ShouldBeTrue();
    }

    /// <summary>Ajan kapalıyken ayrıntıda oynatma adresi yerine açıklama döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Detail_without_agent_explains_playback()
    {
        await using var h = await CompletedAsync(TestSettings.LocalSpeech);
        var video = (await h.VideosAsync()).Single();
        h.Agent.Online = false;

        var detail = await h.WithAsync<IVideoService, VideoDetailDto>(async s => (await s.GetAsync(video.Id, CancellationToken.None)).Value);

        detail.PlaybackUrl.ShouldBeNull();
        detail.AgentOnline.ShouldBeFalse();
        detail.PlaybackMessage.ShouldNotBeNull().ShouldContain("Masaüstü programı kapalı");
        detail.Document.Scenes.Count.ShouldBe(3);
    }

    private static async Task<ProductionTestHarness> CompletedAsync(ProductionSettingsDto settings)
    {
        var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(settings);
        await h.EnqueueSampleAsync();
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
        await h.RunUntilIdleAsync();
        (await h.VideosAsync()).Count.ShouldBe(1);
        return h;
    }
}
