using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Gerçek PostgreSQL 17'ye bağlı API (full profil): her örnek temiz bir test veritabanıyla açılır,
/// açılış migration'ı açıktır. Veritabanı fabrika kapanınca silinir.
/// </summary>
public sealed class IntegrationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Gets test veritabanı.</summary>
    public PostgresTestDatabase Database { get; } = new();

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await Database.InitializeAsync().ConfigureAwait(false);

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
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        await Database.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("ConnectionStrings:Konsept", Database.ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "anewconcept-tests", "logs"));
    }
}
