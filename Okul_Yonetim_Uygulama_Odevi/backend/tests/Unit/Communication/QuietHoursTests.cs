using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Features.Communication.WorkingHours;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Communication;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Communication;

/// <summary>
/// Spec 0011 AC-9 (sessiz mod kuralı): mesai içi = seçili gün ve [başlangıç, bitiş) — başlangıç dahil, bitiş hariç; seçili
/// olmayan gün mesai dışı. Saat Türkiye saatidir: UTC 05:00 = Türkiye 08:00 (aynı gün), UTC 21:00 Pazar = Türkiye 00:00
/// Pazartesi. Geçersiz tanım (adım dışı saat, başlangıç ≥ bitiş, gün yok) reddedilir; API biçimi "SS:DD" ve gün adları.
/// </summary>
public sealed class QuietHoursTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    private static QuietHours Weekdays() =>
        QuietHours.Define(Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(8, 0), new TimeOnly(17, 0), WorkDays.Monday | WorkDays.Tuesday | WorkDays.Wednesday | WorkDays.Thursday | WorkDays.Friday, Now);

    [Theory]
    [InlineData(7, 59, false)]
    [InlineData(8, 0, true)]
    [InlineData(12, 30, true)]
    [InlineData(16, 59, true)]
    [InlineData(17, 0, false)]
    [InlineData(23, 45, false)]
    public void IsWorkingTime_OnWorkday_StartIncludedEndExcluded(int hour, int minute, bool expected)
    {
        Weekdays().IsWorkingTime(DayOfWeek.Friday, new TimeOnly(hour, minute)).ShouldBe(expected);
    }

    [Fact]
    public void IsWorkingTime_OnUnselectedDay_IsOutside()
    {
        QuietHours hours = Weekdays();

        hours.IsWorkingTime(DayOfWeek.Saturday, new TimeOnly(10, 0)).ShouldBeFalse();
        hours.IsWorkingTime(DayOfWeek.Sunday, new TimeOnly(10, 0)).ShouldBeFalse();
        hours.IsWorkingTime(DayOfWeek.Monday, new TimeOnly(10, 0)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(5, 0, true)]
    [InlineData(4, 59, false)]
    [InlineData(14, 0, false)]
    public void IsWorkingTime_UsesTurkeyClock(int utcHour, int utcMinute, bool expected)
    {
        SchoolCalendar calendar = new(new FakeTimeProvider(), NullLogger<SchoolCalendar>.Instance);
        DateTimeOffset local = calendar.TurkeyTime(new DateTimeOffset(2026, 11, 20, utcHour, utcMinute, 0, TimeSpan.Zero));

        Weekdays().IsWorkingTime(local.DayOfWeek, TimeOnly.FromDateTime(local.DateTime)).ShouldBe(expected);
    }

    [Fact]
    public void IsWorkingTime_UtcSundayNight_IsTurkeyMonday()
    {
        SchoolCalendar calendar = new(new FakeTimeProvider(), NullLogger<SchoolCalendar>.Instance);
        QuietHours midnight = QuietHours.Define(Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(0, 0), new TimeOnly(1, 0), WorkDays.Monday, Now);
        DateTimeOffset local = calendar.TurkeyTime(new DateTimeOffset(2026, 11, 22, 21, 0, 0, TimeSpan.Zero));

        local.DayOfWeek.ShouldBe(DayOfWeek.Monday);
        midnight.IsWorkingTime(local.DayOfWeek, TimeOnly.FromDateTime(local.DateTime)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(8, 10, 17, 0, WorkDays.Monday)]
    [InlineData(17, 0, 8, 0, WorkDays.Monday)]
    [InlineData(9, 0, 9, 0, WorkDays.Monday)]
    [InlineData(8, 0, 17, 0, WorkDays.None)]
    public void Define_Invalid_Throws(int startHour, int startMinute, int endHour, int endMinute, WorkDays days)
    {
        Should.Throw<ArgumentException>(() => QuietHours.Define(Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute), days, Now));
    }

    [Theory]
    [InlineData("08:00", true)]
    [InlineData("08:15", true)]
    [InlineData("23:45", true)]
    [InlineData("08:10", false)]
    [InlineData("8:00", false)]
    [InlineData("24:00", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParseTime_AcceptsOnlyQuarterHourHhMm(string? text, bool expected)
    {
        QuietHoursText.TryParseTime(text, out _).ShouldBe(expected);
    }

    [Fact]
    public void Days_RoundTripInWeekOrder_UnknownNameRejected()
    {
        QuietHoursText.TryParseDays(["Friday", "Monday"], out WorkDays days).ShouldBeTrue();
        days.ShouldBe(WorkDays.Monday | WorkDays.Friday);
        QuietHoursText.DayNames(days).ShouldBe(["Monday", "Friday"]);
        QuietHoursText.TryParseDays(["Pazartesi"], out _).ShouldBeFalse();
    }
}
