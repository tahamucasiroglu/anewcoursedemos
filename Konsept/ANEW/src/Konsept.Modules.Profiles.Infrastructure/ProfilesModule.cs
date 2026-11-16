using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Concrete;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Modules.Profiles.Infrastructure.Persistence;
using Konsept.Modules.Profiles.Infrastructure.Persistence.Concrete;
using Konsept.Modules.Profiles.Infrastructure.Templates;
using Konsept.Shared.Infrastructure.Http.Concrete;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Modules.Profiles.Infrastructure;

/// <summary>Profiles modülünün servis kaydı ve açılış migration'ı.</summary>
public static class ProfilesModule
{
    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="connectionString">PostgreSQL bağlantı metni.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddProfilesModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProfilesDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProfilesDbContext.SchemaName)));
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddSingleton<IProfileTemplateSource, EmbeddedTemplateSource>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IConceptSettingsRepository, ConceptSettingsRepository>(); // spec 0005
        services.AddScoped<IConceptSettingsService, ConceptSettingsService>();
        services.AddScoped<IProfileGenerationReader, ProfileGenerationReader>(); // Profiles.Contracts (spec 0014)
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }

    /// <summary>Modülün şemasını bekleyen migration'larla günceller.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public static async Task MigrateProfilesDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProfilesDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
