using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Storage.Base;
using TahaKonsept.Agent.Core.Videos.Abstract;

namespace TahaKonsept.Agent.Core.Videos.Concrete;

/// <summary>
/// <c>Videos/index.json</c> tabanlı video dizini (bellekte önbellekli, her değişiklikte atomik yazılır). Dosya okunamazsa (kilitli) sonuç
/// önbelleğe alınmaz: okumalar o an boş liste görür, değişiklikler <see cref="IOException"/> ile reddedilir — böylece tek yeni kayıtla
/// dizinin üzerine yazılıp önceki videolar kaybolmaz.
/// </summary>
public sealed class VideoIndex : JsonFileStoreBase<List<VideoEntry>>, IVideoIndex
{
    /// <summary>Dizin dosyasının adı.</summary>
    public const string FileName = "index.json";

    private List<VideoEntry>? _entries;

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    public VideoIndex(IAgentPaths paths)
        : base(Path.Combine(paths.VideosDir, FileName))
    {
    }

    /// <inheritdoc />
    public VideoEntry? Find(Guid videoId)
    {
        lock (Gate)
        {
            return Snapshot().FirstOrDefault(e => e.VideoId == videoId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<VideoEntry> GetAll()
    {
        lock (Gate)
        {
            return Snapshot().OrderByDescending(e => e.CreatedAtUtc).ToList();
        }
    }

    /// <inheritdoc />
    public void Upsert(VideoEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (Gate)
        {
            var entries = Entries();
            entries.RemoveAll(e => e.VideoId == entry.VideoId);
            entries.Add(entry);
            Write(entries);
        }
    }

    /// <inheritdoc />
    public bool Remove(Guid videoId)
    {
        lock (Gate)
        {
            var entries = Entries();
            var removed = entries.RemoveAll(e => e.VideoId == videoId) > 0;
            if (removed)
            {
                Write(entries);
            }

            return removed;
        }
    }

    private List<VideoEntry> Snapshot()
    {
        if (_entries is not null)
        {
            return _entries;
        }

        return TryRead(out var stored) ? _entries = stored ?? [] : [];
    }

    private List<VideoEntry> Entries()
    {
        if (_entries is null)
        {
            if (!TryRead(out var stored))
            {
                throw new IOException("Video dizini (" + FileName + ") okunamadı; üzerine yazılmadı. Dosyayı kilitleyen programı kapatıp yeniden deneyin.");
            }

            _entries = stored ?? [];
        }

        return _entries;
    }
}
