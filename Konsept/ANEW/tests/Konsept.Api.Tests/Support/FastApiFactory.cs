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
/// Hızlı profil için API: veritabanı yerine bellek deposu ve ayarlanabilir veritabanı yoklaması;
/// açılış migration'ı kapalı. Gerçek PostgreSQL testleri <c>Category=Integration</c> altında ayrıdır.
/// </summary>
public sealed class FastApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Gets bellek olay deposu.</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets veritabanı yoklaması.</summary>
    public FakeDatabaseProbe Database { get; } = new();

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

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("ConnectionStrings:Konsept", "Host=127.0.0.1;Port=1;Database=unused;Username=unused");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "anewconcept-tests", "logs"));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(Database);
        });
    }
}
