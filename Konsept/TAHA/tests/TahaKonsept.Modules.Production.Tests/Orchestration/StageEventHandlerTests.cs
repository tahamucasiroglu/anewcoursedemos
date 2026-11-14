using Shouldly;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Orchestration;

/// <summary>
/// Ajan olay işleyicisi geçişleri: ilerleme kaydı, geçici hata → yeniden gönderim, kalıcı hata → iş/öğe başarısız ve sıradaki öğe,
/// başka kullanıcının/bilinmeyen komutun raporu yok sayılır.
/// </summary>
public sealed class StageEventHandlerTests
{
    /// <summary>İlerleme raporu çalıştırmaya ve iş mesajına yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Progress_updates_run_and_job_message()
    {
        await using var h = await StartedAsync();
        var command = h.Agent.Pending.Single();

        await Handler(h, x => x.OnStageProgressAsync(Context(h.UserId), new StageProgressReport { CommandId = command.CommandId, Stage = command.Stage, Percent = 42, Message = "Seslendiriliyor (1/3)" }));

        var item = (await h.StateAsync()).Items.Single();
        item.Message.ShouldNotBeNull().ShouldContain("Seslendiriliyor (1/3)");
        item.Stages.Single(s => s.Stage == PipelineStage.Speech).Progress.ShouldBe(42);
    }

    /// <summary>Geçici hata: aşama beklemeye döner ve yeniden gönderilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Retryable_failure_redispatches_stage()
    {
        await using var h = await StartedAsync();

        await h.FailNextAsync("agent.shutdown", retryable: true);
        await h.TickAsync();

        h.Agent.Sent.Count(c => c.Stage == PipelineStage.Speech).ShouldBe(2);
        h.Agent.Pending.Single().Stage.ShouldBe(PipelineStage.Speech);
    }

    /// <summary>Kalıcı hata: iş ve öğe başarısız olur, hata günlüğü yazılır, sıradaki öğe başlar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Permanent_failure_fails_job_and_starts_next_item()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync("Birinci");
        await h.EnqueueSampleAsync("İkinci");
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
        await h.TickAsync();

        await h.FailNextAsync("agent.engine_not_found", retryable: false);
        await h.RunUntilIdleAsync();

        var state = await h.StateAsync();
        state.Items.Single(i => i.Title == "Birinci").Status.ShouldBe(QueueItemStatus.Failed);
        state.Items.Single(i => i.Title == "Birinci").Message.ShouldNotBeNull().ShouldContain("Seslendirme");
        state.Items.Single(i => i.Title == "İkinci").Status.ShouldBe(QueueItemStatus.Done);
        (await h.JobsAsync())[0].Status.ShouldBe(JobStatus.Failed);
        h.Activity.Entries.ShouldContain(e => e.Level == Shared.Contracts.Diagnostics.LogLevelKind.Error && e.Message.Contains("başarısız"));
    }

    /// <summary>Başka kullanıcının bağlamından gelen ya da bilinmeyen komut raporu yok sayılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Reports_from_other_user_or_unknown_command_are_ignored()
    {
        await using var h = await StartedAsync();
        var command = h.Agent.Pending.Single();

        await Handler(h, x => x.OnStageCompletedAsync(Context(Guid.CreateVersion7()), new StageResultReport { CommandId = command.CommandId, Stage = command.Stage }));
        await Handler(h, x => x.OnStageFailedAsync(Context(h.UserId), new StageFailureReport { CommandId = Guid.CreateVersion7(), ErrorCode = "x" }));

        var job = (await h.JobsAsync()).Single();
        job.Status.ShouldBe(JobStatus.Running);
        job.FindRun(PipelineStage.Speech)!.Status.ShouldBe(StageStatus.Running);
    }

    /// <summary>Uzun süre yanıt gelmeyen ajan aşaması yeniden gönderilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stale_agent_stage_is_redispatched()
    {
        await using var h = await StartedAsync();
        h.Agent.Pending.Clear();
        h.Clock.Advance(TimeSpan.FromMinutes(11));

        await h.TickAsync();
        await h.TickAsync();

        h.Agent.Sent.Count(c => c.Stage == PipelineStage.Speech).ShouldBe(2);
    }

    private static async Task<ProductionTestHarness> StartedAsync()
    {
        var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
        await h.TickAsync();
        h.Agent.Pending.Single().Stage.ShouldBe(PipelineStage.Speech);
        return h;
    }

    private static Task Handler(ProductionTestHarness h, Func<IAgentStageEventHandler, Task> action) =>
        h.WithAsync<IAgentStageEventHandler, bool>(async x =>
        {
            await action(x);
            return true;
        });

    private static AgentCallContext Context(Guid userId) => new("c", userId, null);
}
