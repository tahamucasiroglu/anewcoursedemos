using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.Agent.Core.Media.Abstract;
using TahaKonsept.Agent.Core.Videos;
using TahaKonsept.Agent.Core.Videos.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>
/// <c>Mux</c> aşaması (son): <c>compose/composed.mp4</c>'yu <c>Videos/&lt;başlık&gt; (&lt;kısa id&gt;).mp4</c> olarak yerleştirir,
/// küçük resmi (<c>Videos/.thumbs/&lt;videoId&gt;.jpg</c>) hazırlar, süreyi ölçer ve yerel video dizinine kaydeder.
/// Aynı videonun eski sürüm dosyası yeni sürümle değiştirilir. Çıktı JSON'u <see cref="VideoEntry"/>'dir.
/// </summary>
public sealed class MuxStageExecutor : StageExecutorBase<MuxStagePayload>
{
    /// <summary>Küçük resim klasörü adı (Videos altında).</summary>
    public const string ThumbnailsDir = ".thumbs";

    private readonly IVideoLibrary _library;
    private readonly IMediaProbe _media;
    private readonly IAgentPaths _paths;
    private readonly TimeProvider _time;

    /// <summary>Oluşturur.</summary>
    /// <param name="library">Video kütüphanesi.</param>
    /// <param name="media">Medya yardımcısı.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="time">Zaman sağlayıcı.</param>
    public MuxStageExecutor(IVideoLibrary library, IMediaProbe media, IAgentPaths paths, TimeProvider time)
    {
        _library = library;
        _media = media;
        _paths = paths;
        _time = time;
    }

    /// <inheritdoc />
    public override PipelineStage Stage => PipelineStage.Mux;

    /// <inheritdoc />
    protected override string GetOutputDirectory(StageExecutionContext context, MuxStagePayload payload) =>
        Path.Combine(context.JobDir, WorkspaceLayout.FinalDir);

    /// <inheritdoc />
    protected override async Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, MuxStagePayload payload, CancellationToken ct)
    {
        var localSource = WorkspaceLayout.ComposedVideoPath(context.JobDir);
        var source = IsComplete(localSource) ? localSource : context.FindInput(AgentArtifactNames.ComposedVideo);
        if (source is null)
        {
            return Error.NotFound("agent.input_missing", "Birleştirilmiş video bulunamadı.");
        }

        var videoId = context.Command.VideoId == Guid.Empty ? context.Command.JobId : context.Command.VideoId;
        var title = string.IsNullOrWhiteSpace(payload.Title) ? "video" : payload.Title.Trim();
        if (title.Length > 80)
        {
            title = title[..80];
        }

        var fileName = SafeFileName(title, "video") + " (" + videoId.ToString("N")[..8] + ").mp4";
        var destination = Path.Combine(_paths.VideosDir, fileName);

        context.Report(10, "Video kaydediliyor…");
        var previous = _library.Find(videoId);
        if (previous is not null && !string.Equals(previous.VideoPath, destination, StringComparison.OrdinalIgnoreCase) && File.Exists(previous.VideoPath))
        {
            File.Delete(previous.VideoPath);
        }

        var partial = PartialPath(destination);
        File.Copy(source, partial, overwrite: true);
        File.Move(partial, destination, overwrite: true);

        context.Report(60, "Küçük resim hazırlanıyor…");
        var thumbnail = await PrepareThumbnailAsync(context, videoId, destination, ct);
        var duration = await _media.GetDurationAsync(destination, ct);

        var entry = new VideoEntry
        {
            VideoId = videoId,
            JobId = context.Command.JobId,
            Title = title,
            VideoPath = destination,
            ThumbnailPath = thumbnail,
            DurationSec = duration.IsSuccess ? duration.Value : null,
            SizeBytes = new FileInfo(destination).Length,
            CreatedAtUtc = _time.GetUtcNow(),
        };
        _library.Register(entry);
        context.Report(100, "Video hazır.");
        var artifact = CreateArtifact(context, destination, "video", null, entry.DurationSec, new Dictionary<string, string>
        {
            ["videoId"] = videoId.ToString("D"),
            ["hasThumbnail"] = thumbnail is null ? "false" : "true",
        });
        return new StageExecutionOutput([artifact], Serialize(entry));
    }

    private async Task<string?> PrepareThumbnailAsync(StageExecutionContext context, Guid videoId, string videoPath, CancellationToken ct)
    {
        var directory = Path.Combine(_paths.VideosDir, ThumbnailsDir);
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, videoId.ToString("N") + ".jpg");
        var composed = WorkspaceLayout.ComposedThumbnailPath(context.JobDir);
        if (IsComplete(composed))
        {
            File.Copy(composed, target, overwrite: true);
            return target;
        }

        var extracted = await _media.ExtractThumbnailAsync(videoPath, target, 1.0, ct);
        return extracted.IsSuccess && File.Exists(target) ? target : null;
    }
}
