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

        // Tarayıcı cookie'yi biletten uzun tutar (review R1-2): 14 gün boşta kalan oturumda sunucu süresi dolmuş bileti görür
        // ve girişe "Oturumunuz sona erdi" bilgisiyle yönlendirir; bilet süresi yine 14 gündür.
        options.Cookie.MaxAge = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/giris";
        options.LogoutPath = "/cikis";
        options.AccessDeniedPath = WebPolicies.ForbiddenPath;
        options.ReturnUrlParameter = "returnUrl";
        options.EventsType = typeof(KonseptCookieEvents);
    });

// Default-deny (spec 0002 Req 11): yalnızca [AllowAnonymous] işaretli giriş/kayıt sayfaları kimliksiz açılır.
// Yetki politikası (spec 0011): yetki claim'i her istekte Api'nin oturum yanıtından kurulur; asıl karar Api'dedir.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(WebPolicies.AdminPanel, policy => policy.RequireAuthenticatedUser().RequireClaim(WebPolicies.PermissionClaim, WebPolicies.AdminPanel));
});
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseMiddleware<CorrelationLogMiddleware>();
app.UseExceptionHandler("/hata");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();

// Ajan bileşen aynası (spec 0009 Req 11–12): herkese açık üçüncü taraf arşivleri (ör. FFmpeg) Pi'de; ajan resmi kaynağa
// ulaşamazsa buradan indirir ve yine gömülü SHA-256 ile doğrular. Kimliksiz statik dosya yolu (uç değil); yalnızca bu klasör.
var agentDirectory = builder.Configuration["AgentDownload:Directory"];
var mirrorDirectory = string.IsNullOrEmpty(agentDirectory) ? null : Path.Combine(agentDirectory, "components");
if (mirrorDirectory is not null && Directory.Exists(mirrorDirectory))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mirrorDirectory),
        RequestPath = "/ajan-bilesenleri",
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream",
    });
}

// Ajan güncellemesi (spec 0023 Req 6): imzalı sürüm listesi (manifest.json + .sig) ve ajan exe'si; kimliksiz statik yol, yalnızca bu klasör.
// Güven imzadadır (Ed25519, açık anahtar ajana gömülü) — sunucu ya da ağ değiştirse de ajan doğrulamadan geçmeyen paketi kurmaz.
var updateDirectory = string.IsNullOrEmpty(agentDirectory) ? null : Path.Combine(agentDirectory, "update");
if (updateDirectory is not null && Directory.Exists(updateDirectory))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(updateDirectory),
        RequestPath = "/ajan-guncelleme",
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream",
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Services.GetRequiredService<LogFileCleanupScheduler>().Start();

await app.RunAsync();
