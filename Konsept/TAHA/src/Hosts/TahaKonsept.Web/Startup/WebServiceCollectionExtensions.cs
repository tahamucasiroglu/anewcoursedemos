using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Api.Concrete;
using TahaKonsept.Web.Infrastructure.Auth;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Concrete;
using TahaKonsept.Web.Infrastructure.Options;
using TahaKonsept.Web.Services.Account.Abstract;
using TahaKonsept.Web.Services.Account.Concrete;
using TahaKonsept.Web.Services.Downloads.Abstract;
using TahaKonsept.Web.Services.Downloads.Concrete;
using TahaKonsept.Web.Services.Onboarding.Abstract;
using TahaKonsept.Web.Services.Onboarding.Concrete;

namespace TahaKonsept.Web.Startup;

/// <summary>
/// Web host'un DI kaydı. <see cref="AddTahaWeb"/>: MVC (global antiforgery + varsayılan olarak giriş zorunlu),
/// cookie kimlik doğrulama (BFF; Ajax isteklerinde yönlendirme yerine 401/403), admin policy, Data Protection anahtarları
/// (yeniden başlatmada oturumlar düşmesin), API HttpClient'ları, token deposu/yenileyici, oturum servisi, Başlangıç kontrol listesi ve masaüstü programı sürüm bilgisi.
/// </summary>
public static class WebServiceCollectionExtensions
{
    /// <summary>Antiforgery başlık adı (wwwroot/js/core/api.js bunu meta etiketinden okur).</summary>
    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";

    /// <summary>Tüm Web servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="environment">Ortam (içerik kökü için).</param>
    public static IServiceCollection AddTahaWeb(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryHeaderName;
            options.Cookie.Name = ".TahaKonsept.Csrf";
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        var keysDirectory = configuration["DataProtection:KeysDirectory"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "keys", "dataprotection");
        services.AddDataProtection()
            .SetApplicationName("TahaKonsept.Web")
            .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));

        AddAuthentication(services);
        AddApiClients(services);

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddSingleton<IAgentReleaseProvider, WebRootAgentReleaseProvider>();
        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = AuthTokenNames.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/Login";
                options.ReturnUrlParameter = "returnUrl";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context => AjaxAware(context, StatusCodes.Status401Unauthorized),
                    OnRedirectToAccessDenied = context => AjaxAware(context, StatusCodes.Status403Forbidden),
                    OnValidatePrincipal = RejectExpiredSession,
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthTokenNames.AdminPolicy, policy => policy.RequireAssertion(context =>
                context.User.HasClaim(AuthTokenNames.PermissionClaim, AuthTokenNames.AdminPermission) || context.User.IsInRole("Admin")));
    }

    private static void AddApiClients(IServiceCollection services)
    {
        services.AddSingleton<IPrincipalFactory, JwtPrincipalFactory>();
        services.AddSingleton<ITokenStore, CookieTokenStore>();
        services.AddSingleton<ITokenRefresher, ApiTokenRefresher>();

        services.AddHttpClient(ApiTokenRefresher.HttpClientName, ConfigureApiClient);
        services.AddHttpClient<IApiClient, ApiClient>(ConfigureApiClient);
    }

    private static void ConfigureApiClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<ApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TahaKonsept.Web");
    }

    private static Task AjaxAware(RedirectContext<CookieAuthenticationOptions> context, int ajaxStatus)
    {
        var request = context.Request;
        var isAjax = string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
        if (isAjax)
        {
            context.Response.StatusCode = ajaxStatus;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    private static async Task RejectExpiredSession(CookieValidatePrincipalContext context)
    {
        var refreshExpires = context.Properties.GetTokenValue(AuthTokenNames.RefreshTokenExpiresAt);
        if (DateTimeOffset.TryParse(refreshExpires, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var expires)
            && expires != DateTimeOffset.MinValue
            && expires <= DateTimeOffset.UtcNow)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        if (context.Principal?.FindFirst(ClaimTypes.Name) is null)
        {
            context.RejectPrincipal();
        }
    }
}
