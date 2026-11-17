using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Concrete;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Infrastructure.Jobs.Concrete;
using Konsept.Modules.Concepts.Infrastructure.Persistence;
using Konsept.Modules.Concepts.Infrastructure.Persistence.Concrete;
using Konsept.Modules.Concepts.Infrastructure.Rules;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Shared.Infrastructure.Http.Concrete;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Modules.Concepts.Infrastructure;

/// <summary>Concepts modülünün servis kaydı ve açılış migration'ı (spec 0014).</summary>
public static class ConceptsModule
{
    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="connectionString">PostgreSQL bağlantı metni.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddConceptsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ConceptsDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ConceptsDbContext.SchemaName)));
        services.AddScoped<IConceptRepository, ConceptRepository>();
        services.AddScoped<ITopicSuggestionRepository, TopicSuggestionRepository>();
        services.AddSingleton<IConceptRuleSource, EmbeddedConceptRuleSource>();
        services.AddSingleton<IConceptJobRegistry, ConceptJobRegistry>();
        services.AddSingleton<IConceptJobLauncher, ConceptJobLauncher>();
        services.AddScoped<IConceptJobExecutor, ConceptJobExecutor>();
        services.AddScoped<IConceptWorkService, ConceptWorkService>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddSingleton<IConceptRevisionRegistry, ConceptRevisionRegistry>();
        services.AddSingleton<IConceptRegenerationContext, ConceptRegenerationContext>(); // Concepts.Contracts (Production açar, spec 0020).
        services.AddScoped<IConceptRevisionExecutor, ConceptRevisionExecutor>();
        services.AddScoped<IConceptEditService, ConceptEditService>();
        services.AddScoped<IConceptTrashService, ConceptTrashService>();
        services.AddScoped<IProfileConceptPresence, ProfileConceptPresence>(); // Concepts.Contracts (Profiles kullanır).
        services.AddScoped<IConceptQueueBridge, ConceptQueueBridge>(); // Concepts.Contracts (Production kullanır, spec 0006).
        services.AddScoped<IUserSessionsEndedListener, ConceptSessionListener>(); // Identity.Contracts: çıkış/ban partiyi durdurur (0014 Req 20, 0024).
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }

    /// <summary>Modülün şemasını bekleyen migration'larla günceller.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public static async Task MigrateConceptsDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConceptsDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
