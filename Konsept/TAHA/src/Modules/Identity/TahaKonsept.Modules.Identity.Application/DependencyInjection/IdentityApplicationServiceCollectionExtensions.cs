using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Concrete;
using TahaKonsept.Modules.Identity.Application.Validators;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.DependencyInjection;

/// <summary>
/// Identity uygulama servislerinin kaydı: <see cref="IAuthService"/>, <see cref="TokenService"/> (tek örnek üç arayüz:
/// <see cref="ITokenIssuer"/>, <see cref="ITokenRevoker"/>, <see cref="IRefreshTokenRotator"/>), <see cref="IUserDirectory"/>,
/// <see cref="IUserProfileService"/>, <see cref="IPermissionResolver"/>, <see cref="IRefreshTokenGenerator"/>,
/// varsayılan <see cref="ISessionRevocationNotifier"/> (no-op) ve doğrulayıcılar.
/// </summary>
public static class IdentityApplicationServiceCollectionExtensions
{
    /// <summary>Servisleri ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IRefreshTokenGenerator, RandomRefreshTokenGenerator>();
        services.TryAddScoped<IPermissionResolver, PermissionResolver>();
        services.TryAddScoped<TokenService>();
        services.TryAddScoped<ITokenIssuer>(sp => sp.GetRequiredService<TokenService>());
        services.TryAddScoped<ITokenRevoker>(sp => sp.GetRequiredService<TokenService>());
        services.TryAddScoped<IRefreshTokenRotator>(sp => sp.GetRequiredService<TokenService>());
        services.TryAddScoped<IAuthService, AuthService>();
        services.TryAddScoped<IUserDirectory, UserDirectory>();
        services.TryAddScoped<IUserProfileService, UserProfileService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISessionRevocationNotifier, NoOpSessionRevocationNotifier>());

        services.AddScoped<IValidator<SetupRequest>, SetupRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RefreshRequest>, RefreshRequestValidator>();
        services.AddScoped<IValidator<UserPreferencesDto>, UserPreferencesDtoValidator>();
        return services;
    }
}
