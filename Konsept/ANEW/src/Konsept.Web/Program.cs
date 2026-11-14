using Konsept.Shared.Logging;
using Konsept.Shared.Logging.Concrete;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Auth;
using Konsept.Web.Auth.Abstract;
using Konsept.Web.Auth.Concrete;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

KonseptLogging.RegisterKnownSecrets(builder.Configuration.AsEnumerable()); // Servis anahtarı vb. logda hiçbir biçimde geçmez.
var logDirectory = builder.Configuration["Logging:Directory"] ?? Path.Combine(AppContext.BaseDirectory, "logs");
builder.Services.AddSingleton(TimeProvider.System);
var logToConsole = builder.Configuration.GetValue("Logging:Console", defaultValue: true);
builder.Services.AddSerilog(
    (services, logger) =>
    {
        logger
            .ReadFrom.Configuration(builder.Configuration)
            .WithKonseptEnrichment(LogSources.Web)
            .WriteToKonseptFile(logDirectory, "web")
            .WriteToApiForwarding(services);

        // Pi'de konsol journald'a gider (review M-4); üretimde Logging__Console=false.
        if (logToConsole)
        {
            logger.WriteTo.Console(KonseptLogging.CreateFormatter());
        }
    },
    preserveStaticLogger: true);
builder.Services.AddSingleton(services =>
    new LogFileCleanupScheduler(new LogFileJanitor(services.GetRequiredService<TimeProvider>()), logDirectory, services.GetRequiredService<TimeProvider>()));

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl ayarı eksik.");
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAccessTokenSource, HttpContextAccessTokenSource>();
builder.Services.AddKonseptApiClient(new Uri(apiBaseUrl, UriKind.Absolute), builder.Configuration["Internal:ServiceKey"]);
builder.Services.AddSingleton<ITokenRefreshCoordinator, TokenRefreshCoordinator>();
builder.Services.AddScoped<IAuthPageService, AuthPageService>();
builder.Services.AddScoped<KonseptCookieEvents>();

// Oturum cookie'si (spec 0002 Req 13; docs/security.md): token'lar Data Protection ile şifreli bilette, HttpOnly; 14 gün
// kalıcı ve kayan. HTTPS'e geçince Auth:SecureCookie=true (B-3). Anahtar klasörü ayarlıysa anahtarlar orada kalıcıdır.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("anewconcept-web");
var keysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrEmpty(keysDirectory))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = KonseptCookieEvents.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue("Auth:SecureCookie", defaultValue: false)
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/giris";
        options.LogoutPath = "/cikis";
        options.AccessDeniedPath = "/giris";
        options.ReturnUrlParameter = "returnUrl";
        options.EventsType = typeof(KonseptCookieEvents);
    });

// Default-deny (spec 0002 Req 11): yalnızca [AllowAnonymous] işaretli giriş/kayıt sayfaları kimliksiz açılır.
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseMiddleware<CorrelationLogMiddleware>();
app.UseExceptionHandler("/hata");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Services.GetRequiredService<LogFileCleanupScheduler>().Start();

await app.RunAsync();
