using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Abstractions.Gpu.Concrete;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Concrete;
using TahaKonsept.Agent.Engines.Speech.Concrete.Python;
using TahaKonsept.Agent.Engines.Speech.Devices.Abstract;
using TahaKonsept.Agent.Engines.Speech.Devices.Concrete;
using TahaKonsept.Agent.Engines.Speech.Onnx.Abstract;
using TahaKonsept.Agent.Engines.Speech.Onnx.Concrete;

namespace TahaKonsept.Agent.Engines.Speech.DependencyInjection;

/// <summary>
/// Yerel ses motorlarının DI kaydı. <see cref="AddSpeechEngines"/>: indirme HTTP istemcisi, "Önce GPU" yardımcıları
/// (<see cref="IGpuCapabilityProbe"/>, <see cref="ITorchDeviceSelector"/> → <see cref="NvidiaTorchDeviceSelector"/>,
/// <see cref="IOnnxSessionFactory"/> → <see cref="DirectMlOnnxSessionFactory"/>), <see cref="SpeechEngineServices"/> ve
/// 14 motoru tekil (singleton) olarak hem somut türüyle hem <see cref="ISpeechEngine"/> olarak kaydeder.
/// Ön koşul: <c>IAgentPaths</c>, <c>IToolLocator</c>, <c>IProcessRunner</c> ve loglama başka yerde (Agent.Core) kayıtlı olmalı.
/// </summary>
public static class SpeechEngineServiceCollectionExtensions
{
    /// <summary>Tüm yerel ses motorlarını kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddSpeechEngines(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpClient(SpeechEngineServices.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromHours(2);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TahaKonsept-Agent/1.0");
        });
        services.AddLogging();
        services.TryAddSingleton<IGpuCapabilityProbe, WindowsGpuCapabilityProbe>();
        services.TryAddSingleton<ITorchDeviceSelector, NvidiaTorchDeviceSelector>();
        services.TryAddSingleton<IOnnxSessionFactory, DirectMlOnnxSessionFactory>();
        services.TryAddSingleton<SpeechEngineServices>();

        AddEngine<SupertonicSpeechEngine>(services);
        AddEngine<ChatterboxSpeechEngine>(services);
        AddEngine<EmaLightningSpeechEngine>(services);
        AddEngine<AhenkSpeechEngine>(services);
        AddEngine<AhenkCepSpeechEngine>(services);
        AddEngine<TrendyolSpeechEngine>(services);
        AddEngine<XttsV2SpeechEngine>(services);
        AddEngine<F5TtsTurkishSpeechEngine>(services);
        AddEngine<PiperTurkishSpeechEngine>(services);
        AddEngine<MmsTurkishSpeechEngine>(services);
        AddEngine<OmniVoiceSpeechEngine>(services);
        AddEngine<VoxCpm2SpeechEngine>(services);
        AddEngine<FishS2ProSpeechEngine>(services);
        AddEngine<KokoroSpeechEngine>(services);
        return services;
    }

    private static void AddEngine<TEngine>(IServiceCollection services)
        where TEngine : class, ISpeechEngine
    {
        services.TryAddSingleton<TEngine>();
        services.AddSingleton<ISpeechEngine>(sp => sp.GetRequiredService<TEngine>());
    }
}
