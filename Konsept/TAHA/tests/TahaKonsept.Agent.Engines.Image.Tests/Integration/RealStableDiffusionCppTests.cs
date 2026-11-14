using System.Buffers.Binary;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Gpu.Concrete;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.Gpu.Concrete;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.Integration;

/// <summary>
/// Gerçek sd-cli.exe ve Z-Image-Turbo dosyalarıyla uçtan uca testler. <c>.tools-dev/sd-cpp/sd-cli.exe</c> ve
/// <c>.tools-dev/models/image/...</c> yoksa atlanır (indirme yapmaz).
/// </summary>
public sealed class RealStableDiffusionCppTests
{
    /// <summary>
    /// Asgari boş VRAM. Eski sabit bayraklarla (<c>--offload-to-cpu</c>) 4,3 GB boşta CUDA OOM gözlendi (eşik 5000 idi); motor artık boş VRAM'e
    /// göre azaltılmış kipe geçiyor (metin kodlayıcı CPU'da + <c>--max-vram</c>): 3,65 GB boşta 512x768 ≈24 sn'de üretildi. Altında en agresif
    /// kip bile çok yavaşlar / sığmayabilir.
    /// </summary>
    private const int MinFreeVramMiB = 3500;

    /// <summary>İkili çalışıyor: <c>--help</c> 0 ile döner ve beklenen bayrakları listeler.</summary>
    [Fact]
    public async Task Cli_help_lists_low_vram_flags()
    {
        var cli = FindCli();
        Assert.SkipWhen(cli is null, "sd-cli.exe .tools-dev altında yok");

        var result = await new LocalProcessRunner().RunAsync(
            new TahaKonsept.Agent.Abstractions.Processes.ProcessSpec(cli!, ["--help"], Path.GetDirectoryName(cli), Timeout: TimeSpan.FromSeconds(30)),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        var text = result.StandardOutput + result.StandardError;
        foreach (var flag in new[] { "--diffusion-model", "--llm", "--offload-to-cpu", "--vae-tiling", "--flash-attn", "--cfg-scale" })
        {
            text.ShouldContain(flag);
        }
    }

    /// <summary>Z-Image-Turbo ile gerçek 512x768 görsel üretir (RTX 3060 Laptop 6 GB'ta ≈25–30 sn; boş VRAM'e göre bellek kipi otomatik).</summary>
    [Fact]
    public async Task Generates_real_z_image_turbo_512x768()
    {
        var toolsDev = DevToolsLocator.FindToolsDevDir();
        Assert.SkipWhen(toolsDev is null || FindCli() is null, "sd-cli.exe .tools-dev altında yok");

        var paths = new FakeAgentPaths(toolsDev!, toolsDir: toolsDev, modelsDir: Path.Combine(toolsDev!, "models"));
        var runner = new LocalProcessRunner();
        var engine = EngineFactory.CreateSdCpp(
            paths,
            runner,
            options: new ImageEngineOptions { AutoDownloadModels = false },
            gpuMemory: new NvidiaSmiGpuMemoryProbe(runner, new WindowsGpuCapabilityProbe()));
        Assert.SkipUnless(engine.IsModelInstalled("z-image-turbo"), "Z-Image-Turbo dosyaları .tools-dev/models/image altında yok");
        var freeMiB = GpuMemoryProbe.FreeMiB();
        Assert.SkipWhen(
            freeMiB is < MinFreeVramMiB,
            $"Boş VRAM {freeMiB} MiB < {MinFreeVramMiB} MiB (başka uygulamalar GPU belleğini tutuyor); düşük bellek kipinde bile Z-Image-Turbo sığmaz.");

        var output = Path.Combine(toolsDev!, "out", "engine-test.png");
        var request = new ImageRequest(
            "z-image-turbo",
            "a lighthouse on a rocky coast at sunset, dramatic clouds, cinematic photo, no text",
            null,
            512,
            768,
            2026,
            null,
            output);

        var result = await engine.GenerateAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        var bytes = await File.ReadAllBytesAsync(result.Value.ImagePath, TestContext.Current.CancellationToken);
        bytes.Length.ShouldBeGreaterThan(50_000);
        bytes.AsSpan(0, 4).ToArray().ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)).ShouldBe(512);
        BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)).ShouldBe(768);
    }

    private static string? FindCli()
    {
        var toolsDev = DevToolsLocator.FindToolsDevDir();
        if (toolsDev is null)
        {
            return null;
        }

        var cli = Path.Combine(toolsDev, "sd-cpp", "sd-cli.exe");
        return File.Exists(cli) ? cli : null;
    }
}
