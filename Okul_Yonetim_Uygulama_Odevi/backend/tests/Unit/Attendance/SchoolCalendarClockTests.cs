using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Features.Attendance;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Domain.Organization;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Attendance;

/// <summary>
/// Spec 0010 AC-6 (gün kuralı, plan Risks 3): yoklamanın "bugün"ü Türkiye günüdür — UTC 20:59 = Türkiye 23:59 (aynı gün),
/// UTC 21:00 = Türkiye 00:00 (ertesi gün); sabit UTC+3 yedeği de aynı günü verir. Okul günü: dönem içi ve takvimde tatil
/// değil (sınav/etkinlik okul günüdür, silinen tatil sayılmaz, hafta sonu engellenmez). Kayıt saati Türkiye saatiyle.
/// </summary>
public sealed class SchoolCalendarClockTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Term Autumn = Term.Create(Tenant, "Güz", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 22), Created);

    [Theory]
    [InlineData(20, 59, 20)]
    [InlineData(21, 0, 21)]
    [InlineData(21, 1, 21)]
    public void Today_AroundTurkeyMidnight_UsesTurkeyDay(int utcHour, int utcMinute, int expectedDay)
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 20, utcHour, utcMinute, 0, TimeSpan.Zero));
        SchoolCalendar calendar = new(time, NullLogger<SchoolCalendar>.Instance);

        calendar.Today().ShouldBe(new DateOnly(2026, 11, expectedDay));
        SchoolCalendar.TurkeyDay(time.GetUtcNow(), null).ShouldBe(new DateOnly(2026, 11, expectedDay), "sabit UTC+3 yedeği");
    }

    [Fact]
    public void TurkeyTime_SavedAt_ShowsTurkeyClock()
    {
        SchoolCalendar calendar = new(new FakeTimeProvider(), NullLogger<SchoolCalendar>.Instance);

        calendar.TurkeyTime(new DateTimeOffset(2026, 11, 20, 5, 42, 0, TimeSpan.Zero)).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture).ShouldBe("08:42");
    }

    [Fact]
    public void IsSchoolDay_InsideTermWithoutHoliday_IsTrue_WeekendIncluded()
    {
        SchoolCalendar.IsSchoolDay(Autumn, [], new DateOnly(2026, 11, 20)).ShouldBeTrue();
        SchoolCalendar.IsSchoolDay(Autumn, [], new DateOnly(2026, 11, 21)).ShouldBeTrue("cumartesi engellenmez (kurslar)");
        SchoolCalendar.IsSchoolDay(Autumn, [], new DateOnly(2026, 9, 1)).ShouldBeTrue("dönemin ilk günü");
        SchoolCalendar.IsSchoolDay(Autumn, [], new DateOnly(2027, 1, 22)).ShouldBeTrue("dönemin son günü");
    }

    [Fact]
    public void IsSchoolDay_OutsideTerm_IsFalse()
    {
        SchoolCalendar.IsSchoolDay(Autumn, [], new DateOnly(2026, 8, 31)).ShouldBeFalse();
        SchoolCalendar.IsSchoolDay(Autumn, [], new DateOnly(2027, 1, 23)).ShouldBeFalse();
    }

    [Fact]
    public void IsSchoolDay_HolidayCoveringDay_IsFalse_ExamAndDeletedHolidayAreSchoolDays()
    {
        CalendarEntry holiday = CalendarEntry.Create(Tenant, Autumn.Id, "Ara tatil", CalendarEntryKind.Holiday, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 20), Created);
        CalendarEntry exam = CalendarEntry.Create(Tenant, Autumn.Id, "Sınav", CalendarEntryKind.Exam, new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 23), Created);
        CalendarEntry deleted = CalendarEntry.Create(Tenant, Autumn.Id, "İptal", CalendarEntryKind.Holiday, new DateOnly(2026, 11, 24), new DateOnly(2026, 11, 24), Created);
        deleted.Delete(Created);
        CalendarEntry[] entries = [holiday, exam, deleted];

        SchoolCalendar.IsSchoolDay(Autumn, entries, new DateOnly(2026, 11, 16)).ShouldBeFalse("tatilin ilk günü");
        SchoolCalendar.IsSchoolDay(Autumn, entries, new DateOnly(2026, 11, 20)).ShouldBeFalse("tatilin son günü");
        SchoolCalendar.IsSchoolDay(Autumn, entries, new DateOnly(2026, 11, 15)).ShouldBeTrue();
        SchoolCalendar.IsSchoolDay(Autumn, entries, new DateOnly(2026, 11, 23)).ShouldBeTrue("sınav günü");
        SchoolCalendar.IsSchoolDay(Autumn, entries, new DateOnly(2026, 11, 24)).ShouldBeTrue("silinen tatil");
    }

    [Theory]
    [InlineData("Present", AttendanceStatus.Present)]
    [InlineData("absent", AttendanceStatus.Absent)]
    [InlineData("LATE", AttendanceStatus.Late)]
    public void TryParseStatus_KnownNames_Accepted(string text, AttendanceStatus expected)
    {
        SaveTodayAttendanceCommandValidator.TryParseStatus(text, out AttendanceStatus status).ShouldBeTrue();
        status.ShouldBe(expected);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("Excused")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseStatus_UnknownOrNumber_Rejected(string? text) =>
        SaveTodayAttendanceCommandValidator.TryParseStatus(text, out _).ShouldBeFalse();
}
