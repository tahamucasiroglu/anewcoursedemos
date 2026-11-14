using Konsept.Shared.Logging;
using Konsept.Shared.Logging.Concrete;
using Konsept.Web.ApiClient;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddKonseptApiClient(new Uri(apiBaseUrl, UriKind.Absolute), builder.Configuration["Internal:ServiceKey"]);
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseMiddleware<CorrelationLogMiddleware>();
app.UseExceptionHandler("/hata");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Services.GetRequiredService<LogFileCleanupScheduler>().Start();

await app.RunAsync();
