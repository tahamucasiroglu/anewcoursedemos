using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Gerçek PostgreSQL 17'ye bağlı API (full profil): her örnek temiz bir test veritabanıyla açılır,
/// açılış migration'ı açıktır (monitoring + identity). Veritabanı fabrika kapanınca silinir. Kimlikli istemci için
/// <see cref="AuthenticatedClient"/> (servis anahtarı ve imza anahtarı ayarlı).
/// </summary>
public sealed class IntegrationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Gets test veritabanı.</summary>
    public PostgresTestDatabase Database { get; } = new();

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await Database.InitializeAsync().ConfigureAwait(false);

    /// <summary>Test sunucusuna bağlı bir ajan hub bağlantısı kurar (henüz başlatılmamış).</summary>
    /// <param name="agentToken">Eşleştirmeyle alınan ajan oturum belirteci (plan 0012; hub kimliksiz bağlantıyı reddeder).</param>
    /// <returns>Hub bağlantısı.</returns>
    public HubConnection CreateAgentConnection(string? agentToken) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, AgentHubContract.HubPath.TrimStart('/')), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult(agentToken);
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
        builder.UseSetting("Internal:ServiceKey", FastApiFactory.ServiceKey);
        builder.UseSetting("Auth:SigningKey", FastApiFactory.SigningKey);

        // Gerçek veritabanı testleri depoları ve uçları doğrular; üretim hattı (0017) kendi hızlı testlerinde sahte ajan/sağlayıcıyla koşar
        // (plan 0017 A-1). Hat açık olsa Devam et işi gerçek kapıya götürür ve 0006 testinin beklediği Durdur yarışa girerdi.
        builder.UseSetting("Production:Pipeline:Enabled", "false");
    }
}
