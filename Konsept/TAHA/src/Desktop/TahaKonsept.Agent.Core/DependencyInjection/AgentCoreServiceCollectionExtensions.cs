using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Audio.Abstract;
using TahaKonsept.Agent.Core.Audio.Concrete;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Auth.Concrete;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Capabilities.Concrete;
using TahaKonsept.Agent.Core.Commands.Concrete;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Configuration.Concrete;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.Agent.Core.Downloads.Concrete;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Execution.Concrete;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Core.Hosting.Concrete;
using TahaKonsept.Agent.Core.Http;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Http.Concrete;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Hub.Concrete;
using TahaKonsept.Agent.Core.Media.Abstract;
using TahaKonsept.Agent.Core.Media.Concrete;
using TahaKonsept.Agent.Core.Networking.Abstract;
using TahaKonsept.Agent.Core.Networking.Concrete;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.Agent.Core.Pairing.Concrete;
using TahaKonsept.Agent.Core.Paths.Concrete;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Concrete;
using TahaKonsept.Agent.Core.Processes.Concrete;
using TahaKonsept.Agent.Core.Shell.Abstract;
using TahaKonsept.Agent.Core.Shell.Concrete;
using TahaKonsept.Agent.Core.SpeechPreview.Abstract;
using TahaKonsept.Agent.Core.SpeechPreview.Concrete;
using TahaKonsept.Agent.Core.Tools.Concrete;
using TahaKonsept.Agent.Core.VideoServer.Abstract;
using TahaKonsept.Agent.Core.VideoServer.Concrete;
using TahaKonsept.Agent.Core.Videos.Abstract;
using TahaKonsept.Agent.Core.Videos.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Logging;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.DependencyInjection;

/// <summary>
/// Ajan çekirdeğinin DI kaydı. Avalonia uygulaması: <c>services.AddAgentCore(options)</c> + motor projelerinin kendi kayıtları
/// (<c>IVideoEngine</c>, <c>IComposer</c>, <c>IAligner</c>, <c>ISpeechEngine</c>, <c>IImageEngine</c>), sonra <c>IAgentHost.StartAsync</c>.
/// Serilog dosya logu <c>Logs/agent-YYYYMMDD.log</c>, 7 gün.
/// </summary>
public static class AgentCoreServiceCollectionExtensions
{
    /// <summary>Tüm çekirdek servisleri ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="options">Seçenekler.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddAgentCore(this IServiceCollection services, AgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var paths = new AgentPaths(options);
        services.AddSingleton(options);
        services.AddSingleton<IAgentPaths>(paths);
        if (options.EnableFileLogging)
        {
            services.AddTahaSerilog(options.LogAppName, paths.LogsDir);
        }
        else
        {
            services.AddLogging();
        }

        services.TryAddSingleton(TimeProvider.System);
        AddHttpClients(services, options);

        services.AddSingleton<IAgentSettingsStore, AgentSettingsStore>();
        services.AddSingleton<IToolLocator, ToolLocator>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<ITokenStore, TokenStore>();
        services.AddSingleton<IAgentAuthService, AgentAuthService>();
        services.AddSingleton<IDevicePairingService, DevicePairingService>();
        services.AddSingleton<ILanAddressProvider, LanAddressProvider>();
        services.AddSingleton<ICapabilityDetector, CapabilityDetector>();
        services.AddSingleton<IAgentHelloBuilder, AgentHelloBuilder>();

        services.AddSingleton<AgentHubConnection>();
        services.AddSingleton<IAgentHubConnection>(sp => sp.GetRequiredService<AgentHubConnection>());
        services.AddSingleton<IAgentReporter>(sp => sp.GetRequiredService<AgentHubConnection>());
        services.AddSingleton<IAgentClient, AgentCommandHandler>();

        services.AddSingleton<IMediaProbe, FfmpegMediaProbe>();
        services.AddSingleton<IAudioConcatenator, WavAudioConcatenator>();
        services.AddSingleton<IShellLauncher, WindowsShellLauncher>();
        services.AddSingleton<IVideoIndex, VideoIndex>();
        services.AddSingleton<IVideoLibrary, VideoLibrary>();
        services.AddSingleton<ISpeechPreviewService, SpeechPreviewService>();

        services.AddSingleton<IStageExecutor, ImagesStageExecutor>();
        services.AddSingleton<IStageExecutor, SpeechStageExecutor>();
        services.AddSingleton<IStageExecutor, AlignStageExecutor>();
        services.AddSingleton<IStageExecutor, SceneRenderStageExecutor>();
        services.AddSingleton<IStageExecutor, ComposeStageExecutor>();
        services.AddSingleton<IStageExecutor, MuxStageExecutor>();
        services.AddSingleton<IStageExecutorDispatcher, StageExecutorDispatcher>();
        services.AddSingleton<IWorkspaceUsage, WorkspaceUsage>();
        services.AddSingleton<IJobRunner, JobRunner>();
        services.AddSingleton<IVideoServer, LocalVideoServer>();

        services.AddSingleton<IFileDownloader, HttpFileDownloader>();
        services.AddSingleton<IPrerequisite, VcRedistPrerequisite>();
        services.AddSingleton<IPrerequisite, NodePrerequisite>();
        services.AddSingleton<IPrerequisite, FfmpegPrerequisite>();
        services.AddSingleton<IPrerequisite, ChromeHeadlessShellPrerequisite>();
        services.AddSingleton<IPrerequisite, UvPrerequisite>();
        services.AddSingleton<IPrerequisite, PythonPrerequisite>();
        services.AddSingleton<IPrerequisiteInstaller, PrerequisiteInstaller>();
        services.AddSingleton<IToolProvisioner, PrerequisiteToolProvisioner>();

        services.AddSingleton<IAgentHost, AgentHost>();
        return services;
    }

    private static void AddHttpClients(IServiceCollection services, AgentOptions options)
    {
        services.AddHttpClient(AgentHttpClientNames.Anonymous, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient(AgentHttpClientNames.Api, c => c.Timeout = TimeSpan.FromMinutes(30));
        services.AddHttpClient(AgentHttpClientNames.Downloads, c =>
        {
            c.Timeout = TimeSpan.FromHours(1);
            c.DefaultRequestHeaders.UserAgent.TryParseAdd("TahaKonsept-Agent/" + options.AgentVersion.Split('+')[0]);
        });

        services.AddSingleton<IAnonymousAgentApiClient>(sp => new AnonymousAgentApiClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(AgentHttpClientNames.Anonymous),
            sp.GetRequiredService<IAgentSettingsStore>()));
        services.AddSingleton<IAgentApiClient>(sp => new AgentApiClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(AgentHttpClientNames.Api),
            sp.GetRequiredService<IAgentSettingsStore>(),
            sp.GetRequiredService<IAgentAuthService>()));
    }
}
