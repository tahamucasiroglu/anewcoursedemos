using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Engines.Image.Abstract;
using TahaKonsept.Agent.Engines.Image.DependencyInjection;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.DependencyInjection;

/// <summary><c>AddImageEngines</c> kaydı: iki motor çözülür, ayarlar uygulanır.</summary>
public sealed class AddImageEnginesTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>IImageEngine ve ILocalImageEngine olarak sd.cpp + ComfyUI çözülür; aynı örnek paylaşılır.</summary>
    [Fact]
    public void Registers_both_engines()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAgentPaths>(new FakeAgentPaths(_temp.Path));
        services.AddSingleton<IProcessRunner>(new FakeProcessRunner());
        services.AddImageEngines(o => o.ComfyUiBaseUrl = "http://127.0.0.1:18188");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var engines = provider.GetServices<IImageEngine>().ToList();
        engines.Select(e => e.Key).ShouldBe(["stable-diffusion-cpp", "comfyui"]);
        provider.GetServices<ILocalImageEngine>().First().ShouldBeSameAs(engines[0]);
        provider.GetRequiredService<IOptions<ImageEngineOptions>>().Value.ComfyUiBaseUrl.ShouldBe("http://127.0.0.1:18188");
    }
}
