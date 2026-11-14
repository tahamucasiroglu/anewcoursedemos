using Konsept.Api.Tests.Fakes;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Kimlik testleri için hızlı API (plan 0002): saat <see cref="FakeTimeProvider"/> (kilit süresi, 14 günlük oturum, hız
/// sınırı penceresi, JWT ömrü aynı saatten), bellek kullanıcı/oturum/olay/teknik log depoları, örneğe özel log klasörü,
/// test servis ve imza anahtarları.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Gets ilerletilebilir saat (2026-11-14 09:00 UTC'den başlar).</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Gets bellek kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Gets bellek oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; } = new();

    /// <summary>Gets bellek olay deposu.</summary>
    public InMemorySystemEventRepository Events { get; } = new();

    /// <summary>Gets bellek teknik log deposu.</summary>
    public InMemoryTechnicalLogRepository TechnicalLogs { get; } = new();

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
            services.AddSingleton<ISystemEventRepository>(Events);
            services.AddSingleton<IDatabaseProbe>(new FakeDatabaseProbe());
            services.AddSingleton<ITechnicalLogRepository>(TechnicalLogs);
        });
    }
}
