using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AiGateway.Application.Abstract;
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
/// Konsept ayarları testleri için hızlı API (plan 0005): bellek kullanıcı/oturum/olay/teknik log, profil, konsept ayarı ve API key
/// depoları; sahte key doğrulaması (gerçek sağlayıcı çağrısı yok); örneğe özel Data Protection anahtar ve log klasörü. İstenirse
/// kullanıcı rolünün model yetkisi (<c>Identity:ModelPermissions:User</c>) ilk istekten önce kısıtlanır.
/// </summary>
public sealed class ConceptSettingsApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Initializes a new instance of the <see cref="ConceptSettingsApiFactory"/> class.</summary>
    public ConceptSettingsApiFactory()
    {
        Sessions = new InMemorySessionRepository(Users);
    }

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets bellek profil deposu.</summary>
    public InMemoryProfileRepository Profiles { get; } = new();

    /// <summary>Gets bellek konsept ayarı deposu ("veritabanı").</summary>
    public InMemoryConceptSettingsRepository Settings { get; } = new();

    /// <summary>Gets bellek API key deposu.</summary>
    public InMemoryApiKeyRepository Keys { get; } = new();

    /// <summary>Gets or sets kullanıcı rolünün model listesi (virgüllü); <c>null</c> → tümü.</summary>
    public string? UserModelPermissions { get; set; }

    /// <summary>Gets bu örneğin Data Protection anahtar klasörü.</summary>
    public string KeysDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "dp-keys-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "concept-logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Yeni kullanıcı kaydeder ve Bearer istemci döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstemci.</returns>
    public Task<HttpClient> CreateUserClientAsync(CancellationToken cancellationToken) =>
        AuthenticatedClient.CreateAsync(this, cancellationToken);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        foreach (var directory in new[] { LogDirectory, KeysDirectory })
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Geçici klasör; silinemezse işletim sistemi temizler.
            }
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
        builder.UseSetting("DataProtection:KeysDirectory", KeysDirectory);
        if (UserModelPermissions is { } models)
        {
            builder.UseSetting("Identity:ModelPermissions:User", models);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ISessionRepository>();
            services.RemoveAll<IAgentDeviceRepository>();
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.RemoveAll<ITechnicalLogRepository>();
            services.RemoveAll<IProfileRepository>();
            services.RemoveAll<IConceptSettingsRepository>();
            services.RemoveAll<IApiKeyRepository>();
            services.RemoveAll<IProviderKeyValidator>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.AddSingleton<IAgentDeviceRepository>(new InMemoryAgentDeviceRepository());
            services.AddSingleton<ISystemEventRepository>(new InMemorySystemEventRepository());
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(new InMemoryTechnicalLogRepository());
            services.AddSingleton<IProfileRepository>(Profiles);
            services.AddSingleton<IConceptSettingsRepository>(Settings);
            services.AddSingleton<IApiKeyRepository>(Keys);
            services.AddSingleton<IProviderKeyValidator>(new FakeProviderKeyValidator());
            services.RemoveAll<IConceptRepository>(); // test kurulumu: konsept deposu (plan 0014 A-1; profil silme kuralı okur).
            services.AddSingleton<IConceptRepository>(new InMemoryConceptRepository());
        });
    }
}
