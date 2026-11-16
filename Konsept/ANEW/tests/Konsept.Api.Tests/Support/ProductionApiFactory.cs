using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;
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
/// Konseptten Video testleri için hızlı API (plan 0006): bellek kullanıcı/oturum/olay/teknik log, profil, konsept ayarı, API key, harcama,
/// konsept, öneri, üretim işi ve üretim ayarı depoları; sahte key doğrulaması ve sahte sağlayıcı (gerçek LLM çağrısı yok). İsteğe bağlı
/// sabit katalog (tek dilli test girişi) ve kullanıcı model yetkisi. Production, Concepts ve AiGateway servisleri gerçektir.
/// </summary>
public class ProductionApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Initializes a new instance of the <see cref="ProductionApiFactory"/> class.</summary>
    public ProductionApiFactory()
    {
        Sessions = new InMemorySessionRepository(Users);
    }

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets bellek profil deposu.</summary>
    public InMemoryProfileRepository Profiles { get; } = new();

    /// <summary>Gets bellek API key deposu.</summary>
    public InMemoryApiKeyRepository Keys { get; } = new();

    /// <summary>Gets bellek konsept deposu.</summary>
    public InMemoryConceptRepository Concepts { get; } = new();

    /// <summary>Gets bellek üretim işi deposu.</summary>
    public InMemoryProductionJobRepository Jobs { get; } = new();

    /// <summary>Gets bellek üretim ayarı deposu.</summary>
    public InMemoryProductionSettingsRepository ProductionSettings { get; } = new();

    /// <summary>Gets harcama deposu (0017: iş maliyeti = harcama kayıtları).</summary>
    public InMemorySpendRepository Spend { get; } = new();

    /// <summary>Gets olay deposu (0017: Günlük olayları).</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets aşama çıktıları deposu (0017).</summary>
    public InMemoryStageOutputRepository Outputs { get; } = new();

    /// <summary>Gets video kayıtları deposu (0017).</summary>
    public InMemoryProducedVideoRepository Videos { get; } = new();

    /// <summary>Gets sahte ajan kanalı (0017; gerçek ajan yok).</summary>
    public FakeAgentChannel Agent { get; } = new();

    /// <summary>Gets sahte sağlayıcı.</summary>
    public ScriptedProviderClient Provider { get; } = new();

    /// <summary>Gets or sets sabit katalog (verilmezse gömülü seed).</summary>
    public CatalogSeed? Catalog { get; set; }

    /// <summary>Gets or sets kullanıcı rolünün model yetkileri (verilmezse rolün varsayılanı).</summary>
    public string? UserModelPermissions { get; set; }

    /// <summary>Gets a value indicating whether üretim hattı açık (0006 testleri işi Üretiliyor'da bekler: kapalı; plan 0017 Risk 4).</summary>
    protected virtual bool PipelineEnabled => false;

    /// <summary>Gets bu örneğin Data Protection anahtar klasörü.</summary>
    public string KeysDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "dp-keys-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "production-logs-" + Guid.NewGuid().ToString("N"));

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
        builder.UseSetting("Production:Pipeline:Enabled", PipelineEnabled ? "true" : "false");
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
            services.RemoveAll<IProviderClient>();
            services.RemoveAll<ISpendRepository>();
            services.RemoveAll<IConceptRepository>();
            services.RemoveAll<ITopicSuggestionRepository>();
            services.RemoveAll<IProductionJobRepository>();
            services.RemoveAll<IProductionSettingsRepository>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.AddSingleton<IAgentDeviceRepository>(new InMemoryAgentDeviceRepository());
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(new InMemoryTechnicalLogRepository());
            services.AddSingleton<IProfileRepository>(Profiles);
            services.AddSingleton<IConceptSettingsRepository>(new InMemoryConceptSettingsRepository());
            services.AddSingleton<IApiKeyRepository>(Keys);
            services.AddSingleton<IProviderKeyValidator>(new FakeProviderKeyValidator());
            services.AddSingleton<IProviderClient>(Provider);
            services.AddSingleton<ISpendRepository>(Spend);
            services.AddSingleton<IConceptRepository>(Concepts);
            services.AddSingleton<ITopicSuggestionRepository>(new InMemoryTopicSuggestionRepository());
            services.AddSingleton<IProductionJobRepository>(Jobs);
            services.AddSingleton<IProductionSettingsRepository>(ProductionSettings);
            services.RemoveAll<IStageOutputRepository>();
            services.RemoveAll<IProducedVideoRepository>();
            services.RemoveAll<IQueueStateRepository>();
            services.RemoveAll<IAgentChannel>();
            services.AddSingleton<IStageOutputRepository>(Outputs);
            services.AddSingleton<IProducedVideoRepository>(Videos);
            services.AddSingleton<IQueueStateRepository>(new InMemoryQueueStateRepository());
            services.AddSingleton<IAgentChannel>(Agent);
            if (Catalog is { } seed)
            {
                services.RemoveAll<ICatalogSource>();
                services.AddSingleton<ICatalogSource>(new FixedCatalogSource(seed));
            }
        });
    }
}
