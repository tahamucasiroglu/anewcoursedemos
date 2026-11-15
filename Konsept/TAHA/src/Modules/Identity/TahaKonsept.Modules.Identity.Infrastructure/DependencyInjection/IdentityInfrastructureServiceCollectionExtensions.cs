using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Infrastructure.Localization.Concrete;
using TahaKonsept.Modules.Identity.Infrastructure.Persistence;
using TahaKonsept.Modules.Identity.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Identity.Infrastructure.Security.Concrete;

namespace TahaKonsept.Modules.Identity.Infrastructure.DependencyInjection;

/// <summary>
/// Identity altyapı kaydı: <see cref="IdentityModuleDbContext"/> (şema <c>identity</c>, snake_case, migration geçmişi şemada),
/// ASP.NET Core Identity çekirdeği (PBKDF2; parola ≥ 8, karmaşıklık zorunlu değil; 10 hatalı denemede 5 dk kilit; Türkçe harfli kullanıcı adı),
/// depolar (kullanıcı, refresh token, <c>identity.settings</c>), yönetici tohumu ayarları (<see cref="AdminSeedOptions"/>), iş birimi ve <see cref="JwtAccessTokenFactory"/>. Testler <c>configureDb</c> ile başka sağlayıcı (InMemory/SQLite) verebilir.
/// </summary>
public static class IdentityInfrastructureServiceCollectionExtensions
{
    /// <summary>Kullanıcı adında izin verilen karakterler (Türkçe harfler dahil).</summary>
    public const string AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+çğıöşüÇĞİÖŞÜ";

    /// <summary>Altyapı servislerini ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="configureDb">Opsiyonel DbContext ayarı (verilmezse PostgreSQL).</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDb = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (configureDb is null)
        {
            services.AddModuleDbContext<IdentityModuleDbContext>(configuration, IdentityModuleDbContext.SchemaName);
        }
        else
        {
            services.AddDbContext<IdentityModuleDbContext>(configureDb);
        }

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters = AllowedUserNameCharacters;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>()
            .AddErrorDescriber<TurkishIdentityErrorDescriber>();

        services.TryAddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityModuleDbContext>());
        services.TryAddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.TryAddScoped<IUserReadRepository, UserReadRepository>();
        services.TryAddScoped<IIdentitySettingRepository, IdentitySettingRepository>();
        services.TryAddSingleton(_ => ResolveAdminSeed(configuration));
        services.TryAddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();
        return services;
    }

    /// <summary>
    /// Yönetici tohumu ayarlarını okur: önce yapılandırma, yoksa ortam değişkeni (geliştirmede <c>.env</c> yapılandırma kurulduktan sonra
    /// ortama yüklendiği için ortam da ayrıca okunur).
    /// </summary>
    private static AdminSeedOptions ResolveAdminSeed(IConfiguration configuration) => new()
    {
        Email = Read(configuration, AdminSeedOptions.EmailKey),
        PasswordHash = Read(configuration, AdminSeedOptions.PasswordHashKey),
    };

    private static string? Read(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? Environment.GetEnvironmentVariable(key) : value;
    }
}
