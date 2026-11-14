using System.Text.Json;
using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Abstractions.Align.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>
/// <c>Align</c> aşaması: seslendirme dosyasını (yerel <c>speech/speech.wav</c> ya da indirilen artefakt) segmentlerle hizalar,
/// sonucu <c>align/captions.json</c>'a yazar ve <see cref="AlignStageResult"/> döner. Hizalayıcı anahtarla seçilir
/// (yoksa <c>proportional</c>, o da yoksa ilk kayıtlı).
/// </summary>
public sealed class AlignStageExecutor : StageExecutorBase<AlignStagePayload>
{
    /// <summary>Varsayılan hizalayıcı anahtarı.</summary>
    public const string DefaultAlignerKey = "proportional";

    private readonly IEnumerable<IAligner> _aligners;

    /// <summary>Oluşturur.</summary>
    /// <param name="aligners">Hizalayıcılar.</param>
    public AlignStageExecutor(IEnumerable<IAligner> aligners)
    {
        _aligners = aligners;
    }

    /// <inheritdoc />
    public override PipelineStage Stage => PipelineStage.Align;

    /// <inheritdoc />
    protected override string GetOutputDirectory(StageExecutionContext context, AlignStagePayload payload) =>
        Path.Combine(context.JobDir, WorkspaceLayout.AlignDir);

    /// <inheritdoc />
    protected override Result Validate(AlignStagePayload payload, StageExecutionContext context) =>
        payload.Segments.Count == 0
            ? Error.Validation("agent.invalid_payload", "Hizalanacak metin yok.")
            : Result.Success();

    /// <inheritdoc />
    protected override async Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, AlignStagePayload payload, CancellationToken ct)
    {
        var captionsPath = WorkspaceLayout.CaptionsPath(context.JobDir);
        if (IsComplete(captionsPath) && TryReadExisting(captionsPath) is { } existing)
        {
            return new StageExecutionOutput([CreateArtifact(context, captionsPath, "json", null, null)], Serialize(existing));
        }

        var aligner = SelectByKey(_aligners, payload.AlignerKey, a => a.Key)
            ?? SelectByKey(_aligners, DefaultAlignerKey, a => a.Key)
            ?? _aligners.FirstOrDefault();
        if (aligner is null)
        {
            return Error.NotFound("agent.aligner_missing", "Bu bilgisayarda hizalayıcı yok.");
        }

        var audio = ResolveAudio(context, payload);
        if (audio is null)
        {
            return Error.NotFound("agent.input_missing", "Hizalama için seslendirme dosyası bulunamadı.");
        }

        context.Report(10, "Altyazı zamanlaması hesaplanıyor…");
        var segments = payload.Segments
            .Select(s => new CaptionSegment(s.DisplayText, s.SpokenText, s.StartSec, s.EndSec))
            .ToList();
        var aligned = await aligner.AlignAsync(new AlignRequest(audio, segments, payload.Language), ct);
        if (aligned.IsFailure)
        {
            return aligned.Error;
        }

        var result = new AlignStageResult
        {
            Words = aligned.Value.Select(w => new CaptionWordItem { Text = w.Text, StartMs = w.StartMs, EndMs = w.EndMs }).ToList(),
        };
        var json = Serialize(result);
        var partial = PartialPath(captionsPath);
        await File.WriteAllTextAsync(partial, json, ct);
        File.Move(partial, captionsPath, overwrite: true);
        context.Report(100, "Altyazı zamanlaması hazır.");
        return new StageExecutionOutput([CreateArtifact(context, captionsPath, "json", null, null)], json);
    }

    /// <summary>Altyazı dosyasını <see cref="CaptionWord"/> listesine çevirir (Compose aşaması da kullanır).</summary>
    /// <param name="path">Dosya.</param>
    /// <returns>Kelimeler (okunamazsa boş).</returns>
    public static IReadOnlyList<CaptionWord> ReadCaptionWords(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return [];
        }

        var result = TryReadExisting(path);
        return result is null ? [] : result.Words.Select(w => new CaptionWord(w.Text, w.StartMs, w.EndMs)).ToList();
    }

    private static AlignStageResult? TryReadExisting(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<AlignStageResult>(File.ReadAllText(path), ContractJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? ResolveAudio(StageExecutionContext context, AlignStagePayload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.AudioArtifactName))
        {
            return context.FindInput(payload.AudioArtifactName);
        }

        var local = WorkspaceLayout.SpeechAudioPath(context.JobDir);
        return IsComplete(local) ? local : context.FindInput(AgentArtifactNames.SpeechAudio);
    }
}
