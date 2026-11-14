using System.IO.Compression;
using System.Net;
using System.Text;
using Shouldly;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.StableDiffusionCpp;

/// <summary>sd.cpp kurucusu: hızlandırıcı seçimi, sürüm dosyası seçimi ve sahte GitHub ile uçtan uca kurulum.</summary>
public sealed class SdCppInstallerTests : IDisposable
{
    private const string Tag = "master-1094-3bad892";
    private readonly TempDirectory _temp = new();

    private static readonly GitHubRelease Release = new(Tag,
    [
        Asset("cudart-sd-bin-win-cu12-x64.zip"),
        Asset("sd-master-3bad892-bin-Darwin-macOS-26.7.2-arm64.zip"),
        Asset("sd-master-3bad892-bin-Linux-Ubuntu-26.04-x86_64-vulkan.zip"),
        Asset("sd-master-3bad892-bin-win-cpu-x64.zip"),
        Asset("sd-master-3bad892-bin-win-cuda12-x64.zip"),
        Asset("sd-master-3bad892-bin-win-rocm-10.1.0-x64.zip"),
        Asset("sd-master-3bad892-bin-win-vulkan-x64.zip"),
    ]);

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>CUDA: ikili + cudart; Vulkan ve CPU: tek dosya (Linux/mac/ROCm seçilmez).</summary>
    [Fact]
    public void Selects_windows_assets_per_backend()
    {
        var installer = EngineFactory.CreateInstaller(new FakeAgentPaths(_temp.Path), Handler404());

        installer.SelectAssets(Release, SdCppBackend.Cuda).Select(a => a.Name)
            .ShouldBe(["sd-master-3bad892-bin-win-cuda12-x64.zip", "cudart-sd-bin-win-cu12-x64.zip"]);
        installer.SelectAssets(Release, SdCppBackend.Vulkan).Single().Name.ShouldBe("sd-master-3bad892-bin-win-vulkan-x64.zip");
        installer.SelectAssets(Release, SdCppBackend.Cpu).Single().Name.ShouldBe("sd-master-3bad892-bin-win-cpu-x64.zip");
        installer.SelectAssets(new GitHubRelease(Tag, []), SdCppBackend.Cuda).ShouldBeEmpty();
    }

    /// <summary>Otomatik tercih: NVIDIA → CUDA, yoksa Vulkan, yoksa CPU; açık tercih donanımı ezer.</summary>
    [Fact]
    public void Resolves_backend_from_hardware_and_preference()
    {
        var paths = new FakeAgentPaths(_temp.Path);
        EngineFactory.CreateInstaller(paths, Handler404(), hasNvidia: true).ResolveBackend().ShouldBe(SdCppBackend.Cuda);
        EngineFactory.CreateInstaller(paths, Handler404(), hasNvidia: false, hasVulkan: true).ResolveBackend().ShouldBe(SdCppBackend.Vulkan);
        EngineFactory.CreateInstaller(paths, Handler404(), hasNvidia: false, hasVulkan: false).ResolveBackend().ShouldBe(SdCppBackend.Cpu);
        EngineFactory.CreateInstaller(paths, Handler404(), hasNvidia: true, options: new ImageEngineOptions { SdCppBackend = SdCppBackendPreference.Vulkan })
            .ResolveBackend().ShouldBe(SdCppBackend.Vulkan);
    }

    /// <summary>Sahte GitHub: sürüm JSON'u + zip'ler indirilir, açılır, <c>sd-cli.exe</c> bulunur, release.txt yazılır.</summary>
    [Fact]
    public async Task Installs_from_release_and_finds_cli()
    {
        var cliZip = Zip(("sd-cli.exe", "fake-exe"), ("ggml-cuda.dll", "dll"));
        var cudartZip = Zip(("cudart64_12.dll", "rt"));
        var releaseJson = $$"""
            {"tag_name":"{{Tag}}","assets":[
              {"name":"sd-master-3bad892-bin-win-cuda12-x64.zip","size":{{cliZip.Length}},"browser_download_url":"https://dl.test/cli.zip"},
              {"name":"cudart-sd-bin-win-cu12-x64.zip","size":{{cudartZip.Length}},"browser_download_url":"https://dl.test/rt.zip"}]}
            """;
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.AbsoluteUri switch
        {
            var u when u.Contains("api.github.com", StringComparison.Ordinal) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson, Encoding.UTF8, "application/json") },
            "https://dl.test/cli.zip" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(cliZip) },
            "https://dl.test/rt.zip" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(cudartZip) },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var installer = EngineFactory.CreateInstaller(new FakeAgentPaths(_temp.Path), handler);
        installer.FindExecutable().ShouldBeNull();

        var result = await installer.EnsureInstalledAsync(null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.ShouldBe(Path.Combine(installer.InstallDir, "sd-cli.exe"));
        File.Exists(Path.Combine(installer.InstallDir, "cudart64_12.dll")).ShouldBeTrue();
        (await File.ReadAllTextAsync(Path.Combine(installer.InstallDir, "release.txt"), TestContext.Current.CancellationToken)).ShouldContain("Cuda");
        Directory.Exists(Path.Combine(installer.InstallDir, "_download")).ShouldBeFalse();
    }

    /// <summary>GitHub'a ulaşılamazsa istisna değil, anlaşılır hata döner.</summary>
    [Fact]
    public async Task Returns_failure_when_release_api_fails()
    {
        var installer = EngineFactory.CreateInstaller(new FakeAgentPaths(_temp.Path), Handler404());

        var result = await installer.EnsureInstalledAsync(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("sdcpp.release.http");
    }

    private static GitHubReleaseAsset Asset(string name) => new(name, 100, "https://dl.test/" + name);

    private static StubHttpMessageHandler Handler404() => new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }
}
