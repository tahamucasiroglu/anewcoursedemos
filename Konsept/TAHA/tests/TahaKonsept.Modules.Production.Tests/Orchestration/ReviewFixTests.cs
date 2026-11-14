using Shouldly;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Orchestration;

/// <summary>
/// İnceleme bulgularının düzeltmeleri: sıradan çıkarma sürmekte olan sunucu zincirini keser, duraklatma deneme hakkı tüketmez,
/// yanıtsız ajan aşaması yeniden gönderilmeden önce durdurulur, başarısız denemenin harcaması kaybolmaz, sürüm numaraları çakışmaz.
/// </summary>
public sealed class ReviewFixTests
{
    /// <summary>Çalışan öğe çıkarılınca asılı LLM çağrısı iptal edilir; istek zinciri beklemez, iş iptal olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Removing_running_item_aborts_in_flight_server_stage()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await StartAsync(h);
        h.Provider.BlockUntilCancelled = true;

        var tick = h.TickAsync();
        await h.Provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var item = (await h.StateAsync()).Items.Single();
        var removed = await h.WithAsync<IProductionQueueService, bool>(async s => (await s.RemoveAsync(item.Id, CancellationToken.None)).IsSuccess)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await tick.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        removed.ShouldBeTrue();
        h.Provider.Requests.Count.ShouldBe(1);
        (await h.JobsAsync()).Single().Status.ShouldBe(JobStatus.Cancelled);
        (await h.StateAsync()).Items.ShouldBeEmpty();
    }

    /// <summary>Üç kez duraklat/devam sonrası geçici hata hâlâ yeniden denenir (duraklatma deneme sayılmaz).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pause_resume_cycles_do_not_consume_retry_attempts()
    {
        await using var h = await StartedAsync();
        for (var i = 0; i < 3; i++)
        {
            await h.FailNextAsync("agent.paused", retryable: true);
            await h.TickAsync();
            h.Agent.Pending.Count.ShouldBe(1);
        }

        await h.FailNextAsync("agent.gpu_oom", retryable: true);
        await h.TickAsync();

        var job = (await h.JobsAsync()).Single();
        job.Status.ShouldBe(JobStatus.Running);
        job.FindRun(PipelineStage.Speech)!.Attempts.ShouldBe(2);
        h.Agent.Pending.Single().Stage.ShouldBe(PipelineStage.Speech);
    }

    /// <summary>Yanıtsız ajan aşaması yeniden gönderilmeden önce eski komut durdurulur (aynı alanda iki çalıştırma olmasın).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stale_agent_stage_is_stopped_before_redispatch()
    {
        await using var h = await StartedAsync();
        var workspace = (await h.JobsAsync()).Single().WorkspaceId;
        h.Agent.Pending.Clear();
        h.Clock.Advance(TimeSpan.FromMinutes(11));

        await h.TickAsync();

        h.Agent.Paused.ShouldContain(workspace);
        await h.TickAsync();
        h.Agent.Sent.Count(c => c.Stage == PipelineStage.Speech).ShouldBe(2);
    }

    /// <summary>Sahne kodu iki denemede de geçersizse aşama başarısız olur ama iki LLM çağrısının maliyeti çalıştırmada kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Failed_server_stage_keeps_partial_cost()
    {
        await using var h = new ProductionTestHarness();
        h.Provider.InvalidResponsesLeft = 2;
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await StartAsync(h);

        await h.TickAsync();

        var job = (await h.JobsAsync()).Single();
        job.Status.ShouldBe(JobStatus.Failed);
        var run = job.Runs.Single(r => r.Stage == PipelineStage.SceneCode && r.Status == StageStatus.Failed);
        run.CostUsd.ShouldBe(0.10m);
        run.OutputTokens.ShouldBe(1000);
    }

    /// <summary>Konseptin yeniden sıraya eklenmesi ve biri bitmeden istenen iki yeni sürüm farklı sürüm numarası alır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pending_versions_get_distinct_numbers()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        var conceptId = await h.EnqueueSampleAsync();
        await StartAsync(h);
        await h.RunUntilIdleAsync();
        var first = (await h.VideosAsync()).Single();
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.ClearFinishedAsync(CancellationToken.None)).IsSuccess);

        var requeued = await h.WithAsync<IProductionQueueService, bool>(async s => (await s.AddAsync(new AddToQueueRequest { ConceptIds = [conceptId] }, CancellationToken.None)).IsSuccess);
        var a = await NewVersionAsync(h, first.Id, "F1");
        var b = await NewVersionAsync(h, first.Id, "F2");

        requeued.ShouldBeTrue();
        first.Version.ShouldBe(1);
        (await h.StateAsync()).Items.Select(i => i.Version).OrderBy(v => v).ShouldBe([2, 3, 4]);
        a.Version.ShouldBe(3);
        b.Version.ShouldBe(4);
    }

    private static Task StartAsync(ProductionTestHarness h) =>
        h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);

    private static Task<QueueItemDto> NewVersionAsync(ProductionTestHarness h, Guid videoId, string voice) =>
        h.WithAsync<IVideoService, QueueItemDto>(async s =>
            (await s.NewVersionAsync(videoId, new NewVersionRequest { Settings = TestSettings.LocalSpeech with { VoiceId = voice }, Changed = ["speech"] }, CancellationToken.None)).Value);

    private static async Task<ProductionTestHarness> StartedAsync()
    {
        var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await StartAsync(h);
        await h.TickAsync();
        h.Agent.Pending.Single().Stage.ShouldBe(PipelineStage.Speech);
        return h;
    }
}
