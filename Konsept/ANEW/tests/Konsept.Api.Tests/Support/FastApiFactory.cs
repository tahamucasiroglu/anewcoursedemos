using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AgentGateway.Contracts;
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
/// yoklaması; açılış migration'ı kapalı; örneğe özel log klasörü ve test servis anahtarı. Gerçek PostgreSQL testleri
/// <c>Category=Integration</c> altında ayrıdır.
/// </summary>
public sealed class FastApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Testlerde kullanılan servis anahtarı.</summary>
    public const string ServiceKey = "test-service-key-0008";

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

    /// <summary>Log klasöründeki tüm log metnini okur (dosya Serilog tarafından açıkken de).</summary>
    /// <returns>Metin.</returns>
    public string ReadLogText()
    {
        if (!Directory.Exists(LogDirectory))
        {
            return string.Empty;
        }

        return string.Concat(Directory.GetFiles(LogDirectory, "*.log").Select(f =>
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
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.RemoveAll<ITechnicalLogRepository>();
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(Database);
            services.AddSingleton<ITechnicalLogRepository>(TechnicalLogs);
        });
    }
}
