using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>Üretim hattının saf kuralları (spec 0017): atlanan aşamalar, çıktıların yeniden kullanımı, araçlar, metinler, ayrıştırma.</summary>
public sealed class PipelineRulesTests
{
    /// <summary>AC-21: süre "57 sn" / "30 dk 12 sn" / saat; saat aralığı Europe/Istanbul HH:mm.</summary>
    [Fact]
    public void JobTimeText_Formats()
    {
        JobTimeText.Duration(TimeSpan.FromSeconds(57)).ShouldBe("57 sn");
        JobTimeText.Duration(TimeSpan.FromSeconds((30 * 60) + 12)).ShouldBe("30 dk 12 sn");
        JobTimeText.Duration(TimeSpan.FromSeconds(3600 + 65)).ShouldBe("1 sa 1 dk 5 sn");
        JobTimeText.Range(new DateTimeOffset(2026, 11, 16, 7, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 16, 8, 0, 0, TimeSpan.Zero))
            .ShouldBe("10:30–11:00");
    }

    /// <summary>AC-3, AC-4: kapalı görsel/ses atlanır; AC-9/AC-11/AC-30: metin özeti tutan çıktılar ve aynı cihazdaki yerel aşamalar tamamlanmış sayılır.</summary>
    [Fact]
    public void Initial_SkipsOffModels_ReusesOutputs_PerDevice()
    {
        var off = Fixed(image: null, audio: null);
        var fresh = PipelineRules.Initial(off, null, null);
        fresh.Select(s => s.State).ShouldBe([StageState.Pending, StageState.Pending, StageState.Skipped, StageState.Skipped, StageState.Pending, StageState.Pending]);

        var device = Guid.NewGuid();
        var outputs = new StageOutputs
        {
            TextHash = "h",
            Plan = [new ScenePlan(0, 5, "a", "b")],
            Codes = ["<html></html>"],
            LocalDevice = device,
            LocalDone = [PipelineStage.Images, PipelineStage.Render],
        };
        var on = Fixed(image: "local/z-image-turbo", audio: "local/supertonic-3");
        PipelineRules.Initial(on, outputs, device).Select(s => s.State)
            .ShouldBe([StageState.Done, StageState.Done, StageState.Done, StageState.Pending, StageState.Done, StageState.Pending]);
        PipelineRules.Initial(on, outputs, Guid.NewGuid()).Select(s => s.State)
            .ShouldBe([StageState.Done, StageState.Done, StageState.Pending, StageState.Pending, StageState.Pending, StageState.Pending]);
    }

    /// <summary>AC-16: çekirdek araçlar + motor + yalnızca yerel görsel/ses modelleri; online modeller ajandan istenmez.</summary>
    [Fact]
    public void RequiredTools_CoreEngineAndLocalModels()
    {
        PipelineRules.RequiredTools(Fixed("openai/gpt-image-1", "local/supertonic-3"))
            .ShouldBe(["node", "ffmpeg", "chromium", "hyperframes", "supertonic-3"]);
        PipelineRules.ToolName("supertonic-3").ShouldBe("Supertonic 3");
        PipelineRules.AfterFailure(1).ShouldBe((2, false));
        PipelineRules.AfterFailure(2).ShouldBe((3, true));
    }

    /// <summary>AC-8: analiz planı sahne sayısını korur, eksikler konseptten; kod HTML olmalı, kod çiti atılır.</summary>
    [Fact]
    public void PlanParser_SceneCount_Fallbacks_Code()
    {
        (string, string, int)[] scenes = [("Bir", "Görsel bir", 10), ("İki", "Görsel iki", 20)];
        var plan = PlanParser.ParsePlan("```json\n{\"scenes\":[{\"durationSeconds\":12,\"narration\":\"Bir!\",\"imagePrompt\":\"p\"},{}]}\n```", scenes);
        plan.ShouldNotBeNull();
        plan.ShouldBe([new ScenePlan(0, 12, "Bir!", "p"), new ScenePlan(1, 20, "İki", "Görsel iki")]);
        PlanParser.ParsePlan("{\"scenes\":[{}]}", scenes).ShouldBeNull();
        PlanParser.ParsePlan("not json", scenes).ShouldBeNull();
        PlanParser.ParseCode("```html\n<!doctype html><html><body></body></html>\n```").ShouldBe("<!doctype html><html><body></body></html>");
        PlanParser.ParseCode("merhaba").ShouldBeNull();
    }

    private static FixedJobSettings Fixed(string? image, string? audio) => new()
    {
        Engine = "local/hyperframes",
        CodeModel = "anthropic/claude-sonnet-4-5",
        ImageModel = image,
        AudioModel = audio,
        Language = "tr",
        Format = "landscape",
        TargetDurationSeconds = 60,
    };
}
