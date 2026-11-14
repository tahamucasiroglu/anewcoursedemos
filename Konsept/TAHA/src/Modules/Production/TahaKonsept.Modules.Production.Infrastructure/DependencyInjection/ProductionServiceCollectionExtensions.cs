using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Concrete;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Application.Validators;
using TahaKonsept.Modules.Production.Infrastructure.Guides.Concrete;
using TahaKonsept.Modules.Production.Infrastructure.Hosting;
using TahaKonsept.Modules.Production.Infrastructure.Integration.Concrete;
using TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Production.Normalization.DependencyInjection;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Infrastructure.DependencyInjection;

/// <summary>
/// Production modülü DI kaydı.
/// <list type="bullet">
/// <item><see cref="AddProductionModule"/>: <see cref="ProductionDbContext"/> (şema <c>production</c>) + <see cref="AddProductionCore"/> + Concepts adaptörü
/// (<see cref="ConceptsConceptSource"/>, <c>Replace</c> ile yedeğin yerine) + Concepts anlatım hızı portu (<see cref="ProductionNarrationPaceSource"/>,
/// <c>Replace</c>: konsept uzunluğu seçili ses modelinin hızıyla) + orkestratör BackgroundService.</item>
/// <item><see cref="AddProductionCore"/> (testler de kullanır; DbContext hariç): depolar, servisler, aşama işleyicileri (11), hash/plan/planlayıcı,
/// iş fabrikası/ilerletici/bitirici, orkestratör adımı, sinyal ve kilit (singleton), yazım kılavuzları, okunuş normalizasyonu
/// (<c>AddSpeechTextNormalization</c>), <c>IAgentStageEventHandler</c> (Agents'ın no-op varsayılanının yerine — <c>Replace</c>),
/// yedek <see cref="IConceptSource"/> (TryAdd; entegrasyonda Concepts adaptörü önce/yerine kaydedilmeli), istatistik katkıcısı ve doğrulayıcılar.</item>
/// </list>
/// </summary>
public static class ProductionServiceCollectionExtensions
{
    /// <summary>Modülün tüm kayıtları (API host'u için).</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddProductionModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddModuleDbContext<ProductionDbContext>(configuration, ProductionDbContext.SchemaName);
        services.AddProductionCore();
        services.Replace(ServiceDescriptor.Scoped<IConceptSource, ConceptsConceptSource>());
        services.Replace(ServiceDescriptor.Scoped<INarrationPaceSource, ProductionNarrationPaceSource>());
        services.AddHostedService<ProductionOrchestratorService>();
        return services;
    }

    /// <summary>DbContext ve arka plan servisi hariç kayıtlar.</summary>
    /// <param name="services">Servisler.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddProductionCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSpeechTextNormalization();

        services.AddScoped<IProductionSettingsRepository, ProductionSettingsRepository>();
        services.AddScoped<IProductionQueueRepository, ProductionQueueRepository>();
        services.AddScoped<IQueueItemRepository, QueueItemRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IPronunciationRepository, PronunciationRepository>();
        services.AddScoped<ISpeechRateStatRepository, SpeechRateStatRepository>();
        services.AddScoped<ISpeechRateService, SpeechRateService>();

        services.AddSingleton<IStageInputHasher, Sha256StageInputHasher>();
        services.AddSingleton<IScenePlanner, NarrationScenePlanner>();
        services.AddSingleton<ISceneCodeValidator, EngineSceneCodeValidator>();
        services.AddSingleton<IWavCombiner, PcmWavCombiner>();
        services.AddSingleton<IAuthoringGuideProvider, EmbeddedAuthoringGuideProvider>();
        services.AddSingleton<IProductionSignal, ProductionSignal>();
        services.AddSingleton<IUserJobLock, UserJobLock>();
        services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();
        services.AddSingleton<IProductionActivity, ProductionActivity>();

        services.AddScoped<IStageHandler, ScriptStageHandler>();
        services.AddScoped<IStageHandler, FactSheetStageHandler>();
        services.AddScoped<IStageHandler, ScenePlanStageHandler>();
        services.AddScoped<IStageHandler, SceneCodeStageHandler>();
        services.AddScoped<IStageHandler, ImagesStageHandler>();
        services.AddScoped<IStageHandler, NormalizeStageHandler>();
        services.AddScoped<IStageHandler, SpeechStageHandler>();
        services.AddScoped<IStageHandler, AlignStageHandler>();
        services.AddScoped<IStageHandler, SceneRenderStageHandler>();
        services.AddScoped<IStageHandler, ComposeStageHandler>();
        services.AddScoped<IStageHandler, MuxStageHandler>();
        services.AddScoped<IStageHandlerRegistry, StageHandlerRegistry>();
        services.AddScoped<IReusePlanner, ReusePlanner>();
        services.AddScoped<IPipelineContextFactory, PipelineContextFactory>();

        services.AddScoped<IProductionSettingsService, ProductionSettingsService>();
        services.AddScoped<IProductionQueueService, ProductionQueueService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IPronunciationService, PronunciationService>();
        services.AddScoped<IPronunciationSuggestions, PronunciationSuggestions>();
        services.AddScoped<IJobQueryService, JobQueryService>();
        services.TryAddScoped<IConceptSource, UnavailableConceptSource>();

        services.AddScoped<IVideoVersionAllocator, VideoVersionAllocator>();
        services.AddScoped<IJobFactory, JobFactory>();
        services.AddScoped<IJobFinalizer, JobFinalizer>();
        services.AddScoped<JobAdvancerDependencies>();
        services.AddScoped<IJobAdvancer, JobAdvancer>();
        services.AddScoped<IProductionOrchestrator, ProductionOrchestrator>();
        services.AddScoped<StageEventHandlerDependencies>();
        services.Replace(ServiceDescriptor.Scoped<IAgentStageEventHandler, ProductionStageEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStatsContributor, ProductionStatsContributor>());

        services.AddScoped<IValidator<ProductionSettingsDto>, ProductionSettingsDtoValidator>();
        services.AddScoped<IValidator<AddToQueueRequest>, AddToQueueRequestValidator>();
        services.AddScoped<IValidator<MoveQueueItemRequest>, MoveQueueItemRequestValidator>();
        services.AddScoped<IValidator<SavePronunciationRequest>, SavePronunciationRequestValidator>();
        services.AddScoped<IValidator<PronunciationPreviewRequest>, PronunciationPreviewRequestValidator>();
        services.AddScoped<IValidator<NewVersionRequest>, NewVersionRequestValidator>();
        services.AddScoped<IValidator<RegenerateRequest>, RegenerateRequestValidator>();
        return services;
    }
}
