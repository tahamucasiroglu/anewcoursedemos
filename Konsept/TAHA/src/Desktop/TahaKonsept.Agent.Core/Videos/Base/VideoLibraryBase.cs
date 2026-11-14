using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Shell.Abstract;
using TahaKonsept.Agent.Core.Videos.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Videos.Base;

/// <summary>
/// Video kütüphanesi tabanı: dizin (<see cref="IVideoIndex"/>) + kabuk (<see cref="IShellLauncher"/>) üzerinden işlemler.
/// Silme sırasında kilitli dosya hatası <c>agent.delete_failed</c> döner; kayıt yine de kaldırılır.
/// </summary>
public abstract class VideoLibraryBase : IVideoLibrary
{
    private readonly IVideoIndex _index;
    private readonly IShellLauncher _shell;
    private readonly IAgentPaths _paths;

    /// <summary>Oluşturur.</summary>
    /// <param name="index">Video dizini.</param>
    /// <param name="shell">Kabuk başlatıcı.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    protected VideoLibraryBase(IVideoIndex index, IShellLauncher shell, IAgentPaths paths)
    {
        _index = index;
        _shell = shell;
        _paths = paths;
    }

    /// <inheritdoc />
    public VideoEntry? Find(Guid videoId) => _index.Find(videoId);

    /// <inheritdoc />
    public void Register(VideoEntry entry) => _index.Upsert(entry);

    /// <inheritdoc />
    public Result OpenFolder(Guid videoId) => WithEntry(videoId, e => _shell.OpenFolderAndSelect(e.VideoPath));

    /// <inheritdoc />
    public Result OpenInSystemPlayer(Guid videoId) => WithEntry(videoId, e => _shell.OpenWithDefaultApp(e.VideoPath));

    /// <inheritdoc />
    public Result Delete(Guid videoId)
    {
        var entry = _index.Find(videoId);
        if (entry is null)
        {
            return Result.Success();
        }

        var failures = new List<string>();
        TryDeleteFile(entry.VideoPath, failures);
        if (!string.IsNullOrEmpty(entry.ThumbnailPath))
        {
            TryDeleteFile(entry.ThumbnailPath, failures);
        }

        if (entry.JobId != Guid.Empty)
        {
            TryDeleteDirectory(Path.Combine(_paths.WorkspaceDir, entry.JobId.ToString("N")), failures);
        }

        _index.Remove(videoId);
        OnDeleted(entry, failures);
        return failures.Count == 0
            ? Result.Success()
            : Error.Failure("agent.delete_failed", "Bazı dosyalar silinemedi: " + string.Join(", ", failures));
    }

    /// <summary>Silme sonrası çağrılır (loglama için).</summary>
    /// <param name="entry">Silinen kayıt.</param>
    /// <param name="failures">Silinemeyen yollar.</param>
    protected virtual void OnDeleted(VideoEntry entry, IReadOnlyList<string> failures)
    {
    }

    private Result WithEntry(Guid videoId, Func<VideoEntry, Result> action)
    {
        var entry = _index.Find(videoId);
        return entry is null
            ? Error.NotFound("agent.video_not_found", "Video bu bilgisayarda bulunamadı.")
            : action(entry);
    }

    private static void TryDeleteFile(string path, List<string> failures)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(path);
        }
    }

    private static void TryDeleteDirectory(string path, List<string> failures)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(path);
        }
    }
}
