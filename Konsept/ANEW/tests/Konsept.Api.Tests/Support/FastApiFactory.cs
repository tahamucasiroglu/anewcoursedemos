using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Hızlı profil için API: veritabanı yerine bellek depoları (olay ve teknik log) ve ayarlanabilir veritabanı
/// yoklaması, bellek kullanıcı/oturum depoları; açılış migration'ı kapalı; örneğe özel log klasörü, test servis anahtarı ve
/// JWT imza anahtarı. Gerçek PostgreSQL testleri
/// <c>Category=Integration</c> altında ayrıdır.
/// </summary>
public sealed class FastApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Testlerde kullanılan servis anahtarı.</summary>
    public const string ServiceKey = "test-service-key-0008";

    /// <summary>Testlerde kullanılan JWT imza anahtarı (≥ 32 bayt; bilinen sır olarak kaydedilir).</summary>
    public const string SigningKey = "test-signing-key-0002-abcdefghijklmnop";

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="FastApiFactory"/> class.</summary>
    public FastApiFactory()
    {
        Sessions = new InMemorySessionRepository(Users); // test kurulumu: rol (plan 0011) — oturum doğrulaması rolü kullanıcıdan okur.
    }

    /// <summary>Gets bellek ajan cihaz deposu (plan 0012).</summary>
    public InMemoryAgentDeviceRepository AgentDevices { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets bellek olay deposu.</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets bellek teknik log deposu.</summary>
    public InMemoryTechnicalLogRepository TechnicalLogs { get; } = new();

    /// <summary>Gets veritabanı yoklaması.</summary>
    public FakeDatabaseProbe Database { get; } = new();

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Test sunucusuna bağlı bir ajan hub bağlantısı kurar (henüz başlatılmamış).</summary>
    /// <returns>Hub bağlantısı.</returns>
    public HubConnection CreateAgentConnection() =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, AgentHubContract.HubPath.TrimStart('/')), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

    /// <summary>Gerçek kayıt + giriş uçlarıyla kimlikli (Bearer) istemci açar (default-deny, plan 0002).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstemci.</returns>
    public Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken) =>
        AuthenticatedClient.CreateAsync(this, cancellationToken);

    /// <summary>Log klasöründeki tüm log metnini okur (dosya Serilog tarafından açıkken de).</summary>
    /// <returns>Metin.</returns>
    public string ReadLogText() => ReadLogText(LogDirectory);

    /// <summary>Bir klasördeki tüm log metnini okur (dosya Serilog tarafından açıkken de).</summary>
    /// <param name="directory">Klasör.</param>
    /// <returns>Metin.</returns>
    public static string ReadLogText(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return string.Empty;
        }

        return string.Concat(Directory.GetFiles(directory, "*.log").Select(f =>
        {
            using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }));
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        try
        {
            if (Directory.Exists(LogDirectory))
            {
                Directory.Delete(LogDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Geçici klasör; silinemezse işletim sistemi temizler.
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("ConnectionStrings:Konsept", "Host=127.0.0.1;Port=1;Database=unused;Username=unused");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Logging:Directory", LogDirectory);
        builder.UseSetting("Internal:ServiceKey", ServiceKey);
        builder.UseSetting("Auth:SigningKey", SigningKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ISessionRepository>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.RemoveAll<IAgentDeviceRepository>(); // test kurulumu: ajan cihazları (plan 0012).
            services.AddSingleton<IAgentDeviceRepository>(AgentDevices);
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.RemoveAll<ITechnicalLogRepository>();
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(Database);
            services.AddSingleton<ITechnicalLogRepository>(TechnicalLogs);
        });
    }
}
