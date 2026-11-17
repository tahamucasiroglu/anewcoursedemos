using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Base;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Bileşen kuralları (spec 0028 AC-2, AC-5): varyant seçimi karta göre, bağımlılık kapanışı, araç denetimi (eksik bağımlılıklar liste
/// sırasında, kurulu olanlar yok, ekran kartı gereksinimi), dosya listesi ve güvenli yol; şablon, ses oturtma ve yerel model kuralları.
/// </summary>
public sealed class ComponentRulesTests
{
    private static readonly GpuInfo Nvidia6 = new("RTX 3060", GpuInfo.Nvidia, 6L * 1024 * 1024 * 1024);
    private static readonly GpuInfo Nvidia12 = new("RTX 4070", GpuInfo.Nvidia, 12L * 1024 * 1024 * 1024);
    private static readonly GpuInfo Amd16 = new("Radeon RX 7800", "amd", 16L * 1024 * 1024 * 1024);

    private static readonly SetupComponent Torch = Component("torch") with
    {
        Variants =
        [
            new ComponentVariant("cuda", GpuInfo.Nvidia, 8192, new string('c', 64), 2_700, 5_200),
            new ComponentVariant("cpu", null, 0, new string('d', 64), 400, 1_700),
        ],
    };

    /// <summary>AC-5: NVIDIA + yeterli bellek → CUDA varyantı (hash, boyut, sürüm); az bellek, başka üretici ya da kart yok → CPU.</summary>
    [Fact]
    public void Variant_ByGpu()
    {
        var cuda = ComponentRules.Resolve([Torch], Nvidia12).Single();
        (cuda.Variant, cuda.Version, cuda.Sha256, cuda.DownloadBytes, cuda.InstalledBytes).ShouldBe(("cuda", "1.0.0+cuda", new string('c', 64), 2_700L, 5_200L));
        foreach (var gpu in new[] { Nvidia6, Amd16, null })
        {
            ComponentRules.Resolve([Torch], gpu).Single().Variant.ShouldBe("cpu");
        }

        ComponentRules.Resolve([Component("plain")], Nvidia12).Single().ShouldBe(Component("plain"));
    }

    /// <summary>AC-2: eksik modelin kurulu olmayan bağımlılıkları liste sırasında eksiktir; kurulu olanlar ve listede olmayan kurulu araç dönmez.</summary>
    [Fact]
    public void Missing_ClosureInCatalogOrder()
    {
        var catalog = Catalog();

        var result = ComponentRules.Check(["node", "supertonic", "unknown", "known-installed"], catalog, new HashSet<string> { "node", "uv", "known-installed" }, null);

        result.Missing.ShouldBe(["python", "speech", "supertonic", "unknown"]);
        result.Unsupported.ShouldBeEmpty();
        ComponentRules.Closure("supertonic", catalog).ShouldBe(["python", "uv", "speech", "supertonic"]);
    }

    /// <summary>AC-5: ekran kartı gerektiren model uygun kart yokken (kurulu olsa da) eksik + desteklenmez; bağımlılıkları istenmez. Kart yeterliyse normal.</summary>
    [Fact]
    public void Missing_GpuRequired_Unsupported()
    {
        var catalog = Catalog();
        var installed = new HashSet<string> { "image", "sd" };

        var none = ComponentRules.Check(["image"], catalog, installed, null);
        none.Missing.ShouldBe(["image"]);
        none.Unsupported.Select(c => c.Id).ShouldBe(["image"]);
        ComponentRules.Check(["image"], catalog, installed, new GpuInfo("iGPU", "intel", 512L * 1024 * 1024)).Unsupported.ShouldHaveSingleItem();

        var ok = ComponentRules.Check(["image"], catalog, installed, Amd16);
        ok.Missing.ShouldBeEmpty();
        ok.Unsupported.ShouldBeEmpty();
        ComponentRules.Check(["image"], catalog, new HashSet<string>(), Nvidia6).Missing.ShouldBe(["sd", "image"]);
    }

    /// <summary>AC-3: dosya listesi metni ve hash'i; güvenli göreli yol.</summary>
    [Fact]
    public void FilesManifest_AndSafePath()
    {
        var files = new[] { new ComponentFile("onnx/a.onnx", new Uri("https://hf.invalid/a"), new string('a', 64), 12) };
        ComponentRules.FilesManifest(files).ShouldBe(new string('a', 64) + " 12 onnx/a.onnx\n");
        ComponentRules.FilesManifestSha256(files).Length.ShouldBe(64);
        ComponentRules.IsSafeRelativePath("onnx/a.onnx").ShouldBeTrue();
        foreach (var bad in new[] { "../a", "a/../../b", "C:/x", "/abs", "a//b", "./a", string.Empty, "a:stream" })
        {
            ComponentRules.IsSafeRelativePath(bad).ShouldBeFalse(bad);
        }
    }

    /// <summary>AC-6: şablon tek geçişte çözülür (değerdeki süslü parantez yeniden yorumlanmaz), gömülü yer tutucu, bilinmeyen yer tutucu hata.</summary>
    [Fact]
    public void Template_Expand()
    {
        var values = new Dictionary<string, string> { ["self"] = @"C:\m", ["qwen3-4b"] = @"C:\q", ["prompt"] = "a {self} cat; rm -rf" };

        ModelRunTemplate.Expand(["--model", "{self}/z.gguf", "--llm", "{qwen3-4b}/q.gguf", "-p", "{prompt}"], values)
            .ShouldBe(["--model", @"C:\m/z.gguf", "--llm", @"C:\q/q.gguf", "-p", "a {self} cat; rm -rf"]);
        Should.Throw<InvalidOperationException>(() => ModelRunTemplate.Expand(["{nope}"], values));
        ModelRunTemplate.Names(["{self}/a", "{script:speech.py}", "x", "{self}"]).ShouldBe(["self", "script:speech.py"]);
    }

    /// <summary>AC-7: konuşma sahneden uzunsa en çok 1,6 kat hızlanır ve sahneye sığar; kısaysa hız 1; FFmpeg argümanları doldurur ve keser.</summary>
    [Fact]
    public void VoiceFit_PlanAndArguments()
    {
        VoiceFit.Plan(4, 5).ShouldBe((1d, 4d));
        var (tempo, speech) = VoiceFit.Plan(6, 4);
        tempo.ShouldBe(1.5, 1e-9);
        speech.ShouldBe(4, 1e-9);
        VoiceFit.Plan(10, 4).ShouldBe((1.6, 4d));
        VoiceFit.Plan(0, 4).ShouldBe((1d, 0d));

        VoiceFit.Arguments("r.wav", "v.wav", 1, 5).ShouldBe(["-y", "-hide_banner", "-loglevel", "error", "-i", "r.wav", "-af", "apad", "-t", "5", "-ar", "48000", "-ac", "2", "-c:a", "pcm_s16le", "v.wav"]);
        VoiceFit.Arguments("r.wav", "v.wav", 1.5, 4).ShouldContain("atempo=1.5,apad");
        VoiceFit.SilenceArguments("v.wav", 3).ShouldContain("anullsrc=r=48000:cl=stereo");
    }

    /// <summary>AC-6: görsel boyutu biçime göre; işaret girdiye bağlı; ekran kartı/bellek hatası tanınır, olağan hata tanınmaz.</summary>
    [Fact]
    public void LocalModel_SizeMarkersGpuFailure()
    {
        LocalModelRules.ImageSize("landscape").ShouldBe((1024, 576));
        LocalModelRules.ImageSize("portrait").ShouldBe((576, 1024));
        LocalModelRules.ImageMarker("z", "kedi", 1024, 576).ShouldNotBe(LocalModelRules.ImageMarker("z", "köpek", 1024, 576));
        LocalModelRules.VoiceMarker("s", "tr", 5, "a").ShouldNotBe(LocalModelRules.VoiceMarker("s", "tr", 6, "a"));
        LocalModelRules.IsGpuFailure("ggml_vulkan: vk::Device::allocateMemory: ErrorOutOfDeviceMemory").ShouldBeTrue();
        LocalModelRules.IsGpuFailure("torch.OutOfMemoryError: CUDA out of memory.").ShouldBeTrue();
        LocalModelRules.IsGpuFailure("ggml_vulkan: Found 2 Vulkan devices\nerror: model file not found").ShouldBeFalse();
    }

    /// <summary>AC-5: ekran kartı tespiti en çok belleği olanı seçer, üreticiyi sınıflar; okuma hatası "kart yok".</summary>
    [Fact]
    public void GpuProbe_PicksLargest_ClassifiesVendor()
    {
        new FixedProbe([("Intel(R) UHD Graphics", "Intel Corporation", 0), ("NVIDIA GeForce RTX 3060 Laptop GPU", "NVIDIA", 6442450944), ("DisplayLink", "DisplayLink", 0)]).Detect()
            .ShouldBe(new GpuInfo("NVIDIA GeForce RTX 3060 Laptop GPU", GpuInfo.Nvidia, 6442450944));
        GpuProbeBase.Vendor("AMD Radeon RX 7800 XT").ShouldBe("amd");
        GpuProbeBase.Vendor("Intel(R) Arc(TM) A770").ShouldBe("intel");
        new FixedProbe([]).Detect().ShouldBeNull();
        new FixedProbe(null).Detect().ShouldBeNull();
    }

    private static List<SetupComponent> Catalog() =>
    [
        Component("node"),
        Component("python"),
        Component("uv"),
        Component("speech") with { Requires = ["python", "uv"] },
        Component("sd"),
        Component("supertonic") with { Requires = ["speech"] },
        Component("image") with { Requires = ["sd"], Gpu = new GpuRequirement(4096) },
    ];

    private static SetupComponent Component(string id) =>
        new(id, id, "amaç", "MIT", "1.0.0", new Uri("https://example.invalid/" + id), id + ".zip", new string('a', 64), 1, 1, null, "x.exe", null, null, false, false);

    private sealed class FixedProbe((string, string, long)[]? adapters) : GpuProbeBase
    {
        protected override IEnumerable<(string Name, string Provider, long VramBytes)> ReadAdapters() =>
            adapters ?? throw new UnauthorizedAccessException("okunamadı");
    }
}
