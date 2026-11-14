using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Konsept.Shared.Logging.Tests;

/// <summary>Log klasörü temizlik zamanlayıcısı (spec 0008 AC-4; review L-3).</summary>
public sealed class LogFileCleanupSchedulerTests
{
    /// <summary>Başlatınca hemen bir temizlik yapar, sonra günde bir tekrarlar.</summary>
    [Fact]
    public void Scheduler_Start_CleansNowAndDaily()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));
        var janitor = new CountingJanitor();
        using var scheduler = new LogFileCleanupScheduler(janitor, "logs", time).Start();

        janitor.Calls.ShouldBe(1);
        time.Advance(TimeSpan.FromHours(23));
        janitor.Calls.ShouldBe(1);
        time.Advance(TimeSpan.FromHours(1));
        janitor.Calls.ShouldBe(2);
        time.Advance(TimeSpan.FromDays(1));
        janitor.Calls.ShouldBe(3);
    }

    /// <summary>Klasör izni ya da disk hatası zamanlayıcıyı (ve süreci) düşürmez; ertesi gün yeniden denenir.</summary>
    /// <param name="kind">Hata türü.</param>
    [Theory]
    [InlineData("io")]
    [InlineData("access")]
    public void Scheduler_JanitorFails_DoesNotThrowAndRetries(string kind)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));
        Exception failure = kind == "io" ? new IOException("disk") : new UnauthorizedAccessException("izin");
        var janitor = new CountingJanitor(failure);

        using var scheduler = Should.NotThrow(() => new LogFileCleanupScheduler(janitor, "logs", time).Start());
        Should.NotThrow(() => time.Advance(TimeSpan.FromDays(1)));

        janitor.Calls.ShouldBe(2);
    }
}
