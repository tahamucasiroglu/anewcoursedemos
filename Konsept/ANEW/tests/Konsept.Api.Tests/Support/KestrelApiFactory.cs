using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Gerçek Kestrel üzerinde, sabit loopback portunda API (veritabanı yerine bellek deposu). Ajanın
/// gerçek SignalR istemcisi buna ağ üzerinden bağlanır; aynı portta yeniden açılarak "API yeniden başladı"
/// durumu kurulur. İstenirse veritabanı kapalı gibi davranır ya da gerçek bir PostgreSQL test veritabanına bağlanır.
/// </summary>
public sealed class KestrelApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _databaseDown;
    private readonly FlakyRegistrationState? _flakyRegistration;
    private readonly string? _databaseConnectionString;

    /// <summary>Initializes a new instance of the <see cref="KestrelApiFactory"/> class.</summary>
    /// <param name="port">Loopback portu.</param>
    /// <param name="databaseDown">Veritabanı kapalı gibi davransın mı.</param>
    /// <param name="flakyRegistration">Verilirse ilk kayıt istekleri reddedilir.</param>
    /// <param name="databaseConnectionString">Verilirse bellek depoları yerine bu gerçek veritabanı kullanılır (full profil).</param>
    public KestrelApiFactory(int port, bool databaseDown = false, FlakyRegistrationState? flakyRegistration = null, string? databaseConnectionString = null)
    {
        _databaseDown = databaseDown;
        _flakyRegistration = flakyRegistration;
        _databaseConnectionString = databaseConnectionString;
        UseKestrel(port);
    }

    /// <summary>Gets bellek teknik log deposu (gerçek veritabanı kullanılmıyorsa).</summary>
    public InMemoryTechnicalLogRepository TechnicalLogs { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Açık SignalR bağlantıları varken host varsayılan 30 sn kapanma süresini beklemesin.
        builder.UseShutdownTimeout(TimeSpan.FromSeconds(2));
        builder.UseSetting("ConnectionStrings:Konsept", _databaseConnectionString ?? "Host=127.0.0.1;Port=1;Database=unused;Username=unused");
        builder.UseSetting("Database:MigrateOnStartup", _databaseConnectionString is null ? "false" : "true");
        builder.UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "anewconcept-tests", "logs"));
        builder.ConfigureServices(services =>
        {
            if (_databaseConnectionString is not null)
            {
                return;
            }

            services.RemoveAll<ITechnicalLogRepository>();
            services.AddSingleton<ITechnicalLogRepository>(TechnicalLogs);
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.AddSingleton<ISystemEventRepository>(_databaseDown ? new FailingSystemEventRepository() : new InMemorySystemEventRepository());
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe { Available = !_databaseDown });
            if (_flakyRegistration is not null)
            {
                services.AddSingleton(_flakyRegistration);
                services.AddScoped<AgentPresenceService>();
                services.RemoveAll<IAgentPresenceService>();
                services.AddScoped<IAgentPresenceService>(sp =>
                    new FlakyPresenceService(sp.GetRequiredService<AgentPresenceService>(), sp.GetRequiredService<FlakyRegistrationState>()));
            }
        });
    }
}
