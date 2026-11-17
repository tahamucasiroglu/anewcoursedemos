using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>Yeni sürümün aşama kuralları (spec 0020 Req 1 tablosu, AC-1): etkilenen aşamalar, online bilgisi, imzalar ve çalışacak küme.</summary>
public sealed class VersionStagesTests
{
    private const string Supertonic = "local/supertonic-3";
    private const string Chatterbox = "local/chatterbox-multilingual-v3";
    private const string ElevenLabs = "elevenlabs/eleven-multilingual-v2";
    private const string ZImage = "local/z-image-turbo";
    private const string Flux = "local/flux2-klein-4b";
    private const string GptImage = "openai/gpt-image-2.6-flare";

    private static readonly FixedJobSettings Base = new()
    {
        Subtitles = true,
        ProgressBar = false,
        CodeModel = "anthropic/claude-sonnet-5-5",
        Engine = "local/hyperframes",
        ImageModel = ZImage,
        AudioModel = Supertonic,
        Language = "tr",
        Format = "landscape",
        TargetDurationSeconds = 60,
    };

    /// <summary>Tablo satırları: her değişikliğin aşamaları ve online bilgisi.</summary>
    /// <returns>Satırlar.</returns>
    public static TheoryData<string, string[], bool> Rows() => new()
    {
        { "voice-local", ["Voice", "Merge"], false }, // Altyazı açıkken de render yok.
        { "voice-online", ["Voice", "Merge"], true },
        { "engine", ["Code", "Render", "Merge"], true },
        { "image-local", ["Images", "Render", "Merge"], false },
        { "image-online", ["Images", "Render", "Merge"], true },
        { "subtitles", ["Merge"], false },
        { "progress", ["Merge"], false },
        { "silent", ["Merge"], false }, // Altyazı açık olsa da render yok.
        { "voice+progress", ["Voice", "Merge"], false },
        { "engine+image", ["Code", "Images", "Render", "Merge"], true },
        { "none", [], false },
    };

    /// <summary>AC-1: Req 1 tablosu.</summary>
    /// <param name="change">Değişiklik.</param>
    /// <param name="expected">Beklenen aşamalar.</param>
    /// <param name="online">Online çağrı.</param>
    [Theory]
    [MemberData(nameof(Rows))]
    public void Affected_MatchesTable(string change, string[] expected, bool online)
    {
        var next = change switch
        {
            "voice-local" => Base with { AudioModel = Chatterbox },
            "voice-online" => Base with { AudioModel = ElevenLabs },
            "engine" => Base with { Engine = "local/remotion" },
            "image-local" => Base with { ImageModel = Flux },
            "image-online" => Base with { ImageModel = GptImage },
            "subtitles" => Base with { Subtitles = false },
            "progress" => Base with { ProgressBar = true },
            "silent" => Base with { AudioModel = null, Subtitles = false },
            "voice+progress" => Base with { AudioModel = Chatterbox, ProgressBar = true },
            "engine+image" => Base with { Engine = "local/remotion", ImageModel = Flux },
            _ => Base,
        };

        var stages = VersionStages.Affected(Base, next);

        stages.Select(s => s.ToString()).ShouldBe(expected);
        VersionStages.Online(stages, next).ShouldBe(online);
    }

    /// <summary>AC-1: yalnızca değişen girdinin imzası değişir — ses değişince analiz/kod/görsel/render imzaları aynı; motor değişince kod ve render.</summary>
    [Fact]
    public void Signatures_ChangeOnlyWithTheirInputs()
    {
        var a = VersionStages.Signatures("metin", Base);
        var voice = VersionStages.Signatures("metin", Base with { AudioModel = Chatterbox });
        var engine = VersionStages.Signatures("metin", Base with { Engine = "local/remotion" });
        var text = VersionStages.Signatures("başka metin", Base);

        foreach (var stage in new[] { PipelineStage.Analysis, PipelineStage.Code, PipelineStage.Images, PipelineStage.Render })
        {
            voice[stage].ShouldBe(a[stage]);
        }

        voice[PipelineStage.Voice].ShouldNotBe(a[PipelineStage.Voice]);
        engine[PipelineStage.Code].ShouldNotBe(a[PipelineStage.Code]);
        engine[PipelineStage.Render].ShouldNotBe(a[PipelineStage.Render]);
        engine[PipelineStage.Voice].ShouldBe(a[PipelineStage.Voice]);
        text.Values.Intersect(a.Values).ShouldBeEmpty();
        VersionStages.Signatures("metin", Base with { AudioModel = null }).ContainsKey(PipelineStage.Voice).ShouldBeFalse();
    }

    /// <summary>
    /// Çalışacak küme = tablo ∪ çıktısı geçerli olmayanlar: aynı cihazda tüm çıktılar varsa yalnızca tablo; başka cihazda yerel aşamalar da;
    /// imzası tutmayan kod (kaybolmuş çıktı) bağımlısı render'la birlikte online aşama olarak kümeye girer (önizleme bunu gösterir).
    /// </summary>
    [Fact]
    public void RunSet_AddsStagesWhoseOutputIsNotReusable()
    {
        var device = Guid.NewGuid();
        var signatures = VersionStages.Signatures("metin", Base);
        var outputs = new StageOutputs
        {
            ConceptId = Guid.NewGuid(),
            TextHash = "x",
            Plan = [],
            Codes = [],
            LocalDevice = device,
            LocalDone = [PipelineStage.Images, PipelineStage.Voice, PipelineStage.Render],
            Signatures = signatures,
        };
        var next = Base with { AudioModel = Chatterbox };
        var desired = VersionStages.Signatures("metin", next);
        var affected = VersionStages.Affected(Base, next);

        VersionStages.RunSet(next, outputs, desired, device, affected).ShouldBe([PipelineStage.Voice, PipelineStage.Merge]);
        VersionStages.RunSet(next, outputs, desired, Guid.NewGuid(), affected)
            .ShouldBe([PipelineStage.Images, PipelineStage.Voice, PipelineStage.Render, PipelineStage.Merge]);
        var lost = outputs with { Signatures = signatures.Where(p => p.Key != PipelineStage.Code).ToDictionary(p => p.Key, p => p.Value) };
        var withCode = VersionStages.RunSet(next, lost, desired, device, affected);
        withCode.ShouldBe([PipelineStage.Code, PipelineStage.Voice, PipelineStage.Render, PipelineStage.Merge]); // Kodun bağımlısı render.
        VersionStages.Online(withCode, next).ShouldBeTrue();
        VersionStages.All(Base with { ImageModel = null, AudioModel = null })
            .ShouldBe([PipelineStage.Analysis, PipelineStage.Code, PipelineStage.Render, PipelineStage.Merge]);
    }
}
