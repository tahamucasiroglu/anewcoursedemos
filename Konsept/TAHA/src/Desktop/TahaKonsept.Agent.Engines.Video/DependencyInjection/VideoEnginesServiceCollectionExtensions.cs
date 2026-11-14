using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.Agent.Abstractions.Align.Abstract;
using TahaKonsept.Agent.Abstractions.Compose.Abstract;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Engines.Video.Align.Concrete;
using TahaKonsept.Agent.Engines.Video.Compose.Concrete;
using TahaKonsept.Agent.Engines.Video.Engines.Concrete;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.Agent.Engines.Video.Media.Concrete;
using TahaKonsept.Agent.Engines.Video.Safety.Abstract;
using TahaKonsept.Agent.Engines.Video.Safety.Concrete;
using TahaKonsept.Agent.Engines.Video.Subtitles.Abstract;
using TahaKonsept.Agent.Engines.Video.Subtitles.Concrete;

namespace TahaKonsept.Agent.Engines.Video.DependencyInjection;

/// <summary>
/// Video motorları, birleştirici ve hizalayıcının DI kaydı. Önkoşul: <c>IAgentPaths</c>, <c>IToolLocator</c>, <c>IProcessRunner</c>
/// ve <c>ILogger&lt;T&gt;</c> başka yerde (Agent.Core / host) kayıtlı olmalı.
/// </summary>
public static class VideoEnginesServiceCollectionExtensions
{
    /// <summary>
    /// Kaydeder: <see cref="IVideoEngine"/> ×3 (HyperFrames, Remotion, Revideo; ayrıca <see cref="IAgentEngine"/> ve somut türleriyle),
    /// <see cref="IComposer"/> (<see cref="FfmpegComposer"/>), <see cref="IAligner"/> (<see cref="ProportionalAligner"/>),
    /// <see cref="IMediaProbe"/>, <see cref="ISceneCodeGuard"/>, <see cref="ICaptionSubtitleWriter"/>. Tümü singleton.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddVideoEngines(this IServiceCollection services)
    {
        services.TryAddSingleton<IMediaProbe, FfprobeMediaProbe>();
        services.TryAddSingleton<ISceneCodeGuard, DefaultSceneCodeGuard>();
        services.TryAddSingleton<ICaptionSubtitleWriter, AssKaraokeSubtitleWriter>();

        AddEngine<HyperFramesVideoEngine>(services);
        AddEngine<RemotionVideoEngine>(services);
        AddEngine<RevideoVideoEngine>(services);

        services.TryAddSingleton<IComposer, FfmpegComposer>();
        services.TryAddSingleton<ProportionalAligner>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAligner, ProportionalAligner>(sp => sp.GetRequiredService<ProportionalAligner>()));
        return services;
    }

    private static void AddEngine<TEngine>(IServiceCollection services)
        where TEngine : class, IVideoEngine
    {
        services.TryAddSingleton<TEngine>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IVideoEngine, TEngine>(sp => sp.GetRequiredService<TEngine>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEngine, TEngine>(sp => sp.GetRequiredService<TEngine>()));
    }
}
