using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Core.Execution.Concrete;
using TahaKonsept.Agent.Core.Shell.Abstract;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.Agent.Core.Videos;
using TahaKonsept.Agent.Core.Videos.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Videos;

/// <summary>
/// Video dizini ve kütüphanesi: okunamayan (kilitli) dizin boş sayılıp üzerine yazılmaz, bozuk dizin yedeklenir; paylaşılan çalışma alanı
/// (aynı JobId'li başka video ya da çalışan aşama) video silinince silinmez.
/// </summary>
public sealed class VideoLibraryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly TestAgentPaths _paths;

    /// <summary>Kurar.</summary>
    public VideoLibraryTests()
    {
        _paths = new TestAgentPaths(_temp.Path);
        Directory.CreateDirectory(_paths.VideosDir);
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Dizin dosyası kilitliyken yeni kayıt reddedilir; eski kayıtlar korunur ve kilit kalkınca yeni kayıt eklenir.</summary>
    [Fact]
    public void Locked_index_is_not_overwritten()
    {
        var first = Entry(Guid.NewGuid());
        new VideoIndex(_paths).Upsert(first);
        var index = new VideoIndex(_paths);
        var file = Path.Combine(_paths.VideosDir, VideoIndex.FileName);

        using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            index.GetAll().ShouldBeEmpty();
            Should.Throw<IOException>(() => index.Upsert(Entry(Guid.NewGuid())));
        }

        var second = Entry(Guid.NewGuid());
        index.Upsert(second);
        new VideoIndex(_paths).GetAll().Select(e => e.VideoId).ShouldBe([second.VideoId, first.VideoId], ignoreOrder: true);
    }

    /// <summary>Bozuk dizin .bad uzantılı yedeğe taşınır, boş başlanır.</summary>
    [Fact]
    public void Corrupt_index_is_backed_up()
    {
        File.WriteAllText(Path.Combine(_paths.VideosDir, VideoIndex.FileName), "{ bozuk");

        var index = new VideoIndex(_paths);
        index.Upsert(Entry(Guid.NewGuid()));

        Directory.GetFiles(_paths.VideosDir, "index.json.*.bad").ShouldHaveSingleItem();
        index.GetAll().Count.ShouldBe(1);
    }

    /// <summary>Aynı çalışma alanını paylaşan başka video ya da çalışan aşama varken alan silinmez; son video silinince silinir.</summary>
    [Fact]
    public void Shared_workspace_is_kept_until_last_user()
    {
        var workspace = Guid.NewGuid();
        var dir = Path.Combine(_paths.WorkspaceDir, workspace.ToString("N"));
        Directory.CreateDirectory(dir);
        var usage = new WorkspaceUsage();
        var index = new VideoIndex(_paths);
        var library = new VideoLibrary(index, Substitute.For<IShellLauncher>(), _paths, usage, NullLogger<VideoLibrary>.Instance);
        var v1 = Entry(workspace);
        var v2 = Entry(workspace);
        library.Register(v1);
        library.Register(v2);

        library.Delete(v1.VideoId).IsSuccess.ShouldBeTrue();
        Directory.Exists(dir).ShouldBeTrue();

        usage.Enter(workspace);
        library.Delete(v2.VideoId).IsSuccess.ShouldBeTrue();
        Directory.Exists(dir).ShouldBeTrue();

        var v3 = Entry(workspace);
        library.Register(v3);
        usage.Exit(workspace);
        library.Delete(v3.VideoId).IsSuccess.ShouldBeTrue();
        Directory.Exists(dir).ShouldBeFalse();
    }

    private VideoEntry Entry(Guid jobId)
    {
        var id = Guid.NewGuid();
        var path = Path.Combine(_paths.VideosDir, id.ToString("N") + ".mp4");
        File.WriteAllBytes(path, [1]);
        return new VideoEntry { VideoId = id, JobId = jobId, Title = "Video", VideoPath = path, CreatedAtUtc = DateTimeOffset.UtcNow };
    }
}
