using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Organization;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Organization;

/// <summary>
/// Spec 0008 AC-6: güncel dönem Türkiye gününe göre (UTC'de önceki gün olan gece yarısı sonrası saatler dahil), uç günler
/// dahil; dönem arasında yaklaşan dönem; yalnızca geçmiş dönem varsa güncel dönem yok; listelerin varsayılanı.
/// </summary>
public sealed class CurrentTermTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Term Autumn = Term.Create(Guid.NewGuid(), "Güz", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 22), Created);
    private static readonly Term Spring = Term.Create(Guid.NewGuid(), "Bahar", new DateOnly(2027, 2, 8), new DateOnly(2027, 6, 18), Created);
    private static readonly Term Past = Term.Create(Guid.NewGuid(), "Geçen yıl", new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 19), Created);

    [Fact]
    public void Today_AfterMidnightInTurkey_WhileUtcIsPreviousDay_UsesTurkeyDay()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 31, 21, 30, 0, TimeSpan.Zero));
        SchoolCalendar calendar = new(time, NullLogger<SchoolCalendar>.Instance);

        DateOnly today = calendar.Today();

        today.ShouldBe(new DateOnly(2026, 9, 1));
        SchoolCalendar.PickCurrent([Past, Autumn, Spring], today).ShouldBe(Autumn, "Türkiye'de 1 Eylül 00:30: güz dönemi başladı");
    }

    [Fact]
    public void Today_FixedOffsetFallback_GivesSameDay()
    {
        SchoolCalendar.TurkeyDay(new DateTimeOffset(2026, 8, 31, 21, 30, 0, TimeSpan.Zero), null).ShouldBe(new DateOnly(2026, 9, 1));
        SchoolCalendar.TurkeyDay(new DateTimeOffset(2026, 8, 31, 20, 59, 0, TimeSpan.Zero), null).ShouldBe(new DateOnly(2026, 8, 31));
    }

    [Theory]
    [InlineData(2026, 9, 1, "Güz")]
    [InlineData(2027, 1, 22, "Güz")]
    [InlineData(2027, 2, 8, "Bahar")]
    [InlineData(2027, 6, 18, "Bahar")]
    public void PickCurrent_TermCoveringToday_IncludesEndDays(int year, int month, int day, string expected) =>
        SchoolCalendar.PickCurrent([Past, Autumn, Spring], new DateOnly(year, month, day))!.Name.ShouldBe(expected);

    [Fact]
    public void PickCurrent_BetweenTerms_ReturnsNearestUpcoming()
    {
        DateOnly today = new(2027, 1, 30);

        Term? current = SchoolCalendar.PickCurrent([Spring, Past, Autumn], today);

        current.ShouldBe(Spring);
        (current!.StartDate > today).ShouldBeTrue("yaklaşan dönem");
        SchoolCalendar.PickCurrent([Past, Autumn, Spring], new DateOnly(2026, 7, 1)).ShouldBe(Autumn, "en yakın başlayacak dönem");
    }

    [Fact]
    public void PickCurrent_OnlyPastTermsOrNone_ReturnsNull()
    {
        SchoolCalendar.PickCurrent([Past], new DateOnly(2026, 7, 1)).ShouldBeNull();
        SchoolCalendar.PickCurrent([], new DateOnly(2026, 7, 1)).ShouldBeNull();
    }

    [Fact]
    public void PickDefault_NoCurrentOrUpcoming_ReturnsNewest()
    {
        Term older = Term.Create(Guid.NewGuid(), "Önceki", new DateOnly(2024, 9, 1), new DateOnly(2025, 6, 20), Created);

        SchoolCalendar.PickDefault([older, Past], new DateOnly(2026, 7, 1)).ShouldBe(Past);
        SchoolCalendar.PickDefault([older, Past, Autumn], new DateOnly(2026, 7, 1)).ShouldBe(Autumn);
        SchoolCalendar.PickDefault([], new DateOnly(2026, 7, 1)).ShouldBeNull();
    }
}
