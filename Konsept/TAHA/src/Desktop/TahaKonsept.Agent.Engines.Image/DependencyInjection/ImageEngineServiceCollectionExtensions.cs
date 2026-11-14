using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Engines.Image.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog.Abstract;
using TahaKonsept.Agent.Engines.Image.Catalog.Concrete;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Abstract;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Concrete;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.Agent.Engines.Image.Downloads.Concrete;
using TahaKonsept.Agent.Engines.Image.Gpu.Abstract;
using TahaKonsept.Agent.Engines.Image.Gpu.Concrete;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;

namespace TahaKonsept.Agent.Engines.Image.DependencyInjection;

/// <summary>
/// Yerel görsel motorlarını kaydeder: <see cref="StableDiffusionCppImageEngine"/> ve <see cref="ComfyUiImageEngine"/>
/// (ikisi de <see cref="IImageEngine"/> ve <see cref="ILocalImageEngine"/> olarak), katalog, indirici, sd.cpp kurucusu, ComfyUI istemcisi.
/// Önkoşul: <c>IAgentPaths</c> ve <c>IProcessRunner</c> ajan çekirdeğince kaydedilmiş olmalı; <c>IToolLocator</c> isteğe bağlıdır.
/// </summary>
public static class ImageEngineServiceCollectionExtensions
{
    /// <summary>Yerel görsel motorlarını ve yardımcılarını ekler.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configure">Ayarları değiştirmek için (isteğe bağlı).</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddImageEngines(this IServiceCollection services, Action<ImageEngineOptions>? configure = null)
    {
        var options = services.AddOptions<ImageEngineOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.AddHttpClient(ImageEngineOptions.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TahaKonsept-Agent", "1.0"));
        });

        services.TryAddSingleton<IImageModelCatalog, EmbeddedImageModelCatalog>();
        services.TryAddSingleton<IModelFileDownloader, HttpModelFileDownloader>();
        services.TryAddSingleton<IGpuCapabilityProbe, WindowsGpuCapabilityProbe>();
        services.TryAddSingleton<ISdCppInstaller, GitHubSdCppInstaller>();
        services.TryAddSingleton<ISdCppArgumentBuilder, SdCppArgumentBuilder>();
        services.TryAddSingleton<ISdCppMemoryPlanner, SdCppMemoryPlanner>();
        services.TryAddSingleton<IGpuMemoryProbe, NvidiaSmiGpuMemoryProbe>();
        services.TryAddSingleton<IComfyUiClient, ComfyUiHttpClient>();
        services.TryAddSingleton<IComfyUiWorkflowProvider, EmbeddedComfyUiWorkflowProvider>();

        services.TryAddSingleton<StableDiffusionCppImageEngine>();
        services.TryAddSingleton<ComfyUiImageEngine>();
        services.AddSingleton<IImageEngine>(sp => sp.GetRequiredService<StableDiffusionCppImageEngine>());
        services.AddSingleton<IImageEngine>(sp => sp.GetRequiredService<ComfyUiImageEngine>());
        services.AddSingleton<ILocalImageEngine>(sp => sp.GetRequiredService<StableDiffusionCppImageEngine>());
        services.AddSingleton<ILocalImageEngine>(sp => sp.GetRequiredService<ComfyUiImageEngine>());
        return services;
    }
}
