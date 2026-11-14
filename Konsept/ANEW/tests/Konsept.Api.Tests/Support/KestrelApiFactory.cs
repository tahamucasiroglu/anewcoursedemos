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
/// durumu kurulur. İstenirse veritabanı kapalı gibi davranır.
/// </summary>
public sealed class KestrelApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _databaseDown;

    /// <summary>Initializes a new instance of the <see cref="KestrelApiFactory"/> class.</summary>
    /// <param name="port">Loopback portu.</param>
    /// <param name="databaseDown">Veritabanı kapalı gibi davransın mı.</param>
    public KestrelApiFactory(int port, bool databaseDown = false)
    {
        _databaseDown = databaseDown;
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
            services.AddSingleton<ISystemEventRepository>(_databaseDown ? new FailingSystemEventRepository() : new InMemorySystemEventRepository());
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe { Available = !_databaseDown });
        });
    }
}
