using OkulYonetim.Domain.Organization;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Organization;

/// <summary>Spec 0008 AC-4, AC-5: dönem tarih kuralı (başlangıç bitişten önce, en fazla 366 gün, uç günler dahil) ve çakışma uçları.</summary>
public sealed class TermTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsValidRange_366DaysInclusive_IsAccepted_367Rejected()
    {
        DateOnly start = new(2026, 9, 1);
        Term.LengthInDays(start, new DateOnly(2027, 9, 1)).ShouldBe(366);
        Term.IsValidRange(start, new DateOnly(2027, 9, 1)).ShouldBeTrue();
        Term.IsValidRange(start, new DateOnly(2027, 9, 2)).ShouldBeFalse();
    }

    [Fact]
    public void IsValidRange_EndNotAfterStart_IsRejected()
    {
        DateOnly day = new(2026, 9, 1);
        Term.IsValidRange(day, day).ShouldBeFalse();
        Term.IsValidRange(day, day.AddDays(-1)).ShouldBeFalse();
        Term.IsValidRange(day, day.AddDays(1)).ShouldBeTrue();
        Should.Throw<ArgumentOutOfRangeException>(() => Term.Create(Guid.NewGuid(), "Güz", day, day, Now));
    }

    [Fact]
    public void Overlaps_EndDayEqualsOtherStart_Overlaps_NextDayDoesNot()
    {
        Term autumn = Term.Create(Guid.NewGuid(), "2026-2027 Güz", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 22), Now);

        autumn.Overlaps(new DateOnly(2027, 1, 22), new DateOnly(2027, 6, 20)).ShouldBeTrue("bitiş günü öbürünün başlangıç günü: çakışır");
        autumn.Overlaps(new DateOnly(2027, 1, 23), new DateOnly(2027, 6, 20)).ShouldBeFalse("ertesi gün başlayan çakışmaz");
        autumn.Overlaps(new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 1)).ShouldBeTrue("öncekinin bitişi bu dönemin ilk günü");
        autumn.Overlaps(new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31)).ShouldBeFalse();
        autumn.Overlaps(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2)).ShouldBeTrue("içinde kalan");
        autumn.Overlaps(new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31)).ShouldBeTrue("kapsayan");
    }

    [Fact]
    public void Contains_IncludesBothEndDays()
    {
        Term term = Term.Create(Guid.NewGuid(), " Güz ", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 22), Now);

        term.Name.ShouldBe("Güz");
        term.Contains(new DateOnly(2026, 9, 1)).ShouldBeTrue();
        term.Contains(new DateOnly(2027, 1, 22)).ShouldBeTrue();
        term.Contains(new DateOnly(2026, 8, 31)).ShouldBeFalse();
        term.Contains(new DateOnly(2027, 1, 23)).ShouldBeFalse();
    }
}
