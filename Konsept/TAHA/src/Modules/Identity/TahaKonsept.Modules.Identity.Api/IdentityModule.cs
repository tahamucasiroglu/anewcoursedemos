using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Identity.Api.RateLimiting;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.DependencyInjection;
using TahaKonsept.Modules.Identity.Infrastructure.DependencyInjection;
using TahaKonsept.Modules.Identity.Infrastructure.Persistence;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Api;

/// <summary>
/// Identity modülü kaydı: altyapı (DbContext, ASP.NET Core Identity, JWT üretimi), uygulama servisleri, doğrulayıcılar,
/// her izin için aynı adlı authorization policy (<c>[Authorize(Policy = PermissionNames.AdminAccess)]</c>;
/// <c>permission</c> claim'i ya da <see cref="RoleNames.Admin"/> rolü yeter) ve kayıt ucu istek sınırı (<see cref="IdentityRateLimitPolicies"/>).
/// Migration: <see cref="IdentityModuleDbContext"/>; ardından yönetici tohumu (<see cref="IAdminSeeder"/>).
/// </summary>
public sealed class IdentityModule : ModuleBase
{
    /// <summary>Modül adı.</summary>
    public override string Name => "Identity";

    /// <summary>Servisleri kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityInfrastructure(configuration);
        services.AddIdentityApplication();
        services.AddRateLimiter(options => IdentityRateLimitPolicies.Register(options, configuration));

        var authorization = services.AddAuthorizationBuilder();
        foreach (var permission in PermissionNames.All)
        {
            authorization.AddPolicy(permission, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.HasClaim(CustomClaimTypes.Permission, permission) || context.User.HasClaim("role", RoleNames.Admin)));
        }
    }

    /// <summary>Identity migration'larını uygular, ardından yönetici tohumunu (idempotent) çalıştırır.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await MigrateDbContextAsync<IdentityModuleDbContext>(services, cancellationToken);
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAdminSeeder>().SeedAsync(cancellationToken);
    }
}
