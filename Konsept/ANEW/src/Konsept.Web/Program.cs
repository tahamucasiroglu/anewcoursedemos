using Konsept.Web.ApiClient;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = builder.Configuration["Logging:Directory"] ?? Path.Combine(AppContext.BaseDirectory, "logs");
builder.Services.AddSerilog((_, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.File(
        Path.Combine(logDirectory, "web-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        formatProvider: System.Globalization.CultureInfo.InvariantCulture));

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl ayarı eksik.");
builder.Services.AddKonseptApiClient(new Uri(apiBaseUrl, UriKind.Absolute));
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseExceptionHandler("/hata");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

await app.RunAsync();
