using Shouldly;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.Catalog;

/// <summary>Gömülü model kataloğunun bütünlük testleri.</summary>
public sealed class EmbeddedImageModelCatalogTests
{
    private readonly EmbeddedImageModelCatalog _catalog = new();

    /// <summary>İstenen yedi model anahtarı katalogda.</summary>
    [Fact]
    public void Catalog_contains_all_required_model_keys()
    {
        _catalog.Models.Select(m => m.Key).ShouldBe(
            ["z-image-turbo", "flux2-klein-4b", "flux2-klein-9b", "flux3-klein", "sd35-medium", "sdxl-lightning", "qwen-image"],
            ignoreOrder: true);
    }

    /// <summary>Her dosyanın deposu, yolu, boyutu ve SHA-256'sı dolu.</summary>
    [Fact]
    public void Every_file_has_repo_size_and_hash()
    {
        foreach (var file in _catalog.Models.SelectMany(m => m.Files))
        {
            file.Repo.ShouldContain('/');
            file.RepoPath.ShouldNotBeNullOrWhiteSpace();
            file.LocalPath.ShouldNotStartWith("/");
            file.LocalPath.ShouldNotContain("..");
            file.SizeBytes.ShouldBeGreaterThan(1_000_000);
            file.Sha256.ShouldNotBeNull().Length.ShouldBe(64);
        }
    }

    /// <summary>Aynı yerel yolu paylaşan dosyalar aynı içeriği gösterir (paylaşılan VAE gibi).</summary>
    [Fact]
    public void Shared_local_paths_point_to_identical_files()
    {
        foreach (var group in _catalog.Models.SelectMany(m => m.Files).GroupBy(f => f.LocalPath))
        {
            group.Select(f => f.Sha256).Distinct().Count().ShouldBe(1, group.Key);
        }
    }

    /// <summary>Her modelin bir ana modeli (difüzyon ya da tam checkpoint) var.</summary>
    [Fact]
    public void Every_model_has_a_main_weight_file()
    {
        foreach (var model in _catalog.Models)
        {
            model.Files.Count(f => f.Role is ImageModelFileRole.DiffusionModel or ImageModelFileRole.FullModel).ShouldBe(1, model.Key);
        }
    }

    /// <summary>Varsayılan Z-Image modeli 6 GB kartta çalışacak boyutta ve 8 GB'tan az indirme.</summary>
    [Fact]
    public void Z_image_turbo_is_small_enough_for_6gb()
    {
        var model = _catalog.Find("Z-IMAGE-TURBO").ShouldNotBeNull();
        model.MinVramMb.ShouldBeLessThanOrEqualTo(6144);
        model.TotalSizeBytes.ShouldBeLessThan(8L * 1024 * 1024 * 1024);
        model.DefaultSteps.ShouldBe(8);
    }
}
