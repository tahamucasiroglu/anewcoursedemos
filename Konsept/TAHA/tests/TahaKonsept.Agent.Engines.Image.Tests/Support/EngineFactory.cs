using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog.Concrete;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Concrete;
using TahaKonsept.Agent.Engines.Image.Downloads.Concrete;
using TahaKonsept.Agent.Engines.Image.Gpu.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Testlerde motor ve yardımcılarını DI olmadan kurar.</summary>
public static class EngineFactory
{
    /// <summary>Sahte HTTP ile indirici.</summary>
    /// <param name="handler">HTTP işleyicisi.</param>
    /// <param name="options">Ayarlar (isteğe bağlı).</param>
    /// <returns>İndirici.</returns>
    public static HttpModelFileDownloader CreateDownloader(HttpMessageHandler handler, ImageEngineOptions? options = null) =>
        new(new StubHttpClientFactory(handler), MsOptions.Create(options ?? new ImageEngineOptions()), NullLogger<HttpModelFileDownloader>.Instance);

    /// <summary>sd.cpp kurucusu.</summary>
    /// <param name="paths">Klasörler.</param>
    /// <param name="handler">HTTP işleyicisi.</param>
    /// <param name="hasNvidia">NVIDIA sürücüsü var mı.</param>
    /// <param name="hasVulkan">Vulkan var mı.</param>
    /// <param name="options">Ayarlar (isteğe bağlı).</param>
    /// <returns>Kurucu.</returns>
    public static GitHubSdCppInstaller CreateInstaller(IAgentPaths paths, HttpMessageHandler handler, bool hasNvidia = true, bool hasVulkan = true, ImageEngineOptions? options = null)
    {
        var gpu = Substitute.For<IGpuCapabilityProbe>();
        gpu.HasNvidiaDriver.Returns(hasNvidia);
        gpu.HasVulkanRuntime.Returns(hasVulkan);
        var opts = options ?? new ImageEngineOptions();
        return new GitHubSdCppInstaller(
            new StubHttpClientFactory(handler),
            gpu,
            CreateDownloader(handler, opts),
            paths,
            MsOptions.Create(opts),
            NullLogger<GitHubSdCppInstaller>.Instance);
    }

    /// <summary>sd.cpp motoru.</summary>
    /// <param name="paths">Klasörler.</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="handler">HTTP işleyicisi (isteğe bağlı; yoksa her istek 404).</param>
    /// <param name="options">Ayarlar (isteğe bağlı).</param>
    /// <param name="catalog">Katalog (isteğe bağlı; varsayılan gömülü katalog).</param>
    /// <param name="gpuMemory">Boş VRAM yoklaması (isteğe bağlı; varsayılan "bilinmiyor").</param>
    /// <returns>Motor.</returns>
    public static StableDiffusionCppImageEngine CreateSdCpp(IAgentPaths paths, IProcessRunner runner, HttpMessageHandler? handler = null, ImageEngineOptions? options = null, IImageModelCatalog? catalog = null, IGpuMemoryProbe? gpuMemory = null)
    {
        var builder = new SdCppArgumentBuilder();
        var probe = gpuMemory ?? Substitute.For<IGpuMemoryProbe>();
        var opts = options ?? new ImageEngineOptions();
        var http = handler ?? new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        return new StableDiffusionCppImageEngine(
            catalog ?? new EmbeddedImageModelCatalog(),
            CreateDownloader(http, opts),
            CreateInstaller(paths, http, options: opts),
            builder,
            runner,
            paths,
            MsOptions.Create(opts),
            NullLogger<StableDiffusionCppImageEngine>.Instance,
            new SdCppMemoryPlanner(builder),
            probe);
    }

    /// <summary>ComfyUI motoru.</summary>
    /// <param name="paths">Klasörler.</param>
    /// <param name="handler">ComfyUI'yi taklit eden HTTP işleyicisi.</param>
    /// <param name="options">Ayarlar (isteğe bağlı).</param>
    /// <returns>Motor.</returns>
    public static ComfyUiImageEngine CreateComfy(IAgentPaths paths, HttpMessageHandler handler, ImageEngineOptions? options = null)
    {
        var opts = options ?? new ImageEngineOptions { ComfyUiPollInterval = TimeSpan.FromMilliseconds(10) };
        var factory = new StubHttpClientFactory(handler);
        return new ComfyUiImageEngine(
            new EmbeddedImageModelCatalog(),
            CreateDownloader(handler, opts),
            new ComfyUiHttpClient(factory, MsOptions.Create(opts), NullLogger<ComfyUiHttpClient>.Instance),
            new EmbeddedComfyUiWorkflowProvider(),
            paths,
            MsOptions.Create(opts),
            NullLogger<ComfyUiImageEngine>.Instance);
    }
}
