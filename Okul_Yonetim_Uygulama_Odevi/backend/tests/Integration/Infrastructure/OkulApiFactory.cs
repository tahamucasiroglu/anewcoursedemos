using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.WebAPI;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Testler için API: verilen veritabanı bağlantısı, testlere özel log klasörü, CORS ayarı ve test imza anahtarıyla
/// kurulur. İsteğe bağlı: "/__test/throw" kasıtlı hata ucu (spec 0001 AC-13), sahte saat (spec 0004 kilit/oturum süreleri),
/// giriş hız sınırı, servis değiştirme (ör. eşzamanlılık testinde bekleten şifre doğrulayıcı) ve istemci IP'si (her isteğe yerel vekil adresi verilir; X-Forwarded-For başlığı gerçek IP'yi taşır).
/// </summary>
public sealed class OkulApiFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "okul-test-imza-anahtari-en-az-32-bayt-0123456789";

    /// <summary>Testlere özel iki adımlı doğrulama şifreleme anahtarı (32 bayt; spec 0007). Gerçek ortamda kullanılmaz.</summary>
    public const string TestTwoFactorKey = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=";

    private readonly string connectionString;
    private readonly bool addThrowingEndpoint;
    private readonly TimeProvider? timeProvider;
    private readonly int loginRateLimitPerMinute;
    private readonly Action<IServiceCollection>? configureServices;
    private readonly string? twoFactorKey;

    public OkulApiFactory(
        string connectionString,
        bool addThrowingEndpoint = false,
        TimeProvider? timeProvider = null,
        int loginRateLimitPerMinute = 1000,
        Action<IServiceCollection>? configureServices = null,
        string? twoFactorKey = TestTwoFactorKey)
    {
        this.configureServices = configureServices;
        this.twoFactorKey = twoFactorKey;
        this.connectionString = connectionString;
        this.addThrowingEndpoint = addThrowingEndpoint;
        this.timeProvider = timeProvider;
        this.loginRateLimitPerMinute = loginRateLimitPerMinute;
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
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        if (this.twoFactorKey is not null)
        {
            builder.UseSetting("TwoFactor:EncryptionKey", this.twoFactorKey);
        }

        builder.UseSetting("Auth:LoginRateLimitPerMinute", this.loginRateLimitPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, LoopbackRemoteAddressStartupFilter>();
            if (this.timeProvider is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(this.timeProvider);
            }

            if (this.addThrowingEndpoint)
            {
                services.AddSingleton<IStartupFilter, ThrowingEndpointStartupFilter>();
            }

            this.configureServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            DeleteFolder(this.LogFolder);
        }
    }

    // Log dosyası kapandıktan hemen sonra Windows (ör. virüs tarayıcı) dosyayı kısa süre tutabilir; temizlik bu geçici
    // kilidi en fazla ~5 sn bekler. Test doğrulamalarına dokunmaz; yalnızca geride klasör kalmamasını sağlar.
    private static void DeleteFolder(string folder)
    {
        for (int attempt = 1; Directory.Exists(folder); attempt++)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException) when (attempt < 50)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 50)
            {
                Thread.Sleep(100);
            }
        }
    }

    private static string ReadShared(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
