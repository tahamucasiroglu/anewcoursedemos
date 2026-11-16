using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Shouldly;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>
/// Günlük gün kuralları ve olay ayrıntıları (spec 0021 Req 4, 6, 8, 9, 12 — AC-7, AC-9, AC-12).
/// </summary>
public sealed class JournalRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);

    /// <summary>Europe/Istanbul gece yarısı: 23:59:59'daki kayıt o güne, 00:00:00'daki ertesi güne düşer; aralık UTC'dir.</summary>
    [Fact]
    public void JournalDay_IstanbulMidnight_SplitsDays()
    {
        var lastSecond = new DateTimeOffset(2026, 11, 15, 23, 59, 59, TimeSpan.FromHours(3));
        var midnight = new DateTimeOffset(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(3));

        JournalDay.DayOf(lastSecond).ShouldBe(new DateOnly(2026, 11, 15));
        JournalDay.DayOf(midnight).ShouldBe(new DateOnly(2026, 11, 16));
        JournalDay.DayOf(midnight.ToUniversalTime()).ShouldBe(new DateOnly(2026, 11, 16));

        var (from, to) = JournalDay.RangeOf(new DateOnly(2026, 11, 16));
        from.ShouldBe(new DateTimeOffset(2026, 11, 15, 21, 0, 0, TimeSpan.Zero));
        to.ShouldBe(new DateTimeOffset(2026, 11, 16, 21, 0, 0, TimeSpan.Zero));
        (lastSecond >= from).ShouldBeFalse();
        (midnight >= from && midnight < to).ShouldBeTrue();
    }

    /// <summary>İleri, geçersiz ya da boş tarih bugünü; ilk günden önceki tarih ilk günü döndürür.</summary>
    /// <param name="requested">İstenen.</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData("2026-11-17", "2026-11-16")]
    [InlineData("2030-01-01", "2026-11-16")]
    [InlineData("dün", "2026-11-16")]
    [InlineData("2026-13-40", "2026-11-16")]
    [InlineData(null, "2026-11-16")]
    [InlineData("2026-11-10", "2026-11-10")]
    [InlineData("2026-10-01", "2026-11-01")]
    public void JournalDay_FutureOrInvalid_ReturnsToday(string? requested, string expected) =>
        JournalDay.Resolve(requested, Today, new DateOnly(2026, 11, 1)).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).ShouldBe(expected);

    /// <summary>"Hata" filtresi Hata ve Kritik'i, "Uyarı" yalnızca Uyarı'yı kapsar; bilinmeyen filtre Tümü'dür.</summary>
    [Fact]
    public void JournalFilter_ErrorIncludesCritical()
    {
        JournalDay.LevelsOf(JournalDay.ParseFilter("error"))!.ShouldBe([EventLevel.Error, EventLevel.Critical]);
        JournalDay.LevelsOf(JournalDay.ParseFilter("WARNING"))!.ShouldBe([EventLevel.Warning]);
        JournalDay.LevelsOf(JournalDay.ParseFilter("x")).ShouldBeNull();
    }

    /// <summary>
    /// <c>&lt;script&gt;</c> metni olduğu gibi saklanır (gösterim düz metindir); API key maskelenir; süre ve profil etiketi saklanır,
    /// ters verilen başlangıç–bitiş düzeltilir.
    /// </summary>
    [Fact]
    public void SystemEvent_ScriptKeptSecretMasked()
    {
        var now = new DateTimeOffset(2026, 11, 16, 7, 31, 0, TimeSpan.Zero);
        var e = SystemEvent.Create("video.ready", EventLevel.Info, "<script>alert(1)</script> anahtar sk-ant-api03-abcdefa3f", now, Guid.NewGuid())
            .WithDetails(now, now.AddSeconds(-57), Guid.NewGuid(), "  Kanal sk-ant-api03-abcdefa3f ");

        e.Message.ShouldStartWith("<script>alert(1)</script>");
        e.Message.ShouldNotContain("api03-abcdefa3f");
        e.StartedAt.ShouldBe(now.AddSeconds(-57));
        e.EndedAt.ShouldBe(now);
        e.ProfileName!.ShouldStartWith("Kanal");
        e.ProfileName.ShouldNotContain("api03-abcdefa3f");
    }
}
