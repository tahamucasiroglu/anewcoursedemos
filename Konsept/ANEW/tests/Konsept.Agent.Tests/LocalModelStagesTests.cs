using System.Globalization;
using System.Text.Json.Nodes;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Yerel model aşamaları (spec 0028 AC-2, AC-6, AC-7, AC-8): gerçek gömülü bileşen listesi (Z-Image-Turbo, Supertonic 3 şablonları), sahte süreç
/// çalıştırıcı (sd-cli / Python / FFmpeg gibi davranır, çağrıları kaydeder), geçici kurulum yeri. Gerçek model çalışmaz.
/// </summary>
public sealed class LocalModelStagesTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-stages-");
    private readonly FakeProcesses _processes = new();
    // Gerçek liste; model dosyalarının boyutu testte küçültülür (dosyalar yerinde/boyut doğru denetimi; review r1 M-1).
    private readonly IReadOnlyList<SetupComponent> _catalog =
        [.. ComponentCatalog.Load(null).Select(c => c.Files is null ? c : c with { Files = [.. c.Files.Select(f => f with { Bytes = 3 })] })];
    private readonly ISetupStateStore _store = Substitute.For<ISetupStateStore>();
    private readonly Dictionary<string, InstalledComponent> _installed = [];

    /// <summary>Initializes a new instance of the <see cref="LocalModelStagesTests"/> class.</summary>
    public LocalModelStagesTests() => _store.Load().Returns(_ => new SetupState(new Dictionary<string, InstalledComponent>(_installed), null));

    private string Root => _root.FullName;

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>AC-6: sahne başına bir sd-cli süreci, şablondan (model/kodlayıcı/VAE yolları, tarif, boyut, tohum); image.png yazılır; aynı tarifle yeniden üretilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Images_RunsTemplatePerScene_KeepsUnchanged()
    {
        Install("ffmpeg", "sdcpp", "qwen3-4b", "z-image-turbo");
        var runner = Runner(new GpuInfo("RTX", GpuInfo.Nvidia, 8L << 30));
        var command = Command(AgentStageCommand.Images, image: "local/z-image-turbo", scenes: [Scene(0, 5, "Boğaz", "Boğaz köprüsü gün batımı"), Scene(1, 4, "Kedi", null)]);

        var reply = await runner.RunAsync(command);

        (reply.Ok, reply.Device).ShouldBe((true, "gpu"));
        _processes.Calls.Count.ShouldBe(2);
        var first = _processes.Calls[0];
        first.Executable.ShouldBe(Path.Combine(Root, "components", "sdcpp", "sd-cli.exe"));
        Arg(first, "--diffusion-model").ShouldBe(Path.Combine(Root, "components", "z-image-turbo") + "/z_image_turbo-Q4_K.gguf");
        Arg(first, "--llm").ShouldBe(Path.Combine(Root, "components", "qwen3-4b") + "/Qwen3-4B-Q4_K_M.gguf");
        (Arg(first, "-p"), Arg(first, "-W"), Arg(first, "-H"), Arg(first, "--seed"), Arg(first, "--steps")).ShouldBe(("Boğaz köprüsü gün batımı", "1024", "576", "42", "8"));
        Arg(_processes.Calls[1], "-p").ShouldBe("Kedi"); // Tarif yoksa seslendirme metni.
        first.Environment["HF_HUB_OFFLINE"].ShouldBe("1");
        File.Exists(Path.Combine(SceneDir(command, 0), "image.png")).ShouldBeTrue();

        await runner.RunAsync(command);
        _processes.Calls.Count.ShouldBe(2);

        await runner.RunAsync(command with { Scenes = [Scene(0, 5, "Boğaz", "Kız Kulesi"), command.Scenes[1]] });
        _processes.Calls.Count.ShouldBe(3);
    }

    /// <summary>AC-6: ekran kartı/bellek hatası → gpu_unavailable (model adı), diğer hata → failed; kurulu değilse tool_missing; Durdur → canceled.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Images_Failures()
    {
        var runner = Runner(new GpuInfo("RTX", GpuInfo.Nvidia, 8L << 30));
        var command = Command(AgentStageCommand.Images, image: "local/z-image-turbo", scenes: [Scene(0, 5, "a", "b")]);
        (await runner.RunAsync(command)).ShouldBe(new AgentStageReply(false, AgentStageReply.ToolMissing, "z-image-turbo"));

        Install("ffmpeg", "sdcpp", "qwen3-4b", "z-image-turbo");
        _processes.Fail = new ProcessResult(1, "ggml_vulkan: vk::Device::allocateMemory: ErrorOutOfDeviceMemory");
        (await runner.RunAsync(command)).ShouldBe(new AgentStageReply(false, AgentStageReply.GpuUnavailable, "z-image-turbo"));

        _processes.Fail = new ProcessResult(3, "error: bozuk dosya");
        (await runner.RunAsync(command)).ShouldBe(new AgentStageReply(false, AgentStageReply.Failed));
        File.Exists(Path.Combine(SceneDir(command, 0), "image.png")).ShouldBeFalse();

        _processes.Fail = null;
        _processes.BeforeRun = () => runner.Cancel(command.JobId); // Durdur süreç çalışırken gelir.
        (await runner.RunAsync(command)).Error.ShouldBe(AgentStageReply.Canceled);
    }

    /// <summary>
    /// AC-7: tüm sahneler tek Python sürecinde (şablon: betik, site, arka uç, dil, girdi/çıktı); sonra her sahne FFmpeg ile sahne süresine oturtulur
    /// (kısa → doldur, uzun → hızlandır), konuşma süresi yazılır; metinsiz sahne sessiz. Aynı girdiyle yeniden çalışmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Voice_SingleProcess_FitsScenes()
    {
        Install("ffmpeg", "python", "uv", "speech-onnx", "supertonic-3");
        var runner = Runner(null);
        _processes.SpeechSeconds = [4.0, 6.0];
        var command = Command(AgentStageCommand.Voice, audio: "local/supertonic-3", scenes: [Scene(0, 5, "Merhaba dünya.", null), Scene(1, 4, "Uzun bir cümle.", null), Scene(2, 3, " ", null)]);

        var reply = await runner.RunAsync(command);

        (reply.Ok, reply.Device).ShouldBe((true, "cpu"));
        var python = _processes.Calls.Where(c => c.Executable.EndsWith("python.exe", StringComparison.Ordinal)).ShouldHaveSingleItem();
        python.Executable.ShouldBe(Path.Combine(Root, "components", "python", "python.exe"));
        python.Arguments.Take(4).ShouldBe(["-I", "-B", "-X", "utf8"]);
        python.Arguments[4].ShouldBe(Path.Combine(Root, "runners", "speech.py"));
        File.Exists(python.Arguments[4]).ShouldBeTrue();
        (Arg(python, "--site"), Arg(python, "--backend"), Arg(python, "--lang"), Arg(python, "--model-dir"))
            .ShouldBe((Path.Combine(Root, "components", "speech-onnx"), "supertonic", "tr", Path.Combine(Root, "components", "supertonic-3")));
        _processes.Inputs.ShouldHaveSingleItem().ShouldBe(["Merhaba dünya.", "Uzun bir cümle."]);
        var fits = _processes.Calls.Where(c => c.Executable.EndsWith("ffmpeg.exe", StringComparison.Ordinal)).ToList();
        fits.Count.ShouldBe(3);
        Arg(fits[0], "-af").ShouldBe("apad");
        Arg(fits[1], "-af").ShouldBe("atempo=1.5,apad");
        fits[2].Arguments.ShouldContain("anullsrc=r=48000:cl=stereo");
        (Arg(fits[0], "-t"), Arg(fits[1], "-t"), Arg(fits[2], "-t")).ShouldBe(("5", "4", "3"));
        Speech(command, 0).ShouldBe("4");
        Speech(command, 1).ShouldBe("4");
        Speech(command, 2).ShouldBe("0");
        File.Exists(Path.Combine(SceneDir(command, 0), VoiceFit.RawFile)).ShouldBeFalse();

        await runner.RunAsync(command);
        _processes.Calls.Count.ShouldBe(4);
    }

    /// <summary>AC-7: model sahne sesi üretmezse aşama failed; Python GPU belleği hatası → gpu_unavailable.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Voice_Failures()
    {
        Install("ffmpeg", "python", "uv", "speech-onnx", "supertonic-3");
        var runner = Runner(null);
        var command = Command(AgentStageCommand.Voice, audio: "local/supertonic-3", scenes: [Scene(0, 5, "Merhaba.", null)]);
        _processes.SpeechSeconds = [0];
        (await runner.RunAsync(command)).Error.ShouldBe(AgentStageReply.Failed);

        _processes.Fail = new ProcessResult(2, "speech runner error: OutOfMemoryError: CUDA out of memory.");
        (await runner.RunAsync(command)).ShouldBe(new AgentStageReply(false, AgentStageReply.GpuUnavailable, "supertonic-3"));
    }

    /// <summary>AC-2, AC-5: araç denetimi eksik modelin kurulu olmayan bağımlılıklarını da döndürür; kart yoksa görsel modeli desteklenmez.</summary>
    [Fact]
    public void CheckTools_ClosureAndUnsupported()
    {
        Install("node", "ffmpeg", "chromium", "hyperframes", "python");
        var reply = Runner(null).CheckTools(new AgentToolCheckRequest(["node", "ffmpeg", "chromium", "hyperframes", "supertonic-3", "z-image-turbo"]));

        reply.Missing.ShouldBe(["uv", "speech-onnx", "z-image-turbo", "supertonic-3"]);
        reply.Unsupported.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(new AgentUnsupportedTool("z-image-turbo", AgentUnsupportedTool.GpuRequired, 4096));
    }

    /// <summary>
    /// Review r1 M-1: sürümü listedekinden farklı (yeni ajan sürümü) ya da dosyası eksik/boyutu bozuk model kurulu sayılmaz: araç denetiminde eksik
    /// (kurulabilir, 0027 onarımı önerir), aşama çalıştırılmaz (tool_missing).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task StaleOrBrokenModel_IsMissing()
    {
        Install("ffmpeg", "python", "uv", "speech-onnx", "supertonic-3");
        var runner = Runner(null);
        var request = new AgentToolCheckRequest(["ffmpeg", "supertonic-3"]);
        runner.CheckTools(request).Missing.ShouldBeEmpty();

        File.WriteAllText(Path.Combine(Root, "components", "supertonic-3", "onnx", "vocoder.onnx"), "bozuk dosya");
        runner.CheckTools(request).Missing.ShouldBe(["supertonic-3"]);

        Install("supertonic-3");
        _installed["supertonic-3"] = new InstalledComponent("eski-sürüm", "x");
        runner.CheckTools(request).Missing.ShouldBe(["supertonic-3"]);
        var command = Command(AgentStageCommand.Voice, audio: "local/supertonic-3", scenes: [Scene(0, 5, "Merhaba.", null)]);
        (await runner.RunAsync(command)).ShouldBe(new AgentStageReply(false, AgentStageReply.ToolMissing, "supertonic-3"));
        _processes.Calls.ShouldBeEmpty();
    }

    /// <summary>Review r1 L-2: CPU'da çalışan modelin bellek hatası "Ekran kartı kullanılamadı" değil, failed olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Voice_CpuMemoryError_IsNotGpu()
    {
        Install("ffmpeg", "python", "uv", "speech-onnx", "supertonic-3");
        _processes.Fail = new ProcessResult(2, "speech runner error: RuntimeException: onnxruntime Failed to allocate memory for requested buffer");
        var command = Command(AgentStageCommand.Voice, audio: "local/supertonic-3", scenes: [Scene(0, 5, "Merhaba.", null)]);

        (await Runner(null).RunAsync(command)).ShouldBe(new AgentStageReply(false, AgentStageReply.Failed));
    }

    /// <summary>AC-8: birleştirmede altyazı sahnenin konuşma süresine göre zamanlanır.</summary>
    [Fact]
    public void Subtitles_SpeechSeconds()
    {
        var ass = MergeTemplate.Subtitles([(5d, 2d, "aa bb"), (4d, 4d, "cc")], 1920, 1080);

        ass.ShouldContain("Dialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,{\\c&H0000B7F5&}aa{\\c&H00FFFFFF&} bb");
        ass.ShouldContain("Dialogue: 0,0:00:01.00,0:00:02.00,");
        ass.ShouldContain("Dialogue: 0,0:00:05.00,0:00:09.00,");
        MergeTemplate.Subtitles([(5d, "aa bb")], 1920, 1080).ShouldBe(MergeTemplate.Subtitles([(5d, 5d, "aa bb")], 1920, 1080));
    }

    private static AgentStageScene Scene(int index, int seconds, string narration, string? prompt) => new(index, seconds, narration, prompt, null, null);

    private static AgentStageCommand Command(string stage, string? image = null, string? audio = null, IReadOnlyList<AgentStageScene>? scenes = null) => new()
    {
        JobId = Guid.NewGuid(),
        WorkKey = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        Stage = stage,
        Engine = "local/hyperframes",
        ImageModel = image,
        AudioModel = audio,
        Language = "tr",
        Format = "landscape",
        Scenes = scenes ?? [],
    };

    private static string Arg(ProcessSpec spec, string name) => spec.Arguments[spec.Arguments.ToList().IndexOf(name) + 1];

    private ProcessStageRunner Runner(GpuInfo? gpu) =>
        new(_store, Root, () => Root, Substitute.For<ISceneRenderer>(), Serilog.Core.Logger.None, new LocalModelStages(_catalog, _store, Root, _processes, Serilog.Core.Logger.None), _catalog, gpu);

    private void Install(params string[] ids)
    {
        foreach (var id in ids)
        {
            var component = _catalog.Single(c => c.Id == id);
            _installed[id] = new InstalledComponent(component.Version, "x");
            var exe = Path.Combine(Root, "components", id, component.Executable);
            Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
            File.WriteAllText(exe, id);
            foreach (var file in component.Files ?? [])
            {
                var path = Path.Combine(Root, "components", id, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "abc");
            }
        }
    }

    private string SceneDir(AgentStageCommand command, int index) =>
        Path.Combine(Root, "jobs", command.WorkKey.ToString("N", CultureInfo.InvariantCulture), "scene-" + index.ToString(CultureInfo.InvariantCulture));

    private string Speech(AgentStageCommand command, int index) => File.ReadAllText(Path.Combine(SceneDir(command, index), VoiceFit.SpeechFile));

    /// <summary>sd-cli / Python betiği / FFmpeg gibi davranan sahte süreç çalıştırıcı.</summary>
    private sealed class FakeProcesses : IProcessRunner
    {
        public List<ProcessSpec> Calls { get; } = [];

        public List<List<string>> Inputs { get; } = [];

        public ProcessResult? Fail { get; set; }

        public Action? BeforeRun { get; set; }

        public double[] SpeechSeconds { get; set; } = [];

        public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken)
        {
            Calls.Add(spec);
            BeforeRun?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            if (Fail is { } fail)
            {
                return fail;
            }

            var args = spec.Arguments.ToList();
            if (spec.Executable.EndsWith("sd-cli.exe", StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(args[args.IndexOf("-o") + 1], "png", cancellationToken);
            }
            else if (spec.Executable.EndsWith("python.exe", StringComparison.Ordinal))
            {
                var items = JsonNode.Parse(await File.ReadAllTextAsync(args[args.IndexOf("--input") + 1], cancellationToken))!["items"]!.AsArray();
                Inputs.Add([.. items.Select(i => i!["text"]!.GetValue<string>())]);
                var results = new JsonArray();
                for (var i = 0; i < items.Count; i++)
                {
                    var output = items[i]!["output"]!.GetValue<string>();
                    await File.WriteAllTextAsync(output, "wav", cancellationToken);
                    results.Add(new JsonObject { ["output"] = output, ["seconds"] = SpeechSeconds[i] });
                }

                await File.WriteAllTextAsync(args[args.IndexOf("--output") + 1], new JsonObject { ["device"] = "cpu", ["items"] = results }.ToJsonString(), cancellationToken);
            }
            else
            {
                await File.WriteAllTextAsync(args[^1], "wav", cancellationToken);
            }

            return new ProcessResult(0, "ok");
        }
    }
}
