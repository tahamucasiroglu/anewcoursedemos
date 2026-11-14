using Shouldly;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Diagnostics;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Orchestration;

/// <summary>
/// Sıra kontrolü: sıralama/taşıma/çıkarma, sırayla üretim, durdur (ajana PauseJob) → devam (biten aşamalar atlanır), bütçe durdurması
/// (mevcut iş biter, yenisi başlamaz), çalışan öğeyi çıkarınca iptal.
/// </summary>
public sealed class QueueControlTests
{
    /// <summary>Taşıma ve çıkarma sırayı değiştirir; işler yeni sırayla üretilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Move_and_remove_change_processing_order()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync("A");
        await h.EnqueueSampleAsync("B");
        await h.EnqueueSampleAsync("C");
        var items = (await h.StateAsync()).Items;
        items.Select(i => i.Title).ShouldBe(["A", "B", "C"]);

        var moved = await Queue(h, s => s.MoveAsync(items[2].Id, new MoveQueueItemRequest { Direction = -1 }, CancellationToken.None));
        moved.Items.Select(i => i.Title).ShouldBe(["A", "C", "B"]);
        var cannot = await h.WithAsync<IProductionQueueService, bool>(async s => (await s.MoveAsync(items[0].Id, new MoveQueueItemRequest { Direction = -1 }, CancellationToken.None)).IsFailure);
        cannot.ShouldBeTrue();
        (await h.WithAsync<IProductionQueueService, bool>(async s => (await s.RemoveAsync(items[1].Id, CancellationToken.None)).IsSuccess)).ShouldBeTrue();

        await Queue(h, s => s.StartAsync(CancellationToken.None));
        await h.RunUntilIdleAsync();

        (await h.VideosAsync()).Select(v => v.Title).ShouldBe(["A", "C"]);
        (await h.StateAsync()).Message.ShouldBe("Sıra tamamlandı");
    }

    /// <summary>Aynı konsept bekleyen/çalışan durumdayken tekrar eklenmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Same_concept_is_not_queued_twice()
    {
        await using var h = new ProductionTestHarness();
        var conceptId = await h.EnqueueSampleAsync();

        var again = await h.WithAsync<IProductionQueueService, bool>(async s => (await s.AddAsync(new AddToQueueRequest { ConceptIds = [conceptId] }, CancellationToken.None)).IsFailure);

        again.ShouldBeTrue();
        (await h.StateAsync()).Items.Count.ShouldBe(1);
    }

    /// <summary>Durdur: ajandaki aşamaya PauseJob gider, iş duraklar; devam: biten aşamalar tekrarlanmaz, kaldığı yerden sürer.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pause_then_resume_continues_where_left()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await Queue(h, s => s.StartAsync(CancellationToken.None));
        await h.TickAsync();
        h.Agent.Pending.Single().Stage.ShouldBe(PipelineStage.Speech);
        var job = (await h.JobsAsync()).Single();

        var paused = await Queue(h, s => s.PauseAsync(CancellationToken.None));
        paused.Status.ShouldBe(QueueRunState.Paused);
        h.Agent.Paused.ShouldContain(job.WorkspaceId);
        await h.FailNextAsync("agent.paused", retryable: true);
        await h.TickAsync();

        var state = await h.StateAsync();
        state.Items.Single().Status.ShouldBe(QueueItemStatus.Paused);
        (await h.JobsAsync()).Single().Status.ShouldBe(JobStatus.Paused);
        h.Agent.Pending.ShouldBeEmpty();

        await Queue(h, s => s.ResumeAsync(CancellationToken.None));
        await h.RunUntilIdleAsync();

        h.Provider.Requests.Count.ShouldBe(3);
        h.Agent.Sent.Count(c => c.Stage == PipelineStage.Speech).ShouldBe(2);
        (await h.JobsAsync()).Single().Status.ShouldBe(JobStatus.Completed);
        (await h.StateAsync()).Items.Single().Status.ShouldBe(QueueItemStatus.Done);
    }

    /// <summary>Bütçe aşılınca yeni iş başlamaz; sıra Boşta + bütçe engeli, uyarı günlüğü.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Budget_exceeded_blocks_new_jobs_and_logs_warning()
    {
        await using var h = new ProductionTestHarness();
        await h.EnqueueSampleAsync();
        h.Budget.Blocked = true;
        await Queue(h, s => s.StartAsync(CancellationToken.None));

        await h.TickAsync();

        var state = await h.StateAsync();
        state.Status.ShouldBe(QueueRunState.Idle);
        state.BudgetBlocked.ShouldBeTrue();
        state.Items.Single().Status.ShouldBe(QueueItemStatus.Waiting);
        (await h.JobsAsync()).ShouldBeEmpty();
        h.Activity.Entries.ShouldContain(e => e.Level == LogLevelKind.Warning && e.Message.Contains("bütçe"));
    }

    /// <summary>Bütçe çalışan iş sırasında aşılırsa mevcut iş biter, sıradaki başlamaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Budget_exceeded_mid_queue_lets_current_job_finish()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync("Birinci");
        await h.EnqueueSampleAsync("İkinci");
        await Queue(h, s => s.StartAsync(CancellationToken.None));
        await h.TickAsync();
        h.Budget.Blocked = true;

        await h.RunUntilIdleAsync();

        (await h.VideosAsync()).Select(v => v.Title).ShouldBe(["Birinci"]);
        var state = await h.StateAsync();
        state.BudgetBlocked.ShouldBeTrue();
        state.Items.Single(i => i.Title == "İkinci").Status.ShouldBe(QueueItemStatus.Waiting);
    }

    /// <summary>Çalışan öğe çıkarılınca iş iptal edilir, ajana iptal gider, sıradaki öğe başlar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Removing_running_item_cancels_job_and_continues()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync("Birinci");
        await h.EnqueueSampleAsync("İkinci");
        await Queue(h, s => s.StartAsync(CancellationToken.None));
        await h.TickAsync();
        var running = (await h.StateAsync()).Items.Single(i => i.Status == QueueItemStatus.Running);
        h.Agent.Pending.Clear();

        (await h.WithAsync<IProductionQueueService, bool>(async s => (await s.RemoveAsync(running.Id, CancellationToken.None)).IsSuccess)).ShouldBeTrue();
        await h.RunUntilIdleAsync();

        var jobs = await h.JobsAsync();
        jobs[0].Status.ShouldBe(JobStatus.Cancelled);
        h.Agent.Cancelled.ShouldContain(jobs[0].WorkspaceId);
        (await h.VideosAsync()).Select(v => v.Title).ShouldBe(["İkinci"]);
    }

    private static Task<QueueStateDto> Queue(ProductionTestHarness h, Func<IProductionQueueService, Task<BuildingBlocks.Domain.Results.Result<QueueStateDto>>> action) =>
        h.WithAsync<IProductionQueueService, QueueStateDto>(async s => (await action(s)).Value);
}
