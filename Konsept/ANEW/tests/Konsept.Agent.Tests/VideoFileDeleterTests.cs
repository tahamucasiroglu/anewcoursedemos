using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Kalıcı silme, ajan tarafı (spec 0019 Req 4; AC-4, AC-6): video klasöründeki dosyalar gerçekten silinir; diskte olmayan dosya silinmiş sayılır;
/// silinemeyen dosya (açık dosyayı taklit eden alt sınıf) "kalan" olarak döner, diğerleri yine silinir ve tekrar denemede kalan da gider;
/// klasör dışını gösteren ya da <c>.mp4</c> olmayan ada asla dokunulmaz.
/// </summary>
public sealed class VideoFileDeleterTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "anewconcept-delete-" + Guid.NewGuid().ToString("N"));

    /// <summary>Initializes a new instance of the <see cref="VideoFileDeleterTests"/> class.</summary>
    public VideoFileDeleterTests()
    {
        Directory.CreateDirectory(_folder);
    }

    /// <summary>AC-4: tüm sürüm dosyaları silinir; olmayan dosya hata saymaz (kalan boş).</summary>
    [Fact]
    public void Delete_AllVersions_MissingCountsAsDeleted()
    {
        Create("v1.mp4");
        Create("v2.mp4");
        Create("baska.mp4");

        var reply = new VideoFileDeleter(() => _folder).Delete(new AgentDeleteFilesRequest(Guid.NewGuid(), ["v1.mp4", "v2.mp4", "zaten-yok.mp4"]));

        reply.Remaining.ShouldBeEmpty();
        File.Exists(Path.Combine(_folder, "v1.mp4")).ShouldBeFalse();
        File.Exists(Path.Combine(_folder, "v2.mp4")).ShouldBeFalse();
        File.Exists(Path.Combine(_folder, "baska.mp4")).ShouldBeTrue(); // İstenmeyen dosyaya dokunulmaz.
    }

    /// <summary>AC-6: silinemeyen dosya kalan olarak döner, diğeri silinir; tekrar denemede (dosya kapandı) kalan da silinir.</summary>
    [Fact]
    public void Delete_LockedFile_Remaining_ThenRetryDeletes()
    {
        Create("acik.mp4");
        Create("kapali.mp4");
        var deleter = new LockingFileDeleter(_folder) { Locked = "acik.mp4" };
        var request = new AgentDeleteFilesRequest(Guid.NewGuid(), ["acik.mp4", "kapali.mp4"]);

        deleter.Delete(request).Remaining.ShouldBe(["acik.mp4"]);
        File.Exists(Path.Combine(_folder, "acik.mp4")).ShouldBeTrue();
        File.Exists(Path.Combine(_folder, "kapali.mp4")).ShouldBeFalse();

        deleter.Locked = null;
        deleter.Delete(request).Remaining.ShouldBeEmpty();
        File.Exists(Path.Combine(_folder, "acik.mp4")).ShouldBeFalse();
    }

    /// <summary>H-1: klasör dışını gösteren, akış adlı ya da .mp4 olmayan ada dokunulmaz.</summary>
    [Fact]
    public void Delete_UnsafeNames_NeverTouched()
    {
        var outside = Path.Combine(Path.GetTempPath(), "anewconcept-outside-" + Guid.NewGuid().ToString("N") + ".mp4");
        File.WriteAllBytes(outside, [1]);
        Create("ayar.json");
        try
        {
            var reply = new VideoFileDeleter(() => _folder).Delete(
                new AgentDeleteFilesRequest(Guid.NewGuid(), ["..\\" + Path.GetFileName(outside), outside, "ayar.json", "v.mp4:ads", string.Empty]));

            reply.Remaining.ShouldBeEmpty();
            File.Exists(outside).ShouldBeTrue();
            File.Exists(Path.Combine(_folder, "ayar.json")).ShouldBeTrue();
        }
        finally
        {
            File.Delete(outside);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private void Create(string name) => File.WriteAllBytes(Path.Combine(_folder, name), [1, 2, 3]);
}
