using Shouldly;
using TahaKonsept.Agent.Core.Audio;
using TahaKonsept.Agent.Core.Capabilities.Base;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.Agent.Core.Prerequisites.Concrete;
using TahaKonsept.Agent.Core.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Parsing;

/// <summary>Çözümleyiciler: nvidia-smi, SHA256 dosyaları, Node sürüm dizini, Chrome for Testing JSON, WAV birleştirme, dosya adı temizleme.</summary>
public sealed class ParsingTests
{
    /// <summary>nvidia-smi CSV çıktısı GPU adı/VRAM/sürücüye çevrilir; bozuk satır atlanır.</summary>
    [Fact]
    public void NvidiaSmi_ParsesGpus()
    {
        var gpus = CapabilityDetectorBase.ParseNvidiaSmi("NVIDIA GeForce RTX 3060 Laptop GPU, 6144, 566.36\r\nbozuk satır\r\n");

        gpus.Count.ShouldBe(1);
        gpus[0].Name.ShouldBe("NVIDIA GeForce RTX 3060 Laptop GPU");
        gpus[0].VramMb.ShouldBe(6144);
        gpus[0].DriverVersion.ShouldBe("566.36");
        gpus[0].CudaAvailable.ShouldBeTrue();
        CapabilityDetectorBase.ParseNvidiaSmi(string.Empty).ShouldBeEmpty();
    }

    /// <summary>SHA256 metinleri: dosya adına göre ya da ilk hash.</summary>
    [Fact]
    public void Sha256_Parses()
    {
        var a = new string('a', 64);
        var b = new string('B', 64);
        var sums = a + "  node-v22.1.0-x64.msi\n" + b + "  node-v22.1.0-win-x64.zip\n";

        DownloadablePrerequisiteBase.ParseSha256(sums, "node-v22.1.0-win-x64.zip").ShouldBe(b.ToLowerInvariant());
        DownloadablePrerequisiteBase.ParseSha256(b + " *uv-x86_64-pc-windows-msvc.zip").ShouldBe(b.ToLowerInvariant());
        DownloadablePrerequisiteBase.ParseSha256("geçersiz").ShouldBeNull();
    }

    /// <summary>Node dizininden en yeni v22 seçilir.</summary>
    [Fact]
    public void Node_PicksLatest22()
    {
        const string json = """[{"version":"v24.1.0","lts":false},{"version":"v22.21.1","lts":"Jod"},{"version":"v22.21.0","lts":"Jod"}]""";

        NodePrerequisite.PickLatestVersion(json).ShouldBe("v22.21.1");
        NodePrerequisite.PickLatestVersion("{}").ShouldBeNull();
    }

    /// <summary>Chrome for Testing JSON'undan Stable win64 headless shell adresi çıkarılır.</summary>
    [Fact]
    public void Chrome_ParsesStableWin64()
    {
        const string json = """
            {"channels":{"Stable":{"version":"142.0.7444.0","downloads":{"chrome-headless-shell":[
              {"platform":"linux64","url":"https://x/linux.zip"},
              {"platform":"win64","url":"https://x/win64/chrome-headless-shell-win64.zip"}]}}}}
            """;

        var parsed = ChromeHeadlessShellPrerequisite.ParseStableWin64(json);

        parsed.ShouldNotBeNull();
        parsed.Value.Version.ShouldBe("142.0.7444.0");
        parsed.Value.Url.ToString().ShouldEndWith("chrome-headless-shell-win64.zip");
    }

    /// <summary>Aynı biçimli WAV'ler birleştirilir; süre toplanır.</summary>
    [Fact]
    public void Wav_ConcatenatesAndMeasures()
    {
        using var temp = new TempDirectory();
        var a = Path.Combine(temp.Path, "a.wav");
        var b = Path.Combine(temp.Path, "b.wav");
        var output = Path.Combine(temp.Path, "out.wav");
        WavFile.WriteSilence(a, 1.0);
        WavFile.WriteSilence(b, 0.5);
        WavFile.TryReadInfo(a, out var infoA).ShouldBeTrue();
        WavFile.TryReadInfo(b, out var infoB).ShouldBeTrue();

        var duration = WavFile.Concatenate([infoA!, infoB!], [a, b], output);

        duration.ShouldBe(1.5, 0.001);
        WavFile.TryReadInfo(output, out var combined).ShouldBeTrue();
        combined!.DurationSec.ShouldBe(1.5, 0.001);
        combined.SampleRate.ShouldBe(WavFile.DefaultSampleRate);
        WavFile.TryReadInfo(Path.Combine(temp.Path, "yok.wav"), out _).ShouldBeFalse();
    }

    /// <summary>Dosya adı temizleyici yol ayraçlarını ve geçersiz karakterleri atar.</summary>
    [Fact]
    public void FileName_Sanitizes()
    {
        FileNameSanitizer.Sanitize("../../etc/passwd").ShouldBe("passwd");
        FileNameSanitizer.Sanitize("a<b>c.png").ShouldBe("a_b_c.png");
        FileNameSanitizer.Sanitize("  ", "yedek").ShouldBe("yedek");
        FileNameSanitizer.Sanitize("klasör\\sahne-1.png").ShouldBe("sahne-1.png");
    }
}
