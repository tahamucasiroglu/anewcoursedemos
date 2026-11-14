using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Concepts.Api.Jobs;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Concrete;
using TahaKonsept.Modules.Concepts.Application.Validators;
using TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Concepts.Infrastructure.Stats.Concrete;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Api;

/// <summary>
/// Concepts modülü kaydı: <see cref="ConceptsDbContext"/> (şema <c>concepts</c>), depolar, servisler, singleton'lar (prompt kütüphanesi,
/// şablon işleyici, prompt fabrikası, normalleştirici, analizci, aday deposu, iş kuyruğu/izleyici), Production için
/// <see cref="IConceptReader"/> + <see cref="IConceptPronunciationSource"/>, istatistik katkıcısı, doğrulayıcılar ve
/// arka plan işçisi (<see cref="ConceptJobWorker"/>).
/// </summary>
public sealed class ConceptsModule : ModuleBase
{
    /// <summary>Modül adı.</summary>
    public override string Name => "Concepts";

    /// <summary>Servisleri kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ConceptsDbContext>(configuration, ConceptsDbContext.SchemaName);
        services.AddScoped<IConceptRepository, ConceptRepository>();
        services.AddScoped<ITopicMemoryRepository, TopicMemoryRepository>();
        services.AddScoped<IConceptSettingsRepository, ConceptSettingsRepository>();

        services.AddSingleton<IPromptLibrary, EmbeddedPromptLibrary>();
        services.AddSingleton<IPromptTemplateRenderer, PromptTemplateRenderer>();
        services.AddSingleton<IConceptPromptFactory, ConceptPromptFactory>();
        services.AddSingleton<ITopicNormalizer, TurkishTopicNormalizer>();
        services.AddSingleton<IWordRepetitionAnalyzer, WordRepetitionAnalyzer>();
        services.AddSingleton<IRevisionCandidateStore, InMemoryRevisionCandidateStore>();
        services.AddSingleton<IConceptJobQueue, ChannelConceptJobQueue>();
        services.AddSingleton<IConceptJobTracker, InMemoryConceptJobTracker>();

        services.AddScoped<IConceptContextProvider, ConceptContextProvider>();
        services.AddScoped<ITopicMemory, TopicMemory>();
        services.AddScoped<ITopicGenerator, TopicGenerator>();
        services.AddScoped<IConceptGenerator, ConceptGenerator>();
        services.AddScoped<IWordRepetitionChecker, WordRepetitionChecker>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddScoped<IConceptSettingsService, ConceptSettingsService>();
        services.AddScoped<ITopicSuggestionService, TopicSuggestionService>();
        services.AddScoped<IConceptGenerationService, ConceptGenerationService>();
        services.AddScoped<IConceptRevisionService, ConceptRevisionService>();
        services.AddScoped<IConceptJobProcessor, ConceptJobProcessor>();
        services.AddScoped<ConceptReader>();
        services.AddScoped<IConceptReader>(sp => sp.GetRequiredService<ConceptReader>());
        services.AddScoped<IConceptPronunciationSource>(sp => sp.GetRequiredService<ConceptReader>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStatsContributor, ConceptsStatsContributor>());

        services.AddScoped<IValidator<SuggestTopicsRequest>, SuggestTopicsRequestValidator>();
        services.AddScoped<IValidator<GenerateFromTopicsRequest>, GenerateFromTopicsRequestValidator>();
        services.AddScoped<IValidator<GenerateFromIdeaRequest>, GenerateFromIdeaRequestValidator>();
        services.AddScoped<IValidator<DislikeTopicsRequest>, DislikeTopicsRequestValidator>();
        services.AddScoped<IValidator<UpdateConceptRequest>, UpdateConceptRequestValidator>();
        services.AddScoped<IValidator<ReviseConceptRequest>, ReviseConceptRequestValidator>();
        services.AddScoped<IValidator<ConceptSettingsDto>, ConceptSettingsDtoValidator>();

        services.AddHostedService<ConceptJobWorker>();
    }

    /// <summary>Migration'ları uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        MigrateDbContextAsync<ConceptsDbContext>(services, cancellationToken);
}
