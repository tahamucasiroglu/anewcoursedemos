using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.DependencyInjection;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Core.Hub;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.DependencyInjection;

/// <summary><c>AddAgentCore</c> kaydı: tüm ana servisler motorsuz da çözülür (döngüsel bağımlılık yok).</summary>
public sealed class AgentCoreRegistrationTests
{
    /// <summary>Ana makine, komut işleyici, dağıtıcı ve kurulum yöneticisi çözülür (sağlayıcı <c>DisposeAsync</c> ile kapatılmalı).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AddAgentCore_ResolvesServices()
    {
        using var temp = new TempDirectory();
        var services = new ServiceCollection();
        services.AddAgentCore(new AgentOptions { RootDirectory = temp.Path, EnableFileLogging = false });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var host = provider.GetRequiredService<IAgentHost>();
        provider.GetRequiredService<IAgentClient>().ShouldNotBeNull();
        var dispatcher = provider.GetRequiredService<IStageExecutorDispatcher>();
        var installer = provider.GetRequiredService<IPrerequisiteInstaller>();

        host.IsRunning.ShouldBeFalse();
        host.Hub.Status.ShouldBe(HubConnectionStatus.Disconnected);
        foreach (var stage in new[] { PipelineStage.Images, PipelineStage.Speech, PipelineStage.Align, PipelineStage.SceneRender, PipelineStage.Compose, PipelineStage.Mux })
        {
            dispatcher.CanExecute(stage).ShouldBeTrue(stage.ToString());
        }

        installer.GetStatus().Select(s => s.Id).ShouldBe(["vc-redist", "node", "ffmpeg", "chrome-headless-shell", "uv", "python"]);
        installer.GetStatus().Where(s => s.IsOptional).Select(s => s.Id).ShouldBe(["vc-redist", "uv", "python"]);
    }
}
