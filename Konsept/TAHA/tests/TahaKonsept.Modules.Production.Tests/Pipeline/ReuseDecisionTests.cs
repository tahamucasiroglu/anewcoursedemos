using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Production.Domain.Models;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Pipeline;

/// <summary>
/// Aşama hash'i + yeniden kullanım kararları: gerçek bir iş sonuna kadar koşturulur, sonra değişen ayar/metinle yeni sürüm planı çıkarılır.
/// Beklenen (PLAN §3): yalnız ses → Speech/Align/Compose/Mux; yalnız motor → SceneCode/SceneRender/Compose/Mux; metin → hepsi.
/// </summary>
public sealed class ReuseDecisionTests
{
    /// <summary>Ayar değişmezse yalnızca Mux (yeni video kimliği) yeniden çalışır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Same_settings_reuse_everything_but_mux()
    {
        await using var h = new ProductionTestHarness();
        var baseJob = await CompleteBaseAsync(h, TestSettings.LocalSpeech);

        var plan = await PlanAsync(h, baseJob, TestSettings.LocalSpeech);

        plan.RerunStages.ShouldBe([PipelineStage.Mux]);
    }

    /// <summary>Yalnız ses (konuşmacı) değişince Speech, Align, Compose, Mux.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Only_speech_changed_reruns_speech_align_compose_mux()
    {
        await using var h = new ProductionTestHarness();
        var baseJob = await CompleteBaseAsync(h, TestSettings.LocalSpeech);

        var plan = await PlanAsync(h, baseJob, TestSettings.LocalSpeech with { VoiceId = "F2", SpeechSpeed = 1.1 });

        plan.RerunStages.ShouldBe([PipelineStage.Speech, PipelineStage.Align, PipelineStage.Compose, PipelineStage.Mux]);
    }

    /// <summary>Yalnız motor değişince SceneCode, SceneRender, Compose, Mux (ses yeniden kullanılır).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Only_engine_changed_reruns_code_render_compose_mux()
    {
        await using var h = new ProductionTestHarness();
        var baseJob = await CompleteBaseAsync(h, TestSettings.LocalSpeech);

        var plan = await PlanAsync(h, baseJob, TestSettings.LocalSpeech with { VideoEngine = "remotion" });

        plan.RerunStages.ShouldBe([PipelineStage.SceneCode, PipelineStage.SceneRender, PipelineStage.Compose, PipelineStage.Mux]);
    }

    /// <summary>Metin (tüm sahnelerin seslendirmesi ve görsel istemi) değişince tüm aşamalar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Text_changed_reruns_all_stages()
    {
        await using var h = new ProductionTestHarness();
        var baseJob = await CompleteBaseAsync(h, TestSettings.OnlineMedia);
        var original = ProductionJson.Read<ConceptDocument>(baseJob.DocumentJson)!;
        var edited = original with
        {
            Scenes = original.Scenes.Select(s => s with { Narration = s.Narration + " Yeni cümle.", VisualPrompt = s.VisualPrompt + ", gece" }).ToList(),
        };

        var plan = await PlanAsync(h, baseJob, TestSettings.OnlineMedia, edited);

        plan.RerunStages.ShouldBe(PipelineStages.Ordered);
    }

    /// <summary>Tek sahnenin metni değişince yalnızca o sahnenin kodu/render'ı (ve ses zinciri) yeniden çalışır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task One_scene_text_changed_reruns_only_that_scene_code_and_render()
    {
        await using var h = new ProductionTestHarness();
        var baseJob = await CompleteBaseAsync(h, TestSettings.LocalSpeech);
        var original = ProductionJson.Read<ConceptDocument>(baseJob.DocumentJson)!;
        var edited = original with { Scenes = original.Scenes.Select(s => s.Index == 1 ? s with { Narration = "Kuşatma kısa sürdü." } : s).ToList() };

        var plan = await PlanAsync(h, baseJob, TestSettings.LocalSpeech, edited);

        plan.Decisions.Where(d => d.Stage == PipelineStage.SceneCode && !d.Reuse).Select(d => d.SceneIndex).ShouldBe([1]);
        plan.Decisions.Where(d => d.Stage == PipelineStage.SceneRender && !d.Reuse).Select(d => d.SceneIndex).ShouldBe([1]);
        plan.ReusedStages.ShouldContain(PipelineStage.Images);
        plan.RerunStages.ShouldContain(PipelineStage.Speech);
    }

    /// <summary>Kanonik hash: aynı girdi aynı hash, sahne ve aşama ayrımı yapılır.</summary>
    [Fact]
    public void Hash_is_deterministic_and_scoped_by_stage_and_scene()
    {
        var hasher = new Application.Concrete.Sha256StageInputHasher();
        var a = hasher.HashInput(PipelineStage.SceneCode, 0, new { x = 1, y = "ş" });
        hasher.HashInput(PipelineStage.SceneCode, 0, new { x = 1, y = "ş" }).ShouldBe(a);
        hasher.HashInput(PipelineStage.SceneCode, 1, new { x = 1, y = "ş" }).ShouldNotBe(a);
        hasher.HashInput(PipelineStage.SceneRender, 0, new { x = 1, y = "ş" }).ShouldNotBe(a);
        a.Length.ShouldBe(64);
        hasher.HashOutput(a, null).ShouldNotBe(hasher.HashOutput(a, "{}"));
    }

    private static async Task<Job> CompleteBaseAsync(ProductionTestHarness h, ProductionSettingsDto settings)
    {
        await h.SaveSettingsAsync(settings);
        await h.EnqueueSampleAsync();
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
        await h.RunUntilIdleAsync();
        var job = (await h.JobsAsync()).Single();
        job.Status.ShouldBe(JobStatus.Completed);
        return job;
    }

    private static Task<ReusePlan> PlanAsync(ProductionTestHarness h, Job baseJob, ProductionSettingsDto settings, ConceptDocument? document = null) =>
        h.InScopeAsync(async sp =>
        {
            var normalized = ProductionSettingsDefaults.Normalize(settings);
            var job = Job.Create(new JobSpec
            {
                UserId = h.UserId,
                ConceptId = baseJob.ConceptId,
                Title = baseJob.Title,
                BaseJobId = baseJob.Id,
                Format = baseJob.Format,
                Width = baseJob.Width,
                Height = baseJob.Height,
                Fps = normalized.Fps,
                SettingsJson = ProductionJson.Write(normalized),
                DocumentJson = document is null ? baseJob.DocumentJson : ProductionJson.Write(document),
            });
            var context = await sp.GetRequiredService<IPipelineContextFactory>().CreateAsync(job, CancellationToken.None);
            return await sp.GetRequiredService<IReusePlanner>().PlanAsync(context, baseJob.Runs, CancellationToken.None);
        });
}
