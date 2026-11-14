using System.Net;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.StableDiffusionCpp;

/// <summary>sd.cpp motoru: doğrulama, otomatik model indirme, süreç çağrısı ve hata eşleme (sahte süreç ile).</summary>
public sealed class StableDiffusionCppImageEngineTests : IDisposable
{
    private static readonly byte[] DiffusionBytes = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly byte[] VaeBytes = [9, 9, 9];
    private readonly TempDirectory _temp = new();
    private readonly FakeAgentPaths _paths;

    /// <summary>Sahte sd-cli.exe ile klasörleri hazırlar.</summary>
    public StableDiffusionCppImageEngineTests()
    {
        _paths = new FakeAgentPaths(_temp.Path);
        var sdDir = Directory.CreateDirectory(Path.Combine(_paths.ToolsDir, "sd-cpp")).FullName;
        File.WriteAllText(Path.Combine(sdDir, "sd-cli.exe"), "fake");
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Eksik dosyaları indirir, sd-cli'yi doğru argümanlarla çalıştırır ve çıktıyı döner.</summary>
    [Fact]
    public async Task Downloads_missing_files_then_runs_cli()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/test/tiny/resolve/main/d.gguf" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DiffusionBytes) },
            "/test/tiny/resolve/main/vae/ae.safetensors" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(VaeBytes) },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var runner = new FakeProcessRunner();
        var engine = EngineFactory.CreateSdCpp(_paths, runner, handler, catalog: TinyCatalog());
        engine.IsModelInstalled("z-image-turbo").ShouldBeFalse();
        var output = Path.Combine(_temp.Path, "out", "scene-1.png");

        var result = await engine.GenerateAsync(new ImageRequest("z-image-turbo", "kedi", null, 512, 768, 1, null, output), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.ImagePath.ShouldBe(output);
        engine.IsModelInstalled("z-image-turbo").ShouldBeTrue();
        var spec = runner.LastSpec.ShouldNotBeNull();
        spec.FileName.ShouldEndWith("sd-cli.exe");
        spec.Arguments.ShouldContain(Path.Combine(_paths.ModelsDir, "image", "diffusion", "d.gguf"));
        spec.Arguments.ShouldContain("--offload-to-cpu");
        spec.Timeout.ShouldBe(new ImageEngineOptions().GenerationTimeout);
    }

    /// <summary>Otomatik indirme kapalıysa ve dosya yoksa anlaşılır hata.</summary>
    [Fact]
    public async Task Fails_when_model_missing_and_auto_download_disabled()
    {
        var engine = EngineFactory.CreateSdCpp(_paths, new FakeProcessRunner(), options: new ImageEngineOptions { AutoDownloadModels = false }, catalog: TinyCatalog());

        var result = await engine.GenerateAsync(Request(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("image.model.missing");
    }

    /// <summary>sd-cli hata koduyla biterse son çıktı satırları hata mesajına eklenir.</summary>
    [Fact]
    public async Task Maps_nonzero_exit_to_failure_with_output_tail()
    {
        InstallTinyModel();
        var runner = new FakeProcessRunner { ExitCode = 1, OutputLines = ["[I] loading", "[E] CUDA error: out of memory"] };
        var engine = EngineFactory.CreateSdCpp(_paths, runner, catalog: TinyCatalog());

        var result = await engine.GenerateAsync(Request(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("image.sdcpp.failed");
        result.Error.Message.ShouldContain("out of memory");
    }

    /// <summary>Doğrulama: desteklenmeyen model, 8'in katı olmayan boyut, boş istem, yanlış uzantı.</summary>
    [Theory]
    [InlineData("yok-model", "kedi", 512, 512, "a.png", "image.model.unsupported")]
    [InlineData("z-image-turbo", "kedi", 500, 512, "a.png", "image.size.invalid")]
    [InlineData("z-image-turbo", "  ", 512, 512, "a.png", "image.prompt.empty")]
    [InlineData("z-image-turbo", "kedi", 512, 512, "a.gif", "image.output.invalid")]
    public async Task Validates_request(string model, string prompt, int width, int height, string output, string expectedCode)
    {
        var runner = new FakeProcessRunner();
        var engine = EngineFactory.CreateSdCpp(_paths, runner);

        var result = await engine.GenerateAsync(new ImageRequest(model, prompt, null, width, height, 1, null, output), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expectedCode);
        runner.LastSpec.ShouldBeNull();
    }

    /// <summary>Motor bilgileri: anahtar, yedi model, en az VRAM 4 GB; kurulu değil (model yok).</summary>
    [Fact]
    public void Exposes_engine_metadata()
    {
        var engine = EngineFactory.CreateSdCpp(_paths, new FakeProcessRunner());

        engine.Key.ShouldBe("stable-diffusion-cpp");
        engine.SupportedModels.Count.ShouldBe(7);
        engine.MinVramMb.ShouldBe(4096);
        engine.IsInstalled().ShouldBeFalse();
    }

    /// <summary>EnsureModelAsync ilerleme raporlar ve %100 ile biter.</summary>
    [Fact]
    public async Task Ensure_model_reports_progress()
    {
        var handler = new StubHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(request.RequestUri!.AbsolutePath.EndsWith("d.gguf", StringComparison.Ordinal) ? DiffusionBytes : VaeBytes),
        });
        var engine = EngineFactory.CreateSdCpp(_paths, new FakeProcessRunner(), handler, catalog: TinyCatalog());
        var reports = new List<EngineSetupProgress>();

        var result = await engine.EnsureModelAsync("z-image-turbo", new SyncProgress<EngineSetupProgress>(reports), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        reports.Last().Percent.ShouldBe(100);
    }

    private static ImageRequest Request() => new("z-image-turbo", "kedi", null, 512, 512, 1, null, "a.png");

    private static IImageModelCatalog TinyCatalog()
    {
        var model = new ImageModelDefinition("z-image-turbo", "Test Z", "z-image", "Apache-2.0", 4096, 8, 1.0, null, null, null, [],
        [
            new ImageModelFile(ImageModelFileRole.DiffusionModel, "test/tiny", "d.gguf", "diffusion/d.gguf", DiffusionBytes.Length, null),
            new ImageModelFile(ImageModelFileRole.Vae, "test/tiny", "vae/ae.safetensors", "vae/ae.safetensors", VaeBytes.Length, null),
        ]);
        var catalog = Substitute.For<IImageModelCatalog>();
        catalog.Models.Returns([model]);
        catalog.Find(Arg.Any<string>()).Returns(ci => string.Equals((string)ci[0], model.Key, StringComparison.OrdinalIgnoreCase) ? model : null);
        return catalog;
    }

    private void InstallTinyModel()
    {
        var root = Path.Combine(_paths.ModelsDir, "image");
        Directory.CreateDirectory(Path.Combine(root, "diffusion"));
        Directory.CreateDirectory(Path.Combine(root, "vae"));
        File.WriteAllBytes(Path.Combine(root, "diffusion", "d.gguf"), DiffusionBytes);
        File.WriteAllBytes(Path.Combine(root, "vae", "ae.safetensors"), VaeBytes);
    }
}
