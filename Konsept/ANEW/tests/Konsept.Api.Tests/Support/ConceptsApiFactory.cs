using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Profiles.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Konsept üretimi testleri için hızlı API (plan 0014): bellek kullanıcı/oturum/olay/teknik log, profil, konsept ayarı, API key,
/// harcama, konsept ve öneri depoları; sahte key doğrulaması ve isteğe göre yanıt veren sahte sağlayıcı (gerçek LLM çağrısı yok).
/// Çağrı kapısı (0013) ve tüm Concepts servisleri gerçektir.
/// </summary>
public sealed class ConceptsApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Initializes a new instance of the <see cref="ConceptsApiFactory"/> class.</summary>
    public ConceptsApiFactory()
    {
        Sessions = new InMemorySessionRepository(Users);
    }

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets bellek profil deposu.</summary>
    public InMemoryProfileRepository Profiles { get; } = new();

    /// <summary>Gets bellek konsept ayarı deposu.</summary>
    public InMemoryConceptSettingsRepository Settings { get; } = new();

    /// <summary>Gets bellek API key deposu.</summary>
    public InMemoryApiKeyRepository Keys { get; } = new();

    /// <summary>Gets bellek harcama deposu.</summary>
    public InMemorySpendRepository Spend { get; } = new();

    /// <summary>Gets bellek olay deposu (Günlük).</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets bellek konsept deposu.</summary>
    public InMemoryConceptRepository Concepts { get; } = new();

    /// <summary>Gets bellek öneri deposu.</summary>
    public InMemoryTopicSuggestionRepository Suggestions { get; } = new();

    /// <summary>Gets sahte sağlayıcı.</summary>
    public ScriptedProviderClient Provider { get; } = new();

    /// <summary>Gets bellek video kayıtları deposu (spec 0019: Silinenler'deki videolar negatif sinyal).</summary>
    public InMemoryProducedVideoRepository Videos { get; } = new();

    /// <summary>Gets bu örneğin Data Protection anahtar klasörü.</summary>
    public string KeysDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "dp-keys-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "concepts-logs-" + Guid.NewGuid().ToString("N"));

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
            services.RemoveAll<IProviderClient>();
            services.RemoveAll<ISpendRepository>();
            services.RemoveAll<IConceptRepository>();
            services.RemoveAll<ITopicSuggestionRepository>();
            services.RemoveAll<IProducedVideoRepository>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.AddSingleton<IAgentDeviceRepository>(new InMemoryAgentDeviceRepository());
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(new InMemoryTechnicalLogRepository());
            services.AddSingleton<IProfileRepository>(Profiles);
            services.AddSingleton<IConceptSettingsRepository>(Settings);
            services.AddSingleton<IApiKeyRepository>(Keys);
            services.AddSingleton<IProviderKeyValidator>(new FakeProviderKeyValidator());
            services.AddSingleton<IProviderClient>(Provider);
            services.AddSingleton<ISpendRepository>(Spend);
            services.AddSingleton<IConceptRepository>(Concepts);
            services.AddSingleton<ITopicSuggestionRepository>(Suggestions);
            services.AddSingleton<IProducedVideoRepository>(Videos); // Spec 0019: "beğenilmedi" negatif sinyali video kayıtlarından okunur.
        });
    }
}
