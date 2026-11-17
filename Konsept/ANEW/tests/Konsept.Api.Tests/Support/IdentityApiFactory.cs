using System.Net.Http.Headers;
using Konsept.Api.Tests.Fakes;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Kimlik testleri için hızlı API (plan 0002): saat <see cref="FakeTimeProvider"/> (kilit süresi, 14 günlük oturum, hız
/// sınırı penceresi, JWT ömrü aynı saatten), bellek kullanıcı/oturum/olay/teknik log depoları, örneğe özel log klasörü,
/// test servis ve imza anahtarları, ilk admin e-postası <see cref="AdminEmail"/> (plan 0011).
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private InMemoryAdminActivityRepository? _adminActivities;

    /// <summary>Gets ilerletilebilir saat (2026-11-14 09:00 UTC'den başlar).</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Testlerdeki ilk admin e-postası (<c>Identity:AdminEmail</c>; örnek adres, gerçek kişi değil — plan 0011).</summary>
    public const string AdminEmail = "admin@ornek.test";

    /// <summary>Initializes a new instance of the <see cref="IdentityApiFactory"/> class.</summary>
    public IdentityApiFactory()
    {
        Sessions = new InMemorySessionRepository(Users); // test kurulumu: rol (plan 0011) — oturum doğrulaması rolü kullanıcıdan okur.
    }

    /// <summary>Gets bellek ajan cihaz deposu (plan 0012).</summary>
    public InMemoryAgentDeviceRepository AgentDevices { get; } = new();

    /// <summary>Gets ban kayıtları (plan 0024).</summary>
    public InMemoryUserBanRepository Bans { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>
    /// Admin oturumlu (Bearer) istemci (plan 0011): <see cref="AdminEmail"/> hesabı yoksa gerçek kayıt ucuyla açılır (ayar
    /// gereği admin olur), sonra gerçek giriş ucuyla token alınır.
    /// </summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstemci.</returns>
    public async Task<HttpClient> CreateAdminClientAsync(CancellationToken cancellationToken)
    {
        var client = CreateClient();
        if (!Users.Users.Any(u => u.NormalizedEmail == AdminEmail.ToUpperInvariant()))
        {
            await AuthenticatedClient.RegisterAsync(client, Services, AdminEmail, AuthenticatedClient.Password, "Admin", cancellationToken).ConfigureAwait(false);
        }

        var session = await AuthenticatedClient.LoginAsync(client, Services, AdminEmail, AuthenticatedClient.Password, cancellationToken).ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return client;
    }

    /// <summary>Gets bellek olay deposu.</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets bellek teknik log deposu.</summary>
    public InMemoryTechnicalLogRepository TechnicalLogs { get; } = new();

    /// <summary>Gets bellek hata bildirimi deposu (plan 0022).</summary>
    public InMemoryErrorReportRepository ErrorReports { get; } = new();

    /// <summary>Gets bellek video sürüm deposu (klip istatistiği kaynağı; plan 0010).</summary>
    public InMemoryProducedVideoRepository ProducedVideos { get; } = new();

    /// <summary>Gets bellek admin sorguları (plan 0010); olay ve hata bildirimi depolarını okur.</summary>
    public InMemoryAdminActivityRepository AdminActivities => _adminActivities ??= new InMemoryAdminActivityRepository(Events, ErrorReports);

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "identity-logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Log klasöründeki tüm log metnini okur.</summary>
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
        builder.UseSetting("Identity:AdminEmail", AdminEmail);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ISessionRepository>();
            services.RemoveAll<ISystemEventRepository>();
            services.RemoveAll<IDatabaseProbe>();
            services.RemoveAll<ITechnicalLogRepository>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<ISessionRepository>(Sessions);
            services.RemoveAll<IAgentDeviceRepository>(); // test kurulumu: ajan cihazları (plan 0012).
            services.AddSingleton<IAgentDeviceRepository>(AgentDevices);
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(TechnicalLogs);
            services.RemoveAll<IErrorReportRepository>(); // test kurulumu: hata bildirimleri (plan 0022 A-1).
            services.AddSingleton<IErrorReportRepository>(ErrorReports);
            services.RemoveAll<IUserDirectory>(); // test kurulumu: admin istatistikleri (plan 0010).
            services.RemoveAll<IClipStatistics>();
            services.RemoveAll<IAdminActivityRepository>();
            services.AddSingleton<IUserDirectory>(new InMemoryUserDirectory(Users));
            services.AddSingleton<IClipStatistics>(new InMemoryClipStatistics(ProducedVideos));
            services.AddSingleton<IAdminActivityRepository>(AdminActivities);
            services.RemoveAll<IUserBanRepository>(); // test kurulumu: ban kayıtları (plan 0024).
            services.AddSingleton<IUserBanRepository>(Bans);
        });
    }
}
