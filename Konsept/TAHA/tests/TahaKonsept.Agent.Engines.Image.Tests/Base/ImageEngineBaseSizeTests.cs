using Shouldly;
using TahaKonsept.Agent.Engines.Image.Base;
using TahaKonsept.Agent.Engines.Image.Catalog.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.Base;

/// <summary>
/// Üretim boyutu planı (<see cref="ImageEngineBase.PlanGenerationSize"/>): bütçe yoksa/aşılmıyorsa aynen; aşılırsa oran korunarak 64'ün katına küçülür.
/// Katalog: ~1 MP'de eğitilmiş SDXL ve SD3.5 bütçeli, Z-Image ve FLUX.2 1080x1920'de doğrudan üretir.
/// </summary>
public sealed class ImageEngineBaseSizeTests
{
    /// <summary>Plan örnekleri.</summary>
    /// <param name="w">İstenen genişlik.</param>
    /// <param name="h">İstenen yükseklik.</param>
    /// <param name="budget">Bütçe.</param>
    /// <param name="ew">Beklenen genişlik.</param>
    /// <param name="eh">Beklenen yükseklik.</param>
    [Theory]
    [InlineData(1080, 1920, null, 1080, 1920)]
    [InlineData(1080, 1920, 1048576, 768, 1344)]
    [InlineData(1920, 1080, 1048576, 1344, 768)]
    [InlineData(512, 768, 1048576, 512, 768)]
    [InlineData(1080, 1920, 16, 64, 64)]
    public void Plan_scales_into_budget_keeping_aspect(int w, int h, int? budget, int ew, int eh)
    {
        var (gw, gh) = ImageEngineBase.PlanGenerationSize(w, h, budget);

        (gw, gh).ShouldBe((ew, eh));
        if (budget is { } b && b >= 64 * 64)
        {
            ((long)gw * gh).ShouldBeLessThanOrEqualTo(b);
        }
    }

    /// <summary>Katalogda SDXL ve SD3.5 bütçeli; Z-Image ve FLUX.2 değil.</summary>
    [Fact]
    public void Catalog_budgets_only_low_native_resolution_models()
    {
        var catalog = new EmbeddedImageModelCatalog();

        catalog.Find("sdxl-lightning")!.MaxPixels.ShouldBe(1048576);
        catalog.Find("sd35-medium")!.MaxPixels.ShouldBe(1048576);
        catalog.Find("z-image-turbo")!.MaxPixels.ShouldBeNull();
        catalog.Find("flux2-klein-4b")!.MaxPixels.ShouldBeNull();
    }
}
