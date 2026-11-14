using Serilog;
using TahaKonsept.BuildingBlocks.Infrastructure.Logging;
using TahaKonsept.Web.Startup;

namespace TahaKonsept.Web;

/// <summary>
/// Taha Konsept Web (MVC, BFF) giriş noktası. Kestrel <c>http://0.0.0.0:7110</c> (appsettings Kestrel bölümü),
/// Serilog (günlük dönen dosya, 7 gün; <see cref="SerilogSetup"/>), servisler <see cref="WebServiceCollectionExtensions.AddTahaWeb"/>,
/// ara katmanlar <see cref="WebApplicationExtensions.UseTahaWeb"/>. Entegrasyon testleri için <c>public partial</c>.
/// </summary>
public partial class Program
{
    /// <summary>Uygulamayı başlatır.</summary>
    /// <param name="args">Komut satırı argümanları.</param>
    public static async Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        Log.Logger = SerilogSetup.CreateBootstrapLogger("web", SerilogSetup.ResolveLogDirectory(builder.Configuration, builder.Environment.ContentRootPath));

        try
        {
            builder.Host.UseTahaSerilog("web");
            builder.Services.AddTahaWeb(builder.Configuration, builder.Environment);

            var app = builder.Build();
            app.UseTahaWeb();

            Log.Information("Taha Konsept Web başlıyor (API: {ApiBaseUrl}).", builder.Configuration["Api:BaseUrl"]);
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Taha Konsept Web beklenmedik şekilde durdu.");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
