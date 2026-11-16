using System.Net.Http.Headers;
using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.Identity.Application.Abstract;
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
/// Default-deny (plan 0002): fabrikanın açtığı her istemci, gerçek kayıt + giriş uçlarıyla bir kez alınan Bearer token'ı
/// taşır (ajan hub'ı kimliksizdir, 0012'ye kadar).
/// </summary>
public sealed class KestrelApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _databaseDown;
    private readonly FlakyRegistrationState? _flakyRegistration;
    private readonly string? _databaseConnectionString;
    private readonly Lock _tokenGate = new();
    private string? _accessToken;

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
    protected override void ConfigureClient(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken(client));
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Açık SignalR bağlantıları varken host varsayılan 30 sn kapanma süresini beklemesin.
        builder.UseShutdownTimeout(TimeSpan.FromSeconds(2));
        builder.UseSetting("ConnectionStrings:Konsept", _databaseConnectionString ?? "Host=127.0.0.1;Port=1;Database=unused;Username=unused");
        builder.UseSetting("Database:MigrateOnStartup", _databaseConnectionString is null ? "false" : "true");
        builder.UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "anewconcept-tests", "logs"));
        builder.UseSetting("Internal:ServiceKey", FastApiFactory.ServiceKey);
        builder.UseSetting("Auth:SigningKey", FastApiFactory.SigningKey);
        builder.ConfigureServices(services =>
        {
            if (_databaseConnectionString is not null)
            {
                return;
            }

            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ISessionRepository>();
            var users = new InMemoryUserRepository();
            services.AddSingleton<IUserRepository>(users);
            services.AddSingleton<ISessionRepository>(new InMemorySessionRepository(users));
            services.RemoveAll<IAgentDeviceRepository>(); // test kurulumu: ajan cihazları (plan 0012).
            services.AddSingleton<IAgentDeviceRepository>(new InMemoryAgentDeviceRepository());
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

    // Token fabrika başına bir kez alınır (istemci her yoklamada yeniden açılıyor); ConfigureClient eşzamanlıdır.
    private string AccessToken(HttpClient client)
    {
        lock (_tokenGate)
        {
            if (_accessToken is null)
            {
                using var anonymous = new HttpClient { BaseAddress = client.BaseAddress };
                var email = AuthenticatedClient.NewEmail();
                AuthenticatedClient.RegisterAsync(anonymous, Services, email, AuthenticatedClient.Password, "Ajan Testi", CancellationToken.None)
                    .GetAwaiter().GetResult();
                _accessToken = AuthenticatedClient.LoginAsync(anonymous, Services, email, AuthenticatedClient.Password, CancellationToken.None)
                    .GetAwaiter().GetResult().GetProperty("accessToken").GetString();
            }

            return _accessToken!;
        }
    }
}
