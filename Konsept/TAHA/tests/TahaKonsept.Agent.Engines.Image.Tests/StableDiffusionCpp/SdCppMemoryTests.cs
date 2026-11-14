using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog.Concrete;
using TahaKonsept.Agent.Engines.Image.Gpu.Abstract;
using TahaKonsept.Agent.Engines.Image.Gpu.Base;
using TahaKonsept.Agent.Engines.Image.Gpu.Concrete;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.StableDiffusionCpp;

/// <summary>
/// Düşük VRAM: boş belleğe göre kip seçimi (Z-Image-Turbo ≈3 GB difüzyon: ≥5046 MiB standart, altı azaltılmış + <c>--max-vram</c>),
/// OOM sonrası en agresif kip, <c>nvidia-smi</c> çıktısının çözülmesi ve motorun OOM'da bir kez yeniden denemesi.
/// </summary>
public sealed class SdCppMemoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly SdCppMemoryPlanner _planner = new(new SdCppArgumentBuilder());
    private readonly ImageModelDefinition _zImage = new EmbeddedImageModelCatalog().Find("z-image-turbo")!;

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Yeterli boş VRAM ya da bilinmiyorsa standart düşük VRAM bayrakları (ayar kapalıysa hiç).</summary>
    [Fact]
    public void Ample_or_unknown_vram_uses_standard_flags()
    {
        var ample = _planner.Plan(_zImage, 5500, lowVramDefault: true, aggressive: false);
        ample.Level.ShouldBe(SdCppMemoryLevel.Standard);
        ample.Arguments.ShouldContain("--offload-to-cpu");

        _planner.Plan(_zImage, null, lowVramDefault: true, aggressive: false).Level.ShouldBe(SdCppMemoryLevel.Standard);
        _planner.Plan(_zImage, null, lowVramDefault: false, aggressive: false).Arguments.ShouldBeEmpty();
    }

    /// <summary>4,3 GB boşta azaltılmış kip: metin kodlayıcı CPU'da, VAE döşeme, flash attention, bütçe (4300 − 1152) / 1024 ≈ 3,07 GiB.</summary>
    [Fact]
    public void Low_vram_uses_reduced_mode_with_budget()
    {
        var plan = _planner.Plan(_zImage, 4300, lowVramDefault: true, aggressive: false);

        plan.Level.ShouldBe(SdCppMemoryLevel.Reduced);
        plan.Arguments.ShouldBe(["--backend", "te=cpu", "--flash-attn", "auto,diffusion=on", "--vae-tiling", "--max-vram", "3.07"]);
        plan.Arguments.ShouldNotContain("--offload-to-cpu");
    }

    /// <summary>En agresif kip: metin kodlayıcı + VAE CPU'da; çok az boşta en düşük bütçe, bilinmiyorsa 1,5 GiB.</summary>
    [Fact]
    public void Aggressive_mode_moves_text_encoder_and_vae_to_cpu()
    {
        var plan = _planner.Plan(_zImage, 3000, lowVramDefault: true, aggressive: true);
        plan.Level.ShouldBe(SdCppMemoryLevel.Minimal);
        plan.Arguments.ShouldContain("te=cpu,vae=cpu");
        plan.Arguments[^1].ShouldBe("1.42");

        _planner.Plan(_zImage, 1200, true, true).Arguments[^1].ShouldBe("0.5");
        _planner.Plan(_zImage, null, true, true).Arguments[^1].ShouldBe("1.5");
    }

    /// <summary>nvidia-smi çıktısı çözülür; hatalı çıktı null olur; NVIDIA yoksa süreç başlatılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Nvidia_smi_output_is_parsed()
    {
        GpuMemoryProbeBase.Parse("3703\r\n").ShouldBe(3703);
        GpuMemoryProbeBase.Parse("[N/A]").ShouldBeNull();
        GpuMemoryProbeBase.Parse(null).ShouldBeNull();

        var runner = new FakeProcessRunner { OutputLines = ["4321"] };
        var gpu = Substitute.For<IGpuCapabilityProbe>();
        gpu.HasNvidiaDriver.Returns(true);
        (await new NvidiaSmiGpuMemoryProbe(runner, gpu).GetFreeVramMiBAsync(TestContext.Current.CancellationToken)).ShouldBe(4321);
        runner.LastSpec!.FileName.ShouldBe("nvidia-smi");

        var none = Substitute.For<IGpuCapabilityProbe>();
        var unused = Substitute.For<IProcessRunner>();
        (await new NvidiaSmiGpuMemoryProbe(unused, none).GetFreeVramMiBAsync(TestContext.Current.CancellationToken)).ShouldBeNull();
        await unused.DidNotReceiveWithAnyArgs().RunAsync(default!, default, TestContext.Current.CancellationToken);
    }

    /// <summary>OOM işaretleri tanınır.</summary>
    [Fact]
    public void Detects_out_of_memory_output()
    {
        StableDiffusionCppImageEngine.IsOutOfMemory("[W] execution engine cannot make enough memory available on CUDA0").ShouldBeTrue();
        StableDiffusionCppImageEngine.IsOutOfMemory("CUDA error: out of memory").ShouldBeTrue();
        StableDiffusionCppImageEngine.IsOutOfMemory("[E] model file not found").ShouldBeFalse();
    }

    /// <summary>Motor: 4,3 GB boşta azaltılmış kiple başlar; OOM olunca bir kez en agresif kiple yeniden dener ve başarır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Engine_retries_once_with_aggressive_mode_on_oom()
    {
        var paths = PreparePaths();
        var runner = new ScriptedProcessRunner(
            (1, ["[W] execution engine cannot make enough memory available on CUDA0", "[E] generate failed"]),
            (0, ["[I] generate_image completed"]));
        var probe = Substitute.For<IGpuMemoryProbe>();
        probe.GetFreeVramMiBAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<int?>(4300), Task.FromResult<int?>(4100));
        var engine = EngineFactory.CreateSdCpp(paths, runner, catalog: TinyCatalog(), gpuMemory: probe);

        var result = await engine.GenerateAsync(Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        runner.Specs.Count.ShouldBe(2);
        runner.Specs[0].Arguments.ShouldContain("te=cpu");
        runner.Specs[1].Arguments.ShouldContain("te=cpu,vae=cpu");
    }

    /// <summary>OOM olmayan hata yeniden denenmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Engine_does_not_retry_non_memory_failures()
    {
        var paths = PreparePaths();
        var runner = new ScriptedProcessRunner((1, ["[E] invalid model file"]));
        var engine = EngineFactory.CreateSdCpp(paths, runner, catalog: TinyCatalog());

        var result = await engine.GenerateAsync(Request(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        runner.Specs.Count.ShouldBe(1);
    }

    private FakeAgentPaths PreparePaths()
    {
        var paths = new FakeAgentPaths(_temp.Path);
        var sdDir = Directory.CreateDirectory(Path.Combine(paths.ToolsDir, "sd-cpp")).FullName;
        File.WriteAllText(Path.Combine(sdDir, "sd-cli.exe"), "fake");
        var root = Path.Combine(paths.ModelsDir, "image");
        Directory.CreateDirectory(Path.Combine(root, "diffusion"));
        File.WriteAllBytes(Path.Combine(root, "diffusion", "d.gguf"), [1, 2, 3]);
        return paths;
    }

    private static ImageRequest Request() => new("z-image-turbo", "kedi", null, 512, 512, 1, null, "a.png");

    private static IImageModelCatalog TinyCatalog()
    {
        var model = new ImageModelDefinition("z-image-turbo", "Test Z", "z-image", "Apache-2.0", 4096, 8, 1.0, null, null, null, [],
        [
            new ImageModelFile(ImageModelFileRole.DiffusionModel, "test/tiny", "d.gguf", "diffusion/d.gguf", 3, null),
        ]);
        var catalog = Substitute.For<IImageModelCatalog>();
        catalog.Models.Returns([model]);
        catalog.Find(Arg.Any<string>()).Returns(ci => string.Equals((string)ci[0], model.Key, StringComparison.OrdinalIgnoreCase) ? model : null);
        return catalog;
    }
}
