using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;

namespace TahaKonsept.Web.Startup;

/// <summary>
/// Web host'un HTTP ara katman sırası. <see cref="UseTahaWeb"/>: hata sayfası, durum kodu sayfaları, tr-TR kültürü,
/// statik dosyalar (ajan güncellemesi için <c>.nupkg</c> eşlemesi dahil), Serilog istek günlüğü, istek sınırlayıcı (giriş/kayıt), kimlik doğrulama/yetkilendirme ve rotalar (Admin alanı + varsayılan
/// <c>{controller=Konseptler}/{action=Index}/{id?}</c>).
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>Ara katmanları ve rotaları bağlar.</summary>
    /// <param name="app">Web uygulaması.</param>
    public static WebApplication UseTahaWeb(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Hata");
        }

        app.UseStatusCodePagesWithReExecute("/Hata", "?code={0}");

        var turkish = new CultureInfo("tr-TR");
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(turkish),
            SupportedCultures = [turkish],
            SupportedUICultures = [turkish],
        });

        // Masaüstü ajan güncellemeleri (Velopack) /downloads/ altındaki .nupkg paketlerini indirir; varsayılan eşlemede yok.
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".nupkg"] = "application/octet-stream";
        app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypes });
        app.UseSerilogRequestLogging(options =>
            options.GetLevel = (context, _, ex) => ex is not null || context.Response.StatusCode >= 500
                ? Serilog.Events.LogEventLevel.Error
                : context.Request.Path.StartsWithSegments("/Gunluk") ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Information);

        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
        app.MapControllerRoute("default", "{controller=Konseptler}/{action=Index}/{id?}");
        return app;
    }
}
