using System.Globalization;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Concrete;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.StableDiffusionCpp;

/// <summary>sd-cli argüman üretimi testleri.</summary>
public sealed class SdCppArgumentBuilderTests
{
    private readonly EmbeddedImageModelCatalog _catalog = new();
    private readonly SdCppArgumentBuilder _builder = new();

    /// <summary>Z-Image: difüzyon + LLM + VAE, cfg 1, 8 adım, düşük VRAM bayrakları ve çıktı.</summary>
    [Fact]
    public void Z_image_turbo_arguments_match_sdcpp_docs()
    {
        var args = Build("z-image-turbo", new ImageRequest("z-image-turbo", " bir kedi ", null, 512, 768, 42, null, "C:/out/a.png"), lowVram: true);

        ValueOf(args, "--diffusion-model").ShouldBe("M/diffusion/z_image_turbo-Q3_K.gguf");
        ValueOf(args, "--llm").ShouldBe("M/text_encoders/Qwen3-4B-Instruct-2507-Q4_K_M.gguf");
        ValueOf(args, "--vae").ShouldBe("M/vae/ae.safetensors");
        ValueOf(args, "-p").ShouldBe("bir kedi");
        ValueOf(args, "-W").ShouldBe("512");
        ValueOf(args, "-H").ShouldBe("768");
        ValueOf(args, "-s").ShouldBe("42");
        ValueOf(args, "--steps").ShouldBe("8");
        ValueOf(args, "--cfg-scale").ShouldBe("1.0");
        ValueOf(args, "--flash-attn").ShouldBe("auto,diffusion=on");
        args.ShouldContain("--offload-to-cpu");
        args.ShouldContain("--vae-tiling");
        args.ShouldNotContain("-n");
        args[^2].ShouldBe("-o");
        args[^1].ShouldBe("C:/out/a.png");
    }

    /// <summary>Türkçe kültürde ondalıklar virgülle yazılmaz.</summary>
    [Fact]
    public void Decimals_use_invariant_culture_under_turkish_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var sd3 = Build("sd35-medium", Request("sd35-medium"), lowVram: false);
            ValueOf(sd3, "--cfg-scale").ShouldBe("4.5");
            ValueOf(sd3, "--slg-scale").ShouldBe("2.5");

            var qwen = Build("qwen-image", Request("qwen-image"), lowVram: false);
            ValueOf(qwen, "--flow-shift").ShouldBe("3.0");
            ValueOf(qwen, "--cfg-scale").ShouldBe("2.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    /// <summary>SD3.5: tam model <c>-m</c> + clip_l/clip_g/t5xxl; düşük VRAM kapalıysa bayrak yok.</summary>
    [Fact]
    public void Sd35_uses_full_model_and_three_text_encoders()
    {
        var args = Build("sd35-medium", Request("sd35-medium"), lowVram: false);

        ValueOf(args, "-m").ShouldEndWith("sd3.5_medium-Q4_0.gguf");
        ValueOf(args, "--clip_l").ShouldEndWith("clip_l-Q8_0.gguf");
        ValueOf(args, "--clip_g").ShouldEndWith("clip_g-Q8_0.gguf");
        ValueOf(args, "--t5xxl").ShouldEndWith("t5xxl-Q4_0.gguf");
        args.ShouldNotContain("--offload-to-cpu");
        args.ShouldNotContain("--diffusion-model");
    }

    /// <summary>FLUX.2 klein: euler, 4 adım; istek adımı ve negatif istem uygulanır.</summary>
    [Fact]
    public void Flux2_klein_uses_euler_and_respects_overrides()
    {
        var args = Build("flux2-klein-4b", new ImageRequest("flux2-klein-4b", "dağ", "bulanık", 576, 1024, -1, 6, "x.png"), lowVram: true);

        ValueOf(args, "--sampling-method").ShouldBe("euler");
        ValueOf(args, "--steps").ShouldBe("6");
        ValueOf(args, "-n").ShouldBe("bulanık");
        ValueOf(args, "-s").ShouldBe("-1");
        ValueOf(args, "--vae").ShouldEndWith("flux2-vae.safetensors");
    }

    /// <summary>SDXL-Lightning: sgm_uniform zamanlayıcı, 4 adım.</summary>
    [Fact]
    public void Sdxl_lightning_uses_sgm_uniform()
    {
        var args = Build("sdxl-lightning", Request("sdxl-lightning"), lowVram: true);

        ValueOf(args, "--scheduler").ShouldBe("sgm_uniform");
        ValueOf(args, "--steps").ShouldBe("4");
        ValueOf(args, "-m").ShouldEndWith(".gguf");
    }

    private static ImageRequest Request(string key) => new(key, "test", null, 512, 512, 7, null, "o.png");

    private static string ValueOf(IReadOnlyList<string> args, string flag)
    {
        var index = args.ToList().IndexOf(flag);
        index.ShouldBeGreaterThanOrEqualTo(0, $"{flag} yok");
        return args[index + 1];
    }

    private IReadOnlyList<string> Build(string key, ImageRequest request, bool lowVram)
    {
        var model = _catalog.Find(key).ShouldNotBeNull();
        return _builder.Build(model, (ImageModelFile f) => "M/" + f.LocalPath, request, lowVram);
    }
}
