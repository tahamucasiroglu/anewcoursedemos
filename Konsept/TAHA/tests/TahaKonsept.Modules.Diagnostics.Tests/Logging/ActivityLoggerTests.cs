using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Logging;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;
using TahaKonsept.Modules.Diagnostics.Tests.Support;
using TahaKonsept.Shared.Contracts.Diagnostics;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.Logging;

/// <summary>
/// <see cref="ActivityLogger"/>: kullanıcı çözümü, süreli mesaj biçimi, ayrıntı JSON'u ve yazım hatasının yutulması.
/// </summary>
public sealed class ActivityLoggerTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 7, 30, 0, TimeSpan.Zero); // 10:30 Türkiye
    private readonly string _dbName = Guid.NewGuid().ToString("N");
    private readonly FakeClock _clock = new(Start);

    /// <summary>Kullanıcı verilmezse scope'taki oturum kullanıcısı yazılır.</summary>
    [Fact]
    public async Task Uses_current_user_when_user_not_given()
    {
        var userId = Guid.NewGuid();
        var logger = CreateLogger(TestUsers.Regular(userId));

        await logger.InformationAsync(ActivitySources.Production, "Konsept üretildi", cancellationToken: TestContext.Current.CancellationToken);

        var log = ReadAll().ShouldHaveSingleItem();
        log.UserId.ShouldBe(userId);
        log.Level.ShouldBe(LogLevelKind.Information);
        log.Source.ShouldBe(ActivitySources.Production);
        log.Message.ShouldBe("Konsept üretildi");
        log.CreatedAtUtc.ShouldBe(Start);
    }

    /// <summary>Açık kullanıcı oturum kullanıcısını ezer; oturum yoksa sistem (boş Guid) yazılır.</summary>
    [Fact]
    public async Task Explicit_user_wins_and_anonymous_becomes_system()
    {
        var explicitUser = Guid.NewGuid();
        var logger = CreateLogger(TestUsers.Anonymous());

        await logger.WarningAsync("Agent", "Ajan bağlantısı koptu", userId: explicitUser, cancellationToken: TestContext.Current.CancellationToken);
        await logger.ErrorAsync("Agent", "Render başarısız", cancellationToken: TestContext.Current.CancellationToken);

        var logs = ReadAll();
        logs.Count.ShouldBe(2);
        logs.Single(l => l.Level == LogLevelKind.Warning).UserId.ShouldBe(explicitUser);
        logs.Single(l => l.Level == LogLevelKind.Error).UserId.ShouldBe(Guid.Empty);
    }

    /// <summary>Süreli kayıt: "— süre 30 dk, başlangıç 10:30, bitiş 11:00" ve süre ms olarak.</summary>
    [Fact]
    public async Task LogTimed_appends_duration_and_turkish_local_times()
    {
        var logger = CreateLogger(TestUsers.Anonymous());

        await logger.LogTimedAsync(ActivitySources.Production, "Video hazırlandı", Start, Start.AddMinutes(30), cancellationToken: TestContext.Current.CancellationToken);

        var log = ReadAll().ShouldHaveSingleItem();
        log.Message.ShouldBe("Video hazırlandı — süre 30 dk, başlangıç 10:30, bitiş 11:00");
        log.DurationMs.ShouldBe(30 * 60 * 1000);
    }

    /// <summary>Bir dakikadan kısa işlemlerde saniyeli saat gösterilir.</summary>
    [Fact]
    public async Task LogTimed_short_operation_shows_seconds()
    {
        var logger = CreateLogger(TestUsers.Anonymous());

        await logger.LogTimedAsync(ActivitySources.Production, "Ses üretildi", Start, Start.AddSeconds(57), cancellationToken: TestContext.Current.CancellationToken);

        ReadAll().ShouldHaveSingleItem().Message.ShouldBe("Ses üretildi — süre 57 sn, başlangıç 10:30:00, bitiş 10:30:57");
    }

    /// <summary>Ayrıntı nesnesi camelCase JSON olarak saklanır; düz metin JSON dizesine çevrilir.</summary>
    [Fact]
    public async Task Details_are_stored_as_json()
    {
        var logger = CreateLogger(TestUsers.Anonymous());

        await logger.InformationAsync("Production", "İş bitti", new { JobId = 7, Stage = "Render" }, cancellationToken: TestContext.Current.CancellationToken);
        await logger.InformationAsync("Production", "Not", "düz metin", cancellationToken: TestContext.Current.CancellationToken);

        var logs = ReadAll();
        using var doc = JsonDocument.Parse(logs.Single(l => l.Message == "İş bitti").Details!);
        doc.RootElement.GetProperty("jobId").GetInt32().ShouldBe(7);
        doc.RootElement.GetProperty("stage").GetString().ShouldBe("Render");
        logs.Single(l => l.Message == "Not").Details.ShouldBe("\"düz metin\"");
    }

    /// <summary>Veritabanı yoksa (yazım hatası) istisna çağırana sızmaz.</summary>
    [Fact]
    public async Task Write_failure_is_swallowed()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new ActivityLogger(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<ActivityLogger>.Instance);

        await Should.NotThrowAsync(() => logger.CriticalAsync("Api", "DB yok", cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Boş kaynak "System" olur, çok uzun mesaj kırpılır.</summary>
    [Fact]
    public async Task Normalizes_source_and_clips_message()
    {
        var logger = CreateLogger(TestUsers.Anonymous());

        await logger.InformationAsync(" ", new string('x', 1500), cancellationToken: TestContext.Current.CancellationToken);

        var log = ReadAll().ShouldHaveSingleItem();
        log.Source.ShouldBe(ActivitySources.System);
        log.Message.Length.ShouldBe(1000);
        log.Message.ShouldEndWith("…");
    }

    private ActivityLogger CreateLogger(ICurrentUser user)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(_clock);
        services.AddScoped(_ => user);
        services.AddScoped(sp => TestDb.Create(sp.GetRequiredService<IClock>(), _dbName));
        var provider = services.BuildServiceProvider();
        return new ActivityLogger(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ActivityLogger>.Instance);
    }

    private List<Domain.Entities.ActivityLog> ReadAll()
    {
        using DiagnosticsDbContext db = TestDb.Create(_clock, _dbName);
        return db.ActivityLogs.ToList();
    }
}
