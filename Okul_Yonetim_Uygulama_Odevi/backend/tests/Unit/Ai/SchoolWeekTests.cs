using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Ai;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0014 AC-1 (hafta seçimi) ve AC-2 (hafta sınırı, birim): hafta Türkiye günüyle Pazartesi–Pazar; Pazar 23:59 (Türkiye) önceki
/// haftada, Pazartesi 00:00 (Türkiye = Pazar 21:00 UTC) yeni haftada; "geçen hafta" bir önceki Pazartesi–Pazar; geçersiz seçim
/// reddedilir; aralık metni "16–22 Kasım", ay değişince "30 Kasım – 6 Aralık".
/// </summary>
public sealed class SchoolWeekTests
{
    private static readonly TimeZoneInfo? Istanbul = FindIstanbul();

    [Theory]
    [InlineData("2026-11-16", "2026-11-16")]
    [InlineData("2026-11-18", "2026-11-16")]
    [InlineData("2026-11-22", "2026-11-16")]
    [InlineData("2026-11-23", "2026-11-23")]
    public void Containing_Day_StartsOnMondayEndsOnSunday(string day, string monday)
    {
        SchoolWeek week = SchoolWeek.Containing(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture));

        week.Start.ShouldBe(DateOnly.Parse(monday, System.Globalization.CultureInfo.InvariantCulture));
        week.Start.DayOfWeek.ShouldBe(DayOfWeek.Monday);
        week.End.ShouldBe(week.Start.AddDays(6));
        week.End.DayOfWeek.ShouldBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void Choose_TurkeyMidnightBoundary_SundayLateInOldWeekMondayMidnightInNewWeek()
    {
        DateOnly sundayLate = SchoolCalendar.TurkeyDay(new DateTimeOffset(2026, 11, 22, 20, 59, 0, TimeSpan.Zero), Istanbul);
        DateOnly mondayMidnight = SchoolCalendar.TurkeyDay(new DateTimeOffset(2026, 11, 22, 21, 0, 0, TimeSpan.Zero), Istanbul);

        sundayLate.ShouldBe(new DateOnly(2026, 11, 22), "Türkiye 23:59 Pazar");
        mondayMidnight.ShouldBe(new DateOnly(2026, 11, 23), "Türkiye 00:00 Pazartesi");
        SchoolWeek.Choose(SchoolWeek.Current, sundayLate).Start.ShouldBe(new DateOnly(2026, 11, 16));
        SchoolWeek.Choose(SchoolWeek.Current, mondayMidnight).Start.ShouldBe(new DateOnly(2026, 11, 23));
    }

    [Fact]
    public void Choose_Previous_IsMondayToSundayBeforeCurrentWeek()
    {
        DateOnly friday = new(2026, 11, 20);

        SchoolWeek current = SchoolWeek.Choose(SchoolWeek.Current, friday);
        SchoolWeek previous = SchoolWeek.Choose(SchoolWeek.Previous, friday);

        current.Start.ShouldBe(new DateOnly(2026, 11, 16));
        previous.Start.ShouldBe(new DateOnly(2026, 11, 9));
        previous.End.ShouldBe(new DateOnly(2026, 11, 15));
        SchoolWeek.Choose(SchoolWeek.Previous, new DateOnly(2026, 11, 16)).Start.ShouldBe(new DateOnly(2026, 11, 9), "Pazartesi günü geçen hafta");
    }

    [Theory]
    [InlineData("current", true)]
    [InlineData("previous", true)]
    [InlineData("next", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Current", false)]
    public void IsValidChoice_OnlyCurrentOrPrevious(string? choice, bool valid) =>
        SchoolWeek.IsValidChoice(choice).ShouldBe(valid);

    [Fact]
    public void Contains_BoundaryDays_MondayAndSundayInsideNeighboursOutside()
    {
        SchoolWeek week = SchoolWeek.StartingOn(new DateOnly(2026, 11, 16));

        week.Contains(new DateOnly(2026, 11, 15)).ShouldBeFalse("önceki Pazar");
        week.Contains(new DateOnly(2026, 11, 16)).ShouldBeTrue("Pazartesi");
        week.Contains(new DateOnly(2026, 11, 22)).ShouldBeTrue("Pazar");
        week.Contains(new DateOnly(2026, 11, 23)).ShouldBeFalse("sonraki Pazartesi");
        Should.Throw<ArgumentOutOfRangeException>(() => SchoolWeek.StartingOn(new DateOnly(2026, 11, 17)));
    }

    [Theory]
    [InlineData("2026-11-16", "16–22 Kasım")]
    [InlineData("2026-11-30", "30 Kasım – 6 Aralık")]
    [InlineData("2026-12-28", "28 Aralık – 3 Ocak")]
    public void Label_SameOrDifferentMonth_TurkishRange(string monday, string expected) =>
        SchoolWeek.StartingOn(DateOnly.Parse(monday, System.Globalization.CultureInfo.InvariantCulture)).Label().ShouldBe(expected);

    private static TimeZoneInfo? FindIstanbul()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }
}
