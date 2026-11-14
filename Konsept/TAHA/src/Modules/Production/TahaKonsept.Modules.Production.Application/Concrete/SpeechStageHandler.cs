using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Speech</c> aşaması. Kapalı → sunucuda sessiz geçiş (sahne süreleri plandan). Online → her sahnenin okunuşu
/// <see cref="IMediaGateway.SynthesizeSpeechAsync"/> ile WAV olarak seslendirilir, <see cref="IWavCombiner"/> ile tek <c>speech.wav</c> yapılır ve
/// <see cref="IArtifactStore"/>'a konur (Align/Compose bu artefaktı indirir). Yerel → ajana <see cref="SpeechStagePayload"/>; sonuç
/// <see cref="SpeechStageResult"/>'tan okunur. Hash girdisi: Normalize çıktı hash'i + kip + model + ses + hız + referans ses + dil.
/// </summary>
public sealed class SpeechStageHandler : StageHandlerBase
{
    /// <summary>Boş sahne sessizliği (sn).</summary>
    public const double EmptySceneSilenceSec = 0.6;

    private readonly IMediaGateway _media;
    private readonly IArtifactStore _artifacts;
    private readonly IWavCombiner _wav;

    /// <summary>Oluşturur.</summary>
    /// <param name="media">Online ses geçidi.</param>
    /// <param name="artifacts">Artefakt deposu.</param>
    /// <param name="wav">WAV birleştirici.</param>
    public SpeechStageHandler(IMediaGateway media, IArtifactStore artifacts, IWavCombiner wav)
    {
        _media = media ?? throw new ArgumentNullException(nameof(media));
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        _wav = wav ?? throw new ArgumentNullException(nameof(wav));
    }

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Speech;

