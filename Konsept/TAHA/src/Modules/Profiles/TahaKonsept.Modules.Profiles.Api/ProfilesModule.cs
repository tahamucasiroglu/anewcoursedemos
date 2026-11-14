using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Concrete;
using TahaKonsept.Modules.Profiles.Application.Validation;
using TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Concrete;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Api;

/// <summary>
/// Profiles modülü kaydı: <see cref="ProfilesDbContext"/> (şema <c>profiles</c>), repository, <see cref="IProfileService"/>,
/// diğer modüller için <see cref="IProfileReader"/> ve <see cref="IProfilePromptBuilder"/>, şablon kataloğu (singleton) ve doğrulayıcı.
/// </summary>
public sealed class ProfilesModule : ModuleBase
{
    /// <summary>Modül adı.</summary>
    public override string Name => "Profiles";

    /// <summary>Servisleri kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProfilesDbContext>(configuration, ProfilesDbContext.SchemaName);
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProfileReader, ProfileReader>();
        services.AddSingleton<IProfilePromptBuilder, ProfilePromptBuilder>();
        services.AddSingleton<IProfileTemplateCatalog, EmbeddedProfileTemplateCatalog>();
        services.AddScoped<IValidator<SaveProfileRequest>, SaveProfileRequestValidator>();
    }

    /// <summary>Migration'ları uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        MigrateDbContextAsync<ProfilesDbContext>(services, cancellationToken);
}
