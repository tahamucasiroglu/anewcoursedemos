using Konsept.Api.Tests.Fakes;
using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Gerçek Kestrel üzerinde, sabit loopback portunda API (veritabanı yerine bellek deposu). Ajanın
/// gerçek SignalR istemcisi buna ağ üzerinden bağlanır; aynı portta yeniden açılarak "API yeniden başladı"
/// durumu kurulur.
/// </summary>
public sealed class KestrelApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Initializes a new instance of the <see cref="KestrelApiFactory"/> class.</summary>
    /// <param name="port">Loopback portu.</param>
    public KestrelApiFactory(int port)
    {
        UseKestrel(port);
    }

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
            services.AddSingleton<ISystemEventRepository>(new InMemorySystemEventRepository());
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
        });
    }
}