    /// <summary>Kipe göre yer.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.SpeechMode == MediaMode.Local ? StageLocation.Agent : StageLocation.Server;
    }

    /// <summary>Kapalıyken deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Deterministikse <c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.SpeechMode == MediaMode.Off;
    }

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        var s = context.Settings;
        return s.SpeechMode == MediaMode.Off
            ? new { mode = s.SpeechMode, durations = context.Outputs.ScenePlan.Scenes.Select(x => x.DurationSec).ToList() }
            : new
            {
                normalized = context.Outputs.OutputHashOf(PipelineStage.Normalize),
                mode = s.SpeechMode,
                model = s.SpeechModelKey,
                voice = s.VoiceId,
                speed = s.SpeechSpeed,
                reference = s.ReferenceAudioArtifactId,
                language = context.Language,
            };
    }

    /// <summary>Sessiz geçiş ya da online seslendirme.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override async Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mode = context.Settings.SpeechMode;
        if (mode == MediaMode.Off)
        {
            return Output(Silent(context), "Ses kapalı; sessiz video");
        }

        if (mode != MediaMode.Online)
        {
            return ProductionErrors.WrongLocation(Stage.ToString());
        }

        if (string.IsNullOrWhiteSpace(context.Settings.SpeechModelKey))
        {
            return ProductionErrors.ModelMissing("Ses");
        }

        var segments = Segments(context);
        if (segments is null)
        {
            return ProductionErrors.MissingOutput(nameof(PipelineStage.Normalize));
        }

        decimal cost = 0;
        var parts = new List<byte[]?>(segments.Count);
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment.SpokenText))
            {
                parts.Add(null);
                continue;
            }

            var spoken = await _media.SynthesizeSpeechAsync(
                context.Settings.SpeechModelKey,
                new SpeechRequest
                {
                    Context = new CallContext(context.UserId, "speech"),
                    Text = segment.SpokenText,
                    Voice = context.Settings.VoiceId,
                    Language = ToLocale(context.Language),
                    Speed = (decimal)(context.Settings.SpeechSpeed > 0 ? context.Settings.SpeechSpeed : 1.0),
                    Format = "wav",
                },
                cancellationToken);
            if (spoken.IsFailure)
            {
                return spoken.Error;
            }

            cost += spoken.Value.CostUsd;
            request.Usage.Add(spoken.Value.CostUsd);
            parts.Add(spoken.Value.Audio);
        }

        var combined = _wav.Combine(parts, EmptySceneSilenceSec);
        if (combined.IsFailure)
        {
            return combined.Error;
        }

        using var stream = new MemoryStream(combined.Value.Audio, writable: false);
        var saved = await _artifacts.SaveAsync(context.UserId, context.JobId, AgentArtifactNames.SpeechAudio, "audio/wav", stream, null, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var start = 0d;
        var timings = new List<SpeechSceneTiming>(segments.Count);
        for (var i = 0; i < segments.Count; i++)
        {
            var duration = combined.Value.DurationsSec[i];
            timings.Add(new SpeechSceneTiming { SceneIndex = segments[i].SceneIndex, StartSec = start, DurationSec = duration, FileName = AgentArtifactNames.SceneAudio(segments[i].SceneIndex) });
            start += duration;
        }

        var output = new SpeechOutput
        {
            Mode = MediaMode.Online,
            TotalDurationSec = Math.Round(combined.Value.TotalSec, 3),
            Scenes = timings,
            AudioArtifactName = AgentArtifactNames.SpeechAudio,
        };
        return Output(output, "Seslendirme hazır (" + Math.Round(combined.Value.TotalSec) + " sn)", cost, [saved.Value]);
    }

    /// <summary>Yerel seslendirme komutu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public override Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var segments = Segments(context);
        if (segments is null)
        {
            return Task.FromResult(Result.Failure<StageCommand>(ProductionErrors.MissingOutput(nameof(PipelineStage.Normalize))));
        }

        if (string.IsNullOrWhiteSpace(context.Settings.SpeechModelKey))
        {
            return Task.FromResult(Result.Failure<StageCommand>(ProductionErrors.ModelMissing("Ses")));
        }

        var payload = new SpeechStagePayload
        {
            EngineKey = context.Settings.SpeechModelKey,
            Language = context.Language,
            VoiceId = context.Settings.VoiceId,
            Speed = context.Settings.SpeechSpeed > 0 ? context.Settings.SpeechSpeed : 1.0,
            Segments = segments.Select(s => new SpeechSegmentItem { SceneIndex = s.SceneIndex, DisplayText = s.DisplayText, SpokenText = s.SpokenText }).ToList(),
        };
        return Task.FromResult(Result.Success(Command(context, request, payload)));
    }

    /// <summary>Yerel seslendirme sonucunu okur.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public override Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var result = ProductionJson.Read<SpeechStageResult>(report.OutputJson);
        if (result is null || result.Scenes.Count == 0)
        {
            return Error.Failure("production.speech_result_invalid", "Masaüstü programı seslendirme zamanlarını göndermedi.");
        }

        var output = new SpeechOutput { Mode = MediaMode.Local, TotalDurationSec = result.TotalDurationSec, Scenes = result.Scenes };
        return Output(output, "Seslendirme hazır (" + Math.Round(result.TotalDurationSec) + " sn)");
    }

    /// <summary>Dil kodunu yerel ayara çevirir (<c>tr</c> → <c>tr-TR</c>).</summary>
    /// <param name="language">Dil.</param>
    /// <returns>Yerel ayar.</returns>
    public static string ToLocale(string language) => language.ToLowerInvariant() switch
    {
        "tr" => "tr-TR",
        "en" => "en-US",
        "de" => "de-DE",
        _ => language,
    };

    private static SpeechOutput Silent(PipelineContext context)
    {
        var start = 0d;
        var timings = new List<SpeechSceneTiming>();
        foreach (var scene in context.Outputs.ScenePlan.Scenes)
        {
            timings.Add(new SpeechSceneTiming { SceneIndex = scene.Index, StartSec = start, DurationSec = scene.DurationSec });
            start += scene.DurationSec;
        }

        return new SpeechOutput { Mode = MediaMode.Off, Silent = true, TotalDurationSec = start, Scenes = timings };
    }

    private static IReadOnlyList<NormalizedSegment>? Segments(PipelineContext context) =>
        context.Outputs.OutputOf<NormalizeOutput>(PipelineStage.Normalize)?.Segments;
}
