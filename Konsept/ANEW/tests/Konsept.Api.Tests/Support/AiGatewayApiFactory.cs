using System.Net.Http.Headers;
using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Infrastructure.Providers.Concrete;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// AiGateway testleri için hızlı API (plan 0004): bellek kullanıcı/oturum/olay/teknik log ve API key depoları, sahte sağlayıcı
/// doğrulaması (gerçek çağrı yok), örneğe özel geçici Data Protection anahtar klasörü ve log klasörü, ilk admin e-postası
/// <see cref="IdentityApiFactory.AdminEmail"/>. <see cref="WithOtherKeyRing"/> aynı depoları paylaşan ama başka anahtar halkası
/// kullanan ikinci bir Api açar ("şifre çözme anahtarları kayboldu", AC-11). İstenirse katalog seed'i değiştirilir (AC-2).
/// </summary>
public sealed class AiGatewayApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Initializes a new instance of the <see cref="AiGatewayApiFactory"/> class.</summary>
    public AiGatewayApiFactory()
    {
        Users = new InMemoryUserRepository();
        Sessions = new InMemorySessionRepository(Users);
        Keys = new InMemoryApiKeyRepository();
    }

    private AiGatewayApiFactory(AiGatewayApiFactory parent)
    {
        Users = parent.Users;
        Sessions = parent.Sessions;
        Keys = parent.Keys;
        Validator = parent.Validator;
        Provider = parent.Provider;
        Spend = parent.Spend;
    }

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; }

    /// <summary>Gets bellek ajan cihaz deposu (plan 0012).</summary>
    public InMemoryAgentDeviceRepository AgentDevices { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets bellek API key deposu ("veritabanı").</summary>
    public InMemoryApiKeyRepository Keys { get; }

    /// <summary>Gets sahte sağlayıcı doğrulaması.</summary>
    public FakeProviderKeyValidator Validator { get; } = new();

    /// <summary>Gets sahte sağlayıcı istemcisi (plan 0013; gerçek çağrı yok).</summary>
    public FakeProviderClient Provider { get; } = new();

    /// <summary>Gets bellek harcama deposu (plan 0013).</summary>
    public InMemorySpendRepository Spend { get; } = new();

    /// <summary>
    /// Gets or sets kullanıcı rolünün model listesi (<c>Identity:ModelPermissions:User</c>, plan 0013); <c>null</c> → tümü. İlk
    /// istekten önce ayarlanır.
    /// </summary>
    public string? UserModelPermissions { get; set; }

    /// <summary>
    /// Gets or sets gerçek <c>HttpProviderClient</c>'ın kullanacağı sahte HTTP işleyicisi; verilirse sahte istemci yerine gerçek
    /// istemci bu işleyiciyle kullanılır (sızıntı testi, plan 0013). İlk istekten önce ayarlanır.
    /// </summary>
    public HttpMessageHandler? ProviderHandler { get; set; }

    /// <summary>Gets bellek olay deposu.</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets bellek teknik log deposu.</summary>
    public InMemoryTechnicalLogRepository TechnicalLogs { get; } = new();

    /// <summary>Gets or sets katalog seed'i; <c>null</c> ise gömülü seed (ilk istekten önce ayarlanır).</summary>
    public CatalogSeed? Catalog { get; set; }

    /// <summary>Gets bu örneğin Data Protection anahtar klasörü.</summary>
    public string KeysDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "dp-keys-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "ai-logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Aynı depoları paylaşan, başka (yeni) Data Protection anahtar halkalı ikinci Api.</summary>
    /// <returns>Fabrika.</returns>
    public AiGatewayApiFactory WithOtherKeyRing() => new(this);

    /// <summary>Yeni kullanıcı kaydeder ve Bearer istemci döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="email">E-posta (verilmezse benzersiz).</param>
    /// <returns>İstemci.</returns>
    public Task<HttpClient> CreateUserClientAsync(CancellationToken cancellationToken, string? email = null) =>
        AuthenticatedClient.CreateAsync(this, cancellationToken, email);

    /// <summary>Kayıtlı kullanıcıyla giriş yapar (kayıt yapmadan).</summary>
    /// <param name="email">E-posta.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstemci.</returns>
    public async Task<HttpClient> SignInAsync(string email, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var session = await AuthenticatedClient.LoginAsync(client, Services, email, AuthenticatedClient.Password, cancellationToken).ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return client;
    }

    /// <summary>Log klasöründeki tüm log metni.</summary>
    /// <returns>Metin.</returns>
    public string ReadLogText() => FastApiFactory.ReadLogText(LogDirectory);

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
        builder.UseSetting("Identity:AdminEmail", IdentityApiFactory.AdminEmail);
        builder.UseSetting("DataProtection:KeysDirectory", KeysDirectory);
        if (UserModelPermissions is { } models)
        {
            builder.UseSetting("Identity:ModelPermissions:User", models); // test kurulumu: model yetkisi (plan 0013).
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ISessionRepository>();
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.RemoveAll<ITechnicalLogRepository>();
            services.RemoveAll<IApiKeyRepository>();
            services.RemoveAll<IProviderKeyValidator>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.RemoveAll<IAgentDeviceRepository>(); // test kurulumu: ajan cihazları (plan 0012).
            services.AddSingleton<IAgentDeviceRepository>(AgentDevices);
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(TechnicalLogs);
            services.AddSingleton<IApiKeyRepository>(Keys);
            services.AddSingleton<IProviderKeyValidator>(Validator);
            services.RemoveAll<IProviderClient>(); // test kurulumu: sahte sağlayıcı ve harcama deposu (plan 0013).
            services.RemoveAll<ISpendRepository>();
            services.AddSingleton<ISpendRepository>(Spend);
            services.AddSingleton<IProviderClient>(ProviderHandler is { } handler
                ? new HttpProviderClient(new StubHttpClientFactory(handler))
                : Provider);
            if (Catalog is { } seed)
            {
                services.RemoveAll<ICatalogSource>();
                services.AddSingleton<ICatalogSource>(new FixedCatalogSource(seed));
            }
        });
    }
}
