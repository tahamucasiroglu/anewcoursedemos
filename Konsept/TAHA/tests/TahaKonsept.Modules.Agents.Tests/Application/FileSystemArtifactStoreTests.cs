using Shouldly;
using TahaKonsept.BuildingBlocks.Infrastructure.Storage.Concrete;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Agents.Infrastructure.Storage.Concrete;
using TahaKonsept.Modules.Agents.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Application;

/// <summary>
/// Dosya sistemi artefakt deposu: tek artefakt onayı (silme) iş klasörünü silmez (aynı işin başka dosyası yazılabilsin),
/// işin tüm artefaktları silinince klasör temizlenir; klasör dışarıdan silinse de kayıt yeniden yazılabilir.
/// </summary>
public sealed class FileSystemArtifactStoreTests : IDisposable
{
    private readonly AgentsTestHarness _h = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tk-artifacts-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Tek artefakt silinince dosyası gider, iş klasörü kalır; toplu silmede klasör de gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Single_delete_keeps_job_folder_until_job_cleanup()
    {
        var store = CreateStore();
        var jobId = Guid.NewGuid();
        var first = (await store.SaveAsync(_h.UserId, jobId, "a.png", "image/png", new MemoryStream([1, 2, 3]), 0, Ct)).Value;
        var folder = Path.Combine(_root, "artifacts", jobId.ToString("N"));

        (await store.DeleteAsync(_h.UserId, first.ArtifactId, Ct)).IsSuccess.ShouldBeTrue();

        Directory.Exists(folder).ShouldBeTrue();
        Directory.EnumerateFiles(folder).ShouldBeEmpty();
        var second = await store.SaveAsync(_h.UserId, jobId, "b.png", "image/png", new MemoryStream([4]), 1, Ct);
        second.IsSuccess.ShouldBeTrue();

        (await store.DeleteJobArtifactsAsync(_h.UserId, jobId, Ct)).Value.ShouldBe(1);
        Directory.Exists(folder).ShouldBeFalse();
    }

    /// <summary>İş klasörü dışarıdan silinmiş olsa da yeni artefakt kaydedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Save_recreates_missing_job_folder()
    {
        var store = CreateStore();
        var jobId = Guid.NewGuid();
        await store.SaveAsync(_h.UserId, jobId, "a.png", "image/png", new MemoryStream([1]), 0, Ct);
        Directory.Delete(Path.Combine(_root, "artifacts", jobId.ToString("N")), recursive: true);

        var saved = await store.SaveAsync(_h.UserId, jobId, "b.png", "image/png", new MemoryStream([2]), 1, Ct);

        saved.IsSuccess.ShouldBeTrue();
        var opened = await store.OpenAsync(_h.UserId, saved.Value.ArtifactId, Ct);
        opened.IsSuccess.ShouldBeTrue();
        await opened.Value.Content.DisposeAsync();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose()
    {
        _h.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private FileSystemArtifactStore CreateStore() => new(new AppDataLocation(_root), new StoredArtifactRepository(_h.Db));
}
