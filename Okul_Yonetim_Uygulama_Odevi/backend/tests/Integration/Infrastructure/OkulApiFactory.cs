using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.WebAPI;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Testler için API: verilen veritabanı bağlantısı, testlere özel log klasörü ve CORS ayarıyla kurulur.
/// İsteğe bağlı olarak "/__test/throw" yolunda kasıtlı beklenmeyen hata üreten bir uç ekler (AC-13 için).
/// </summary>
public sealed class OkulApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString;
    private readonly bool addThrowingEndpoint;

    public OkulApiFactory(string connectionString, bool addThrowingEndpoint = false)
    {
        this.connectionString = connectionString;
        this.addThrowingEndpoint = addThrowingEndpoint;
        this.LogFolder = Path.Combine(Path.GetTempPath(), "okul-api-test-logs-" + Guid.NewGuid().ToString("N"));
    }

    public string LogFolder { get; }

    public string ReadLog()
    {
        string[] files = Directory.Exists(this.LogFolder) ? Directory.GetFiles(this.LogFolder, "okul-api-*.log") : [];
        return string.Concat(files.Select(ReadShared));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:OkulDb", this.connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("FileLogging:Folder", this.LogFolder);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        if (this.addThrowingEndpoint)
        {
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ThrowingEndpointStartupFilter>());
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(this.LogFolder))
        {
            Directory.Delete(this.LogFolder, recursive: true);
        }
    }

    private static string ReadShared(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
