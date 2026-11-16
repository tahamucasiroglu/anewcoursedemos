using Konsept.Api.Tests.Fakes;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Profiles.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Profiles testleri için hızlı API (plan 0003): bellek kullanıcı/oturum/ajan/olay/teknik log/profil depoları. Gerçek PostgreSQL testleri
/// <c>ProfilesIntegrationTests</c>'te.
/// </summary>
public sealed class ProfilesApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Initializes a new instance of the <see cref="ProfilesApiFactory"/> class.</summary>
    public ProfilesApiFactory()
    {
        Sessions = new InMemorySessionRepository(Users);
    }

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets bellek profil deposu ("veritabanı").</summary>
    public InMemoryProfileRepository Profiles { get; } = new();

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "profile-logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Yeni kullanıcı kaydeder ve Bearer istemci döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="email">E-posta (verilmezse benzersiz).</param>
    /// <returns>İstemci.</returns>
    public Task<HttpClient> CreateUserClientAsync(CancellationToken cancellationToken, string? email = null) =>
        AuthenticatedClient.CreateAsync(this, cancellationToken, email);

    /// <summary>Log klasöründeki tüm log metni.</summary>
    /// <returns>Metin.</returns>
    public string ReadLogText() => FastApiFactory.ReadLogText(LogDirectory);

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
        builder.UseSetting("Internal:ServiceKey", FastApiFactory.ServiceKey);
        builder.UseSetting("Auth:SigningKey", FastApiFactory.SigningKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ISessionRepository>();
            services.RemoveAll<IAgentDeviceRepository>();
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.RemoveAll<ITechnicalLogRepository>();
            services.RemoveAll<IProfileRepository>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.AddSingleton<IAgentDeviceRepository>(new InMemoryAgentDeviceRepository());
            services.AddSingleton<ISystemEventRepository>(new InMemorySystemEventRepository());
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(new InMemoryTechnicalLogRepository());
            services.AddSingleton<IProfileRepository>(Profiles);
            services.RemoveAll<IConceptRepository>(); // test kurulumu: konsept deposu (plan 0014 A-1; profil silme kuralı okur).
            services.AddSingleton<IConceptRepository>(new InMemoryConceptRepository());
        });
    }
}
